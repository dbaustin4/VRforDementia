using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MoodTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag your VisualMoodDirector here (the one on the Volume).")]
    public VisualMoodDirector visualMoodDirector;

    [Header("Mood to Activate")]
    public VisualMoodDirector.MoodId moodToActivate;

    [Header("Interaction")]
    public string playerTag = "Player";
    [Tooltip("If false, mood triggers as soon as player enters the trigger.")]
    public bool requireKeyPress = true;           // E for desktop testing
    public KeyCode interactKey = KeyCode.E;

    [Header("Transition")]
    [Tooltip("Fade duration when switching moods. Set to 0 for hard cut.")]
    public float fadeSeconds = 1.5f;
    [Tooltip("If true, ignore fadeSeconds and apply instantly.")]
    public bool useInstantApply = false;

    [Header("Debug Helpers")]
    [Tooltip("If true, auto-activate this mood 2s after Start(), for testing.")]
    public bool autoTestAfter2s = false;

    private bool playerInRange;

    private void Reset()
    {
        // Make sure the collider is a trigger
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Start()
    {
        Debug.Log($"[MoodTrigger:{name}] Ready. requireKeyPress={requireKeyPress}");
        if (autoTestAfter2s)
            Invoke(nameof(ActivateMood), 2f);
    }

    private void Update()
    {
        if (!requireKeyPress || !playerInRange) return;

        if (Input.GetKeyDown(interactKey))
        {
            Debug.Log("[MoodTrigger] Key pressed -> ActivateMood()");
            ActivateMood();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInRange = true;
        Debug.Log("[MoodTrigger] Player entered trigger.");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInRange = false;
        Debug.Log("[MoodTrigger] Player exited trigger.");
    }

    // Call this from XR button / UI event to bypass keyboard completely.
    public void Interact()
    {
        if (!playerInRange)
        {
            Debug.Log("[MoodTrigger] Interact() ignored (player not in range).");
            return;
        }

        Debug.Log("[MoodTrigger] Interact() -> ActivateMood()");
        ActivateMood();
    }

    [ContextMenu("Test Activate (ContextMenu)")]
    public void TestContextMenu() => ActivateMood();

    // ------- Apply mood on the director -------
    private void ActivateMood()
    {
        if (visualMoodDirector == null)
        {
            Debug.LogWarning("[MoodTrigger] No VisualMoodDirector assigned.");
            return;
        }

        if (useInstantApply || fadeSeconds <= 0f)
        {
            visualMoodDirector.forceInstantApply(moodToActivate);
            Debug.Log($"[MoodTrigger] Instant mood: {moodToActivate}");
        }
        else
        {
            visualMoodDirector.CrossfadeTo(moodToActivate, fadeSeconds);
            Debug.Log($"[MoodTrigger] Crossfade mood: {moodToActivate} over {fadeSeconds:0.00}s");
        }
    }
}
