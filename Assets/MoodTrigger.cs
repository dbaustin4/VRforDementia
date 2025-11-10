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
    public bool requireKeyPress = true;                  // E for desktop test
    public KeyCode interactKey = KeyCode.E;

    private bool playerInRange;

    // ------- Ways to interact -------
    // 1) Call this from any Button/XR event: hook up Interact() in the Inspector.
    public void Interact()
    {
        if (!playerInRange) return;      // only when the player is near the object
        ActivateMood();
    }

    // 2) Optional desktop key (for testing without VR)
    private void Update()
    {
        if (requireKeyPress && playerInRange && Input.GetKeyDown(interactKey))
            ActivateMood();
    }

    // ------- Range gate via trigger -------
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag)) playerInRange = true;
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag)) playerInRange = false;
    }

    // ------- Apply mood on the director -------
    private void ActivateMood()
    {
        if (moodFXDirector == null)
        {
            Debug.LogWarning("MoodInteractable: No MoodFXDirector assigned.");
            return;
        }

        // Try common director APIs without you changing your director:
        // 1) CrossfadeTo(MoodId)
        var t = moodFXDirector.GetType();
        var crossfade = t.GetMethod("CrossfadeTo", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                                    null, new[] { typeof(MoodFXDirector.MoodId) }, null);
        if (crossfade != null)
        {
            crossfade.Invoke(moodFXDirector, new object[] { moodToActivate });
            Debug.Log($"MoodInteractable: Crossfaded to {moodToActivate}");
            return;
        }

        // 2) ApplyMoodInstant(MoodId)
        var applyInstant = t.GetMethod("ApplyMoodInstant", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                                       null, new[] { typeof(MoodFXDirector.MoodId) }, null);
        if (applyInstant != null)
        {
            applyInstant.Invoke(moodFXDirector, new object[] { moodToActivate });
            Debug.Log($"MoodInteractable: Applied {moodToActivate} (instant)");
            return;
        }

        // 3) Fallback: if your director uses "selectedMood" + "ApplySelectedMoodInstant()"
        var selectedField = t.GetField("selectedMood", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var applySelected = t.GetMethod("ApplySelectedMoodInstant", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                                        null, System.Type.EmptyTypes, null);

        if (selectedField != null && applySelected != null)
        {
            selectedField.SetValue(moodFXDirector, moodToActivate);
            applySelected.Invoke(moodFXDirector, null);
            Debug.Log($"MoodInteractable: Set selected and applied {moodToActivate}");
            return;
        }

        Debug.LogWarning("MoodInteractable: No compatible method found on MoodFXDirector (expected CrossfadeTo(MoodId), ApplyMoodInstant(MoodId) or selectedMood + ApplySelectedMoodInstant()).");
    }
}
