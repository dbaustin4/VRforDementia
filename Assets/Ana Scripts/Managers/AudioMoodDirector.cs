using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Handles layered audio moods for VR:
/// - 2D layers: Ambience, Music, Emotional SFX/Texture
/// - 3D layers: Spatial SFX, Spatial Voices, Hallucination whispers
/// - Each mood has its own clips + volumes
/// - Smooth crossfades between moods
/// - Simple helpers to spawn spatial audio sources
/// </summary>
[DisallowMultipleComponent]
public class AudioMoodDirector : MonoBehaviour
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

        [Header("Ambience Layer (2D or subtle 3D)")]
        public AudioClip ambienceClip;
        [Range(0f, 1f)] public float ambienceVolume;

        [Header("Music / Pad Layer (2D)")]
        public AudioClip musicClip;
        [Range(0f, 1f)] public float musicVolume;

        [Header("SFX / Texture Layer (2D bed)")]
        public AudioClip sfxClip;
        [Range(0f, 1f)] public float sfxVolume;

        [Header("Global Mood Settings")]
        [Range(0.1f, 2f)] public float pitch;  // 1 = normal, <1 slower, >1 more intense
    }

    // ---------- Inspector: 2D mood bed ----------

    [Header("Layered 2D Audio Sources (Mood Bed)")]
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

    // ---------- Inspector: 3D spatial layers ----------

    [Header("Spatial Audio Settings")]
    [Tooltip("If true, mark 3D AudioSources as 'spatialize' so the project spatializer (Meta/Oculus, etc.) is used.")]
    public bool useProjectSpatializer = true;

    [Header("3D SFX Defaults (Environment, objects)")]
    public float sfx3DMinDistance = 1f;
    public float sfx3DMaxDistance = 15f;
    public AudioMixerGroup sfx3DMixerGroup;

    [Header("3D Voice Defaults (Characters)")]
    public float voice3DMinDistance = 0.5f;
    public float voice3DMaxDistance = 10f;
    public AudioMixerGroup voice3DMixerGroup;

    [Header("Hallucination Defaults")]
    [Tooltip("Radius around the listener head for random whispers.")]
    public float hallucinationRadius = 0.7f;
    public float hallucinationMinVertical = -0.1f;
    public float hallucinationMaxVertical = 0.4f;
    public AudioMixerGroup hallucinationMixerGroup;

    // ---------- Testing / Preview ----------

    [Header("Testing (Preview Without NarrativeDirector)")]
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

    // ============================================================
    //                    2D MOOD BED API
    // ============================================================

    /// <summary>
    /// Instantly switches to a mood (no fade). Useful for scene loads / hard cuts.
    /// </summary>
    public void SetMoodInstant(MoodId mood)
    {
        var preset = GetPreset(mood);
        _currentPreset = preset;
        ApplyPresetInstant(preset);

        if (logTransitions)
            Debug.Log($"[AudioMoodDirector] Set mood instant: {mood}");
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

    // ---------- Core logic for 2D bed ----------

    private MoodAudioPreset GetPreset(MoodId mood)
    {
        foreach (var preset in moodPresets)
        {
            if (preset.mood == mood)
                return preset;
        }

        Debug.LogWarning($"[AudioMoodDirector] No preset found for mood '{mood}'. Falling back to Neutral.");
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
            Debug.Log($"[AudioMoodDirector] Crossfading from {from.mood} to {to.mood} over {duration:0.00}s");

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
            Debug.LogWarning("[AudioMoodDirector] AmbienceSource is not set.");
        if (musicSource == null)
            Debug.LogWarning("[AudioMoodDirector] MusicSource is not set.");
        if (sfxSource == null)
            Debug.LogWarning("[AudioMoodDirector] SfxSource is not set.");
    }

    // ============================================================
    //                    3D SPATIAL LAYERS API
    // ============================================================

    // ----------------- Spatial SFX (environment) -----------------

    /// <summary>
    /// Play a 3D one-shot SFX at a world position.
    /// Use for environmental sounds (door, kettle, clock tick, etc.).
    /// </summary>
    public void PlaySpatialSFX(AudioClip clip, Vector3 worldPosition, float volume = 1f,
                               float minDistanceOverride = -1f, float maxDistanceOverride = -1f)
    {
        if (clip == null) return;

        GameObject go = new GameObject("SpatialSFX_" + clip.name);
        go.transform.position = worldPosition;

        AudioSource aSrc = go.AddComponent<AudioSource>();
        aSrc.clip = clip;
        aSrc.volume = volume;
        aSrc.spatialBlend = 1f; // full 3D
        aSrc.rolloffMode = AudioRolloffMode.Logarithmic;
        aSrc.minDistance = (minDistanceOverride > 0f) ? minDistanceOverride : sfx3DMinDistance;
        aSrc.maxDistance = (maxDistanceOverride > 0f) ? maxDistanceOverride : sfx3DMaxDistance;
        aSrc.dopplerLevel = 0f; // avoid weird Doppler in VR

        if (useProjectSpatializer)
            aSrc.spatialize = true;

        if (sfx3DMixerGroup != null)
            aSrc.outputAudioMixerGroup = sfx3DMixerGroup;

        aSrc.Play();
        Destroy(go, clip.length + 0.2f);
    }

    /// <summary>
    /// Convenience overload: play 3D SFX at a Transform position.
    /// </summary>
    public void PlaySpatialSFX(AudioClip clip, Transform sourceTransform, float volume = 1f,
                               float minDistanceOverride = -1f, float maxDistanceOverride = -1f)
    {
        if (sourceTransform == null) return;
        PlaySpatialSFX(clip, sourceTransform.position, volume, minDistanceOverride, maxDistanceOverride);
    }

    // ----------------- Spatial Voices (characters) -----------------

    /// <summary>
    /// Play a 3D voice line attached to a character transform (e.g. head).
    /// The AudioSource follows the transform and auto-destroys when the clip ends.
    /// </summary>
    public AudioSource PlaySpatialVoiceFollowing(AudioClip clip, Transform characterTransform,
                                                 float volume = 1f)
    {
        if (clip == null || characterTransform == null) return null;

        GameObject go = new GameObject("SpatialVoice_" + clip.name);
        go.transform.position = characterTransform.position;

        AudioSource aSrc = go.AddComponent<AudioSource>();
        aSrc.clip = clip;
        aSrc.volume = volume;
        aSrc.spatialBlend = 1f;
        aSrc.rolloffMode = AudioRolloffMode.Logarithmic;
        aSrc.minDistance = voice3DMinDistance;
        aSrc.maxDistance = voice3DMaxDistance;
        aSrc.dopplerLevel = 0f;

        if (useProjectSpatializer)
            aSrc.spatialize = true;

        if (voice3DMixerGroup != null)
            aSrc.outputAudioMixerGroup = voice3DMixerGroup;

        var follow = go.AddComponent<SpatialAudioFollow>();
        follow.target = characterTransform;
        follow.audioSource = aSrc;

        aSrc.Play();
        return aSrc;
    }

    /// <summary>
    /// Play a 3D voice line from a fixed world position (no follow).
    /// Use for TV, radio, neighbours through wall, etc.
    /// </summary>
    public AudioSource PlaySpatialVoiceAt(AudioClip clip, Vector3 worldPosition, float volume = 1f)
    {
        if (clip == null) return null;

        GameObject go = new GameObject("SpatialVoice_" + clip.name);
        go.transform.position = worldPosition;

        AudioSource aSrc = go.AddComponent<AudioSource>();
        aSrc.clip = clip;
        aSrc.volume = volume;
        aSrc.spatialBlend = 1f;
        aSrc.rolloffMode = AudioRolloffMode.Logarithmic;
        aSrc.minDistance = voice3DMinDistance;
        aSrc.maxDistance = voice3DMaxDistance;
        aSrc.dopplerLevel = 0f;

        if (useProjectSpatializer)
            aSrc.spatialize = true;

        if (voice3DMixerGroup != null)
            aSrc.outputAudioMixerGroup = voice3DMixerGroup;

        var follow = go.AddComponent<SpatialAudioFollow>();
        follow.target = null;     // no follow
        follow.audioSource = aSrc;

        aSrc.Play();
        return aSrc;
    }

    // ----------------- Hallucination Whispers -----------------

    /// <summary>
    /// Play a hallucination whisper somewhere around the listener's head.
    /// Provide the head transform (e.g. camera center).
    /// </summary>
    public AudioSource PlayHallucinationWhisper(AudioClip clip, Transform listenerHead,
                                                float volume = 1f)
    {
        if (clip == null || listenerHead == null) return null;

        Vector3 offset = UnityEngine.Random.onUnitSphere * hallucinationRadius;
        offset.y = Mathf.Clamp(offset.y, hallucinationMinVertical, hallucinationMaxVertical);

        Vector3 pos = listenerHead.position + offset;

        GameObject go = new GameObject("HallucinationWhisper_" + clip.name);
        go.transform.position = pos;

        AudioSource aSrc = go.AddComponent<AudioSource>();
        aSrc.clip = clip;
        aSrc.volume = volume;
        aSrc.spatialBlend = 1f;
        aSrc.rolloffMode = AudioRolloffMode.Linear;
        aSrc.minDistance = 0.01f;
        aSrc.maxDistance = hallucinationRadius * 2f;
        aSrc.dopplerLevel = 0f;

        if (useProjectSpatializer)
            aSrc.spatialize = true;

        if (hallucinationMixerGroup != null)
            aSrc.outputAudioMixerGroup = hallucinationMixerGroup;

        var follow = go.AddComponent<SpatialAudioFollow>();
        follow.target = null; // fixed at spawn point
        follow.audioSource = aSrc;

        aSrc.Play();
        return aSrc;
    }
}
