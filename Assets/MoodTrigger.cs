using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MoodInteractable : MonoBehaviour
{
    [Header("References")]
    public MoodFXDirector moodFXDirector;                // drag your MoodFXDirector here

    [Header("Mood to Activate")]
    public MoodFXDirector.MoodId moodToActivate;         // dropdown

    [Header("Interaction")]
    public string playerTag = "Player";
    public bool requireKeyPress = true;                  // E for desktop testing
    public KeyCode interactKey = KeyCode.E;

    [Header("Debug Helpers")]
    public bool autoTestAfter2s = false;                 // set true once to prove wiring works

    private bool playerInRange;

    private void Reset()
    {
        // Make sure the collider is a trigger
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Start()
    {
        Debug.Log($"[MoodInteractable:{name}] Ready. requireKeyPress={requireKeyPress}");
        if (autoTestAfter2s) Invoke(nameof(ActivateMood), 2f);
    }

    private void Update()
    {
        if (requireKeyPress && playerInRange && Input.GetKeyDown(interactKey))
        {
            Debug.Log("[MoodInteractable] E pressed -> ActivateMood()");
            ActivateMood();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInRange = true;
        Debug.Log("[MoodInteractable] Player entered trigger.");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInRange = false;
        Debug.Log("[MoodInteractable] Player exited trigger.");
    }

    // Call this from XR button/UI event to bypass E-key completely.
    public void Interact()
    {
        if (!playerInRange)
        {
            Debug.Log("[MoodInteractable] Interact() ignored (player not in range).");
            return;
        }
        Debug.Log("[MoodInteractable] Interact() -> ActivateMood()");
        ActivateMood();
    }

    [ContextMenu("Test Activate (ContextMenu)")]
    public void TestContextMenu() => ActivateMood();

    // ------- Apply mood on the director -------
    private void ActivateMood()
    {
        if (moodFXDirector == null)
        {
            Debug.LogWarning("[MoodInteractable] No MoodFXDirector assigned.");
            return;
        }

        var t = moodFXDirector.GetType();
        Debug.Log($"[MoodInteractable] Activating '{moodToActivate}' using director type {t.Name}");

        // 1) CrossfadeTo(MoodId)
        var crossfade = t.GetMethod("CrossfadeTo",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new[] { typeof(MoodFXDirector.MoodId) }, null);
        if (crossfade != null)
        {
            Debug.Log("[MoodInteractable] Found CrossfadeTo(MoodId).");
            crossfade.Invoke(moodFXDirector, new object[] { moodToActivate });
            return;
        }

        // 2) ApplyMoodInstant(MoodId)
        var applyInstant = t.GetMethod("ApplyMoodInstant",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new[] { typeof(MoodFXDirector.MoodId) }, null);
        if (applyInstant != null)
        {
            Debug.Log("[MoodInteractable] Found ApplyMoodInstant(MoodId).");
            applyInstant.Invoke(moodFXDirector, new object[] { moodToActivate });
            return;
        }

        // 3) selectedMood + ApplySelectedMoodInstant()
        var selectedField = t.GetField("selectedMood",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var applySelected = t.GetMethod("ApplySelectedMoodInstant",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, System.Type.EmptyTypes, null);

        if (selectedField != null && applySelected != null)
        {
            Debug.Log("[MoodInteractable] Using selectedMood + ApplySelectedMoodInstant().");
            selectedField.SetValue(moodFXDirector, moodToActivate);
            applySelected.Invoke(moodFXDirector, null);
            return;
        }

        Debug.LogWarning("[MoodInteractable] No compatible method found on MoodFXDirector " +
                         "(expected CrossfadeTo(MoodId), ApplyMoodInstant(MoodId), or selectedMood + ApplySelectedMoodInstant()).");
    }
}
