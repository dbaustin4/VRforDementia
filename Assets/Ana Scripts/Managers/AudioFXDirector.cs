using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Handles layered audio moods for VR:
/// - 3 layers: Ambience, Music, SFX
/// - Each mood has its own clips + volumes
/// - Smooth crossfades between moods
/// - Has testing/preview tools so you can try moods without StoryGameManager
/// </summary>
[DisallowMultipleComponent]
public class AudioFXDirector : MonoBehaviour
{
    // ---------- Types ----------

    public enum MoodId
    {
        Neutral,
        Happiness,
        Sadness,
        Nostalgic,
        Furious,
        Triggered
    }

    [Serializable]
    public struct MoodAudioPreset
    {
        [Header("Mood Id")]
        public MoodId mood;

        [Header("Ambience Layer")]
        public AudioClip ambienceClip;
        [Range(0f, 1f)] public float ambienceVolume;

        [Header("Music / Pad Layer")]
        public AudioClip musicClip;
        [Range(0f, 1f)] public float musicVolume;

        [Header("SFX / Texture Layer")]
        public AudioClip sfxClip;
        [Range(0f, 1f)] public float sfxVolume;

        [Header("Global Mood Settings")]
        [Range(0.1f, 2f)] public float pitch;  // 1 = normal, <1 slower, >1 more intense
    }

    // ---------- Inspector ----------

    [Header("Layered Audio Sources")]
    [Tooltip("Continuous environment tone (wind, room tone, etc.)")]
    public AudioSource ambienceSource;

    [Tooltip("Music, pads, tonal emotional layer")]
    public AudioSource musicSource;

    [Tooltip("Heartbeats, ticks, vinyl noise, whispers, etc.")]
    public AudioSource sfxSource;

    [Header("Mood Presets")]
    public MoodAudioPreset[] moodPresets;

    [Header("Default Settings")]
    public MoodId startMood = MoodId.Neutral;
    [Range(0.1f, 10f)] public float defaultFadeDuration = 2f;

    [Header("Debug")]
    public bool logTransitions = false;

    // ---------- Testing / Preview ----------

    [Header("Testing (Preview Without StoryGameManager)")]
    [Tooltip("Use this to preview moods directly from the inspector or with hotkeys.")]
    public bool enableTestingTools = true;

    [Tooltip("Mood used when pressing the 'Apply Preview Mood' button in the inspector.")]
    public MoodId previewMood = MoodId.Neutral;

    [Range(0.1f, 10f)]
    [Tooltip("Fade time used when previewing moods from the inspector or hotkeys.")]
    public float previewFadeSeconds = 1.5f;

    [Tooltip("Enable number keys 1–6 in Play Mode to quickly switch moods.")]
    public bool enableKeyboardShortcuts = true;

    // ---------- Private state ----------

    private MoodAudioPreset _currentPreset;
    private Coroutine _activeFadeRoutine;

    // ---------- Unity ----------

    private void Awake()
    {
        ValidateSources();
        _currentPreset = GetPreset(startMood);
        ApplyPresetInstant(_currentPreset);
    }

    private void Update()
    {
        if (!Application.isPlaying || !enableTestingTools || !enableKeyboardShortcuts)
            return;

        // Simple hotkeys for quick mood testing in Play Mode
        if (Input.GetKeyDown(KeyCode.Alpha1)) CrossfadeTo(MoodId.Neutral, previewFadeSeconds);
        if (Input.GetKeyDown(KeyCode.Alpha2)) CrossfadeTo(MoodId.Happiness, previewFadeSeconds);
        if (Input.GetKeyDown(KeyCode.Alpha3)) CrossfadeTo(MoodId.Sadness, previewFadeSeconds);
        if (Input.GetKeyDown(KeyCode.Alpha4)) CrossfadeTo(MoodId.Nostalgic, previewFadeSeconds);
        if (Input.GetKeyDown(KeyCode.Alpha5)) CrossfadeTo(MoodId.Furious, previewFadeSeconds);
        if (Input.GetKeyDown(KeyCode.Alpha6)) CrossfadeTo(MoodId.Triggered, previewFadeSeconds);
    }

    // ---------- Public API ----------

    /// <summary>
    /// Instantly switches to a mood (no fade). Useful for scene loads / hard cuts.
    /// </summary>
    public void SetMoodInstant(MoodId mood)
    {
        var preset = GetPreset(mood);
        _currentPreset = preset;
        ApplyPresetInstant(preset);

        if (logTransitions)
            Debug.Log($"[AudioFXDirector] Set mood instant: {mood}");
    }

    /// <summary>
    /// Crossfades smoothly between current mood and target mood over a duration.
    /// </summary>
    public void CrossfadeTo(MoodId mood, float duration = -1f)
    {
        if (duration <= 0f)
            duration = defaultFadeDuration;

        var targetPreset = GetPreset(mood);

        if (_activeFadeRoutine != null)
            StopCoroutine(_activeFadeRoutine);

        _activeFadeRoutine = StartCoroutine(FadeRoutine(_currentPreset, targetPreset, duration));
    }

    // Convenience if you ever want to call this directly
    public void CrossfadeToNeutral(float duration = -1f) => CrossfadeTo(MoodId.Neutral, duration);

    /// <summary>
    /// Called from the inspector button / preview tools.
    /// </summary>
    public void ApplyPreviewMood()
    {
        if (!enableTestingTools) return;
        CrossfadeTo(previewMood, previewFadeSeconds);
    }

    // ---------- Core logic ----------

    private MoodAudioPreset GetPreset(MoodId mood)
    {
        foreach (var preset in moodPresets)
        {
            if (preset.mood == mood)
                return preset;
        }

        Debug.LogWarning($"[AudioFXDirector] No preset found for mood '{mood}'. Falling back to Neutral.");
        foreach (var preset in moodPresets)
        {
            if (preset.mood == MoodId.Neutral)
                return preset;
        }

        // Final fallback: empty struct
        return new MoodAudioPreset { mood = mood, pitch = 1f };
    }

    private void ApplyPresetInstant(MoodAudioPreset preset)
    {
        // Set clips & volumes
        ApplyLayer(ambienceSource, preset.ambienceClip, preset.ambienceVolume, preset.pitch);
        ApplyLayer(musicSource, preset.musicClip, preset.musicVolume, preset.pitch);
        ApplyLayer(sfxSource, preset.sfxClip, preset.sfxVolume, preset.pitch);

        _currentPreset = preset;
    }

    private void ApplyLayer(AudioSource source, AudioClip clip, float volume, float pitch)
    {
        if (source == null) return;

        source.pitch = pitch;

        if (clip == null)
        {
            source.Stop();
            source.clip = null;
            return;
        }

        if (source.clip != clip)
        {
            source.clip = clip;
            source.loop = true;
            source.Play();
        }

        source.volume = volume;
    }

    private IEnumerator FadeRoutine(MoodAudioPreset from, MoodAudioPreset to, float duration)
    {
        if (logTransitions)
            Debug.Log($"[AudioFXDirector] Crossfading from {from.mood} to {to.mood} over {duration:0.00}s");

        float t = 0f;

        float startAmbienceVol = ambienceSource != null ? ambienceSource.volume : 0f;
        float startMusicVol = musicSource != null ? musicSource.volume : 0f;
        float startSfxVol = sfxSource != null ? sfxSource.volume : 0f;

        // Ensure target clips are assigned & playing
        SetupTargetLayer(ambienceSource, to.ambienceClip);
        SetupTargetLayer(musicSource, to.musicClip);
        SetupTargetLayer(sfxSource, to.sfxClip);

        float targetAmbienceVol = to.ambienceVolume;
        float targetMusicVol = to.musicVolume;
        float targetSfxVol = to.sfxVolume;

        while (t < duration)
        {
            float normalized = t / duration;
            float ease = Mathf.SmoothStep(0f, 1f, normalized);

            if (ambienceSource != null)
            {
                ambienceSource.pitch = Mathf.Lerp(from.pitch, to.pitch, ease);
                ambienceSource.volume = Mathf.Lerp(startAmbienceVol, targetAmbienceVol, ease);
            }

            if (musicSource != null)
            {
                musicSource.pitch = Mathf.Lerp(from.pitch, to.pitch, ease);
                musicSource.volume = Mathf.Lerp(startMusicVol, targetMusicVol, ease);
            }

            if (sfxSource != null)
            {
                sfxSource.pitch = Mathf.Lerp(from.pitch, to.pitch, ease);
                sfxSource.volume = Mathf.Lerp(startSfxVol, targetSfxVol, ease);
            }

            t += Time.deltaTime;
            yield return null;
        }

        // Final snap
        ApplyPresetInstant(to);
        _currentPreset = to;
        _activeFadeRoutine = null;
    }

    private void SetupTargetLayer(AudioSource source, AudioClip clip)
    {
        if (source == null) return;

        if (clip == null)
        {
            if (source.isPlaying)
                source.Stop();
            source.clip = null;
            return;
        }

        if (source.clip != clip)
        {
            source.clip = clip;
            source.loop = true;
            source.Play();
        }
    }

    private void ValidateSources()
    {
        if (ambienceSource == null)
            Debug.LogWarning("[AudioFXDirector] AmbienceSource is not set.");
        if (musicSource == null)
            Debug.LogWarning("[AudioFXDirector] MusicSource is not set.");
        if (sfxSource == null)
            Debug.LogWarning("[AudioFXDirector] SfxSource is not set.");
    }
}
