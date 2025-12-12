using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class AudioMoodDirector : MonoBehaviour
{
    // Moods must match VisualMoodDirector enum indexing.
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
        public MoodId mood;

        [Header("Ambience")]
        public AudioClip ambienceClip;
        [Range(0f, 1f)] public float ambienceVolume;

        [Header("Music")]
        public AudioClip musicClip;
        [Range(0f, 1f)] public float musicVolume;

        [Header("SFX / Texture")]
        public AudioClip sfxClip;
        [Range(0f, 1f)] public float sfxVolume;

        [Header("Pitch (optional timbre shift)")]
        public float pitch;
    }

    [Header("Audio Sources")]
    public AudioSource ambienceSource;
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Mood Presets")]
    public MoodAudioPreset[] moodPresets;

    [Header("Defaults")]
    public MoodId startMood = MoodId.Neutral;
    public float defaultFadeDuration = 2f;

    [Header("Debug")]
    public bool logTransitions;

    private Coroutine fadeRoutine;
    private MoodAudioPreset currentPreset;

    private void Start()
    {
        ApplyMoodInstant(startMood);
    }

    // --------------------------------------------------
    // PUBLIC API
    // --------------------------------------------------
    public void SetMoodInstant(MoodId mood)
    {
        ApplyMoodInstant(mood);
    }

    public void CrossfadeTo(MoodId mood, float fadeSeconds)
    {
        if (fadeSeconds <= 0f)
        {
            ApplyMoodInstant(mood);
            return;
        }

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(CrossfadeRoutine(mood, fadeSeconds));
    }

    // --------------------------------------------------
    // INTERNAL
    // --------------------------------------------------
    private MoodAudioPreset GetPreset(MoodId mood)
    {
        foreach (var p in moodPresets)
            if (p.mood == mood)
                return p;

        Debug.LogWarning($"[AudioMoodDirector] No preset for mood: {mood}");
        return default;
    }

    private void ApplyMoodInstant(MoodId mood)
    {
        var p = GetPreset(mood);
        currentPreset = p;

        if (ambienceSource) ApplyAudioInstant(ambienceSource, p.ambienceClip, p.ambienceVolume);
        if (musicSource) ApplyAudioInstant(musicSource, p.musicClip, p.musicVolume);
        if (sfxSource) ApplyAudioInstant(sfxSource, p.sfxClip, p.sfxVolume);

        if (logTransitions)
            Debug.Log($"[AudioMoodDirector] Instant mood applied: {mood}");
    }

    private IEnumerator CrossfadeRoutine(MoodId mood, float duration)
    {
        var target = GetPreset(mood);
        float t = 0f;

        // Cache current values
        float startAmbVol = ambienceSource ? ambienceSource.volume : 0f;
        float startMusVol = musicSource ? musicSource.volume : 0f;
        float startSfxVol = sfxSource ? sfxSource.volume : 0f;

        // Change clips immediately so they begin playing
        if (ambienceSource && target.ambienceClip != null)
            SwapClip(ambienceSource, target.ambienceClip);

        if (musicSource && target.musicClip != null)
            SwapClip(musicSource, target.musicClip);

        if (sfxSource && target.sfxClip != null)
            SwapClip(sfxSource, target.sfxClip);

        while (t < duration)
        {
            float lerp = t / duration;

            if (ambienceSource)
                ambienceSource.volume = Mathf.Lerp(startAmbVol, target.ambienceVolume, lerp);

            if (musicSource)
                musicSource.volume = Mathf.Lerp(startMusVol, target.musicVolume, lerp);

            if (sfxSource)
                sfxSource.volume = Mathf.Lerp(startSfxVol, target.sfxVolume, lerp);

            t += Time.deltaTime;
            yield return null;
        }

        // Final assignment
        if (ambienceSource) ambienceSource.volume = target.ambienceVolume;
        if (musicSource) musicSource.volume = target.musicVolume;
        if (sfxSource) sfxSource.volume = target.sfxVolume;

        currentPreset = target;
        fadeRoutine = null;

        if (logTransitions)
            Debug.Log($"[AudioMoodDirector] Crossfaded to mood: {mood}");
    }

    private void ApplyAudioInstant(AudioSource src, AudioClip clip, float volume)
    {
        if (!src) return;

        if (clip != null)
        {
            src.clip = clip;
            src.loop = true;
            src.Play();
        }

        src.volume = volume;
    }

    private void SwapClip(AudioSource src, AudioClip newClip)
    {
        if (newClip == null) return;

        if (src.clip != newClip)
        {
            src.clip = newClip;
            src.loop = true;
            src.Play();
        }
    }
}
