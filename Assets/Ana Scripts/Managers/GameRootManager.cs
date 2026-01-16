using UnityEngine;

/// <summary>
/// GameRootManager
/// ----------------
/// - Lives in your boot scene (add it to an empty GameObject, e.g. "GameRoot").
/// - Becomes a persistent singleton (DontDestroyOnLoad).
/// - Holds references to:
///     * VisualMoodDirector (visual post-fx moods)
///     * AudioMoodDirector  (audio mood bed + spatial helpers)
///     * PlayerHeadTransform (for spatial hallucinations etc.)
/// - Auto-finds missing references on startup.
///
/// Think of this as the "app root" or "system hub".
/// </summary>
[DisallowMultipleComponent]
public class GameRootManager : MonoBehaviour
{
    public static GameRootManager Instance { get; private set; }

    [Header("Core Systems (optional: drag in Inspector)")]
    public VisualMoodDirector visualMoodDirector;
    public AudioMoodDirector audioMoodDirector;

    [Header("Player")]
    [Tooltip("Center of the player's head / camera (used by spatial audio helpers).")]
    public Transform playerHeadTransform;

    [Header("Options")]
    [Tooltip("If true, the root object will persist across scene loads.")]
    public bool dontDestroyOnLoad = true;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameRootManager] Duplicate instance found, destroying this one.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        EnsureCoreReferences();
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

        if (playerHeadTransform == null)
        {
            // Fallback: main camera as head transform
            var cam = Camera.main;
            if (cam != null)
                playerHeadTransform = cam.transform;
        }

        Debug.Log(
            $"[GameRootManager] Refs: Visual={visualMoodDirector}, Audio={audioMoodDirector}, Head={playerHeadTransform}"
        );
    }

    /// <summary>
    /// Small helper: are all the core systems currently found?
    /// </summary>
    public bool IsReady =>
        visualMoodDirector != null &&
        audioMoodDirector != null;

    // --------------------------------------------------------------------
    // Convenience accessors
    // --------------------------------------------------------------------

    public VisualMoodDirector GetVisualMoodDirector() => visualMoodDirector;
    public AudioMoodDirector GetAudioMoodDirector() => audioMoodDirector;
    public Transform GetPlayerHeadTransform() => playerHeadTransform;
}
