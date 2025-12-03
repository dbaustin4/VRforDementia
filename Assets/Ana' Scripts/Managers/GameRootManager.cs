using UnityEngine;

/// <summary>
/// GameRootManager
/// ----------------
/// - Lives in your boot scene (add it to an empty GameObject, e.g. "GameRoot").
/// - Becomes a persistent singleton (DontDestroyOnLoad).
/// - Holds references to:
///     * VisualMoodDirector (visual post-fx moods)
///     * AudioMoodDirector  (audio mood bed + spatial helpers)
///     * NarrativeDirector  (acts/chapters/dialogue)
///     * PlayerHeadTransform (for spatial hallucinations etc.)
/// - Auto-finds missing references on startup.
/// - Exposes small helper methods to start/restart the story.
/// 
/// It does NOT micro-manage moods or narrative; that stays inside the
/// director scripts. Think of this as the "app root" or "system hub".
/// </summary>
[DisallowMultipleComponent]
public class GameRootManager : MonoBehaviour
{
    public static GameRootManager Instance { get; private set; }

    [Header("Core Systems (optional: drag in Inspector)")]
    public VisualMoodDirector visualMoodDirector;
    public AudioMoodDirector audioMoodDirector;
    public NarrativeDirector narrativeDirector;

    [Header("Player")]
    [Tooltip("Center of the player's head / camera (used by spatial audio helpers).")]
    public Transform playerHeadTransform;

    [Header("Options")]
    [Tooltip("If true, the root object will persist across scene loads.")]
    public bool dontDestroyOnLoad = true;

    [Tooltip("Automatically call NarrativeDirector.StartStory() when ready.")]
    public bool autoStartStory = true;

    private void Awake()
    {
        // Simple singleton pattern
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameRootManager] Duplicate instance found, destroying this one.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        // Try to locate systems if not assigned
        EnsureCoreReferences();
    }

    private void Start()
    {
        if (autoStartStory && narrativeDirector != null)
        {
            narrativeDirector.StartStory();
        }
    }

    /// <summary>
    /// Ensure core system references are assigned.
    /// Called in Awake, but you can also call it manually if scenes change.
    /// </summary>
    public void EnsureCoreReferences()
    {
        if (visualMoodDirector == null)
            visualMoodDirector = FindObjectOfType<VisualMoodDirector>();

        if (audioMoodDirector == null)
            audioMoodDirector = FindObjectOfType<AudioMoodDirector>();

        if (narrativeDirector == null)
            narrativeDirector = FindObjectOfType<NarrativeDirector>();

        if (playerHeadTransform == null)
        {
            // Try to find main camera as a fallback for head transform
            var cam = Camera.main;
            if (cam != null)
                playerHeadTransform = cam.transform;
        }

        Debug.Log(
            $"[GameRootManager] Refs: Visual={visualMoodDirector}, Audio={audioMoodDirector}, " +
            $"Narrative={narrativeDirector}, Head={playerHeadTransform}"
        );
    }

    /// <summary>
    /// Small helper: are all the core systems currently found?
    /// </summary>
    public bool IsReady =>
        visualMoodDirector != null &&
        audioMoodDirector != null &&
        narrativeDirector != null;

    // --------------------------------------------------------------------
    // Public helpers you can call from UI / XR buttons / debug code
    // --------------------------------------------------------------------

    /// <summary>
    /// Restart the entire story from the first act/chapter.
    /// </summary>
    public void RestartStory()
    {
        if (narrativeDirector == null)
        {
            Debug.LogWarning("[GameRootManager] RestartStory() called but NarrativeDirector is missing.");
            return;
        }

        narrativeDirector.StartStory();
    }

    /// <summary>
    /// Convenience accessors so other scripts don't have to call FindObjectOfType.
    /// </summary>
    public VisualMoodDirector GetVisualMoodDirector() => visualMoodDirector;
    public AudioMoodDirector GetAudioMoodDirector() => audioMoodDirector;
    public NarrativeDirector GetNarrativeDirector() => narrativeDirector;
    public Transform GetPlayerHeadTransform() => playerHeadTransform;
}
