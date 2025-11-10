using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
[ExecuteAlways]
public class MoodFXDirector : MonoBehaviour
{
    // ---------- Types ----------
    public enum MoodId { Neutral, Happiness, Sadness, Nostalgic, Furious, Triggered }

    [Serializable]
    public struct MoodPreset
    {
        [Header("Color Adjustments")]
        [Range(-2f, 2f)] public float exposure;
        [Range(-100f, 100f)] public float contrast;
        [Range(-100f, 100f)] public float saturation;
        [ColorUsage(false, true)] public Color colorFilter;

        [Header("Vignette")]
        [Range(0f, 1f)] public float vignetteIntensity;
        [Range(0f, 1f)] public float vignetteSmoothness;

        [Header("Bloom")]
        [Range(0f, 10f)] public float bloomIntensity;

        [Header("Tone (Lift/Gamma/Gain)")]
        public Color lift;
        public Color gamma;
        public Color gain;
    }

    // ---------- Inspector ----------
    [Header("Volume (assign OR leave empty to auto-find/create)")]
    public Volume targetVolume;

    [Range(0f, 1f)] public float MasterIntensity = 0.55f;
    public MoodId InitialMood = MoodId.Happiness;
    [SerializeField] private MoodId _selectedMood = MoodId.Happiness;

    [Header("Presets")]
    public MoodPreset Neutral;
    public MoodPreset Happiness;
    public MoodPreset Sadness;
    public MoodPreset Nostalgic;
    public MoodPreset Furious;
    public MoodPreset Triggered;

    // ---------- State ----------
    private ColorAdjustments colorAdj;
    private Vignette vignette;
    private Bloom bloom;
    private LiftGammaGain liftGammaGain;
    private Coroutine blendRoutine;

    // ---------- Lifecycle ----------
    private void OnEnable()
    {
        EnsureVolume();
        CacheOverrides();
        if (!Application.isPlaying)
            ApplyMoodInstant(InitialMood);
    }

    private void Reset()
    {
        // Neutral baseline
        Neutral = new MoodPreset
        {
            exposure = 0f,
            contrast = 0f,
            saturation = 0f,
            colorFilter = Color.white,
            vignetteIntensity = 0.1f,
            vignetteSmoothness = 0.25f,
            bloomIntensity = 0.3f,
            lift = new Color(0f, 0f, 0f, 0f),
            gamma = new Color(0f, 0f, 0f, 0f),
            gain = new Color(0f, 0f, 0f, 0f)
        };

        // Bright and warm
        Happiness = Neutral;
        Happiness.saturation = 20f;
        Happiness.exposure = 0.2f;
        Happiness.bloomIntensity = 0.6f;
        Happiness.colorFilter = new Color(1.05f, 1.02f, 0.95f, 1f);

        // Cool and muted
        Sadness = Neutral;
        Sadness.saturation = -30f;
        Sadness.exposure = -0.2f;
        Sadness.vignetteIntensity = 0.22f;
        Sadness.colorFilter = new Color(0.85f, 0.9f, 1.05f, 1f);

        // Slightly faded, nostalgic warmth
        Nostalgic = Neutral;
        Nostalgic.saturation = -10f;
        Nostalgic.contrast = -10f;
        Nostalgic.colorFilter = new Color(1.02f, 0.98f, 0.9f, 1f);

        // Passionate, high energy
        Furious = Neutral;
        Furious.contrast = 25f;
        Furious.saturation = 10f;
        Furious.colorFilter = new Color(1.07f, 0.92f, 0.9f, 1f);
        Furious.vignetteIntensity = 0.28f;

        // Subtle anxious/triggered tone — slightly sharper, cooler, tighter
        Triggered = Neutral;
        Triggered.contrast = 30f;              // slightly higher contrast
        Triggered.saturation = -25f;           // lightly desaturated
        Triggered.exposure = -0.25f;           // touch darker
        Triggered.vignetteIntensity = 0.30f;   // subtle tunnel vision
        Triggered.bloomIntensity = 0.2f;       // less bloom, less warmth
        Triggered.colorFilter = new Color(0.92f, 0.96f, 1.05f, 1f); // soft cool tint
        Triggered.lift = new Color(-0.02f, -0.02f, -0.02f, 0f);
        Triggered.gamma = new Color(0.01f, 0.01f, 0.01f, 0f);
        Triggered.gain = new Color(0f, 0f, 0f, 0f);
    }

    private void OnValidate()
    {
        if (targetVolume == null || colorAdj == null)
        {
            EnsureVolume();
            CacheOverrides();
        }
        if (!Application.isPlaying && targetVolume != null)
            ApplyMoodInstant(InitialMood);
    }

    // ---------- Public API ----------
    public MoodId SelectedMood => _selectedMood;
    public void SetSelectedMood(MoodId mood) => _selectedMood = mood;
    public void ApplySelectedMoodInstant() => ApplyMoodInstant(_selectedMood);

    public void ApplyMoodInstant(MoodId mood)
    {
        StopAllBlends();
        var basePreset = Neutral;
        var target = GetPreset(mood);
        SetMoodValues(basePreset, target, 1f);
    }

    public void CrossfadeTo(MoodId mood, float duration = 1f)
    {
        StopAllBlends();
        blendRoutine = StartCoroutine(BlendToMood(mood, Mathf.Max(0.0001f, duration)));
    }

    public void forceInstantApply() => ApplySelectedMoodInstant();
    public void forceInstantApply(MoodId mood) => ApplyMoodInstant(mood);

    // ---------- Internals ----------
    private void EnsureVolume()
    {
        if (targetVolume != null) return;

        var found = FindFirstObjectByType<Volume>();
        if (found != null) targetVolume = found;

        if (targetVolume == null)
        {
            Transform parent = transform;
            var centerEye = GameObject.Find("CenterEyeAnchor");
            if (centerEye != null) parent = centerEye.transform;

            var go = new GameObject("Global Volume");
            go.transform.SetParent(parent, false);
            targetVolume = go.AddComponent<Volume>();
            targetVolume.isGlobal = true;
            targetVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }

        if (targetVolume.profile == null)
            targetVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
    }

    private void CacheOverrides()
    {
        if (targetVolume == null || targetVolume.profile == null) return;

        if (!targetVolume.profile.TryGet(out colorAdj))
            colorAdj = targetVolume.profile.Add<ColorAdjustments>(true);
        if (!targetVolume.profile.TryGet(out vignette))
            vignette = targetVolume.profile.Add<Vignette>(true);
        if (!targetVolume.profile.TryGet(out bloom))
            bloom = targetVolume.profile.Add<Bloom>(true);
        if (!targetVolume.profile.TryGet(out liftGammaGain))
            liftGammaGain = targetVolume.profile.Add<LiftGammaGain>(true);

        colorAdj.postExposure.overrideState = true;
        colorAdj.contrast.overrideState = true;
        colorAdj.saturation.overrideState = true;
        colorAdj.colorFilter.overrideState = true;
        vignette.intensity.overrideState = true;
        vignette.smoothness.overrideState = true;
        bloom.intensity.overrideState = true;
        liftGammaGain.lift.overrideState = true;
        liftGammaGain.gamma.overrideState = true;
        liftGammaGain.gain.overrideState = true;
    }

    private IEnumerator BlendToMood(MoodId mood, float duration)
    {
        var basePreset = Neutral;
        var target = GetPreset(mood);
        float t = 0f;
        while (t < 1f)
        {
            SetMoodValues(basePreset, target, t);
            t += (Application.isPlaying ? Time.deltaTime : 0.016f) / duration;
            yield return null;
        }
        SetMoodValues(basePreset, target, 1f);
    }

    private void StopAllBlends()
    {
        if (blendRoutine != null)
            StopCoroutine(blendRoutine);
        blendRoutine = null;
    }

    private void SetMoodValues(MoodPreset neutral, MoodPreset target, float weight01)
    {
        float w = Mathf.Clamp01(weight01) * Mathf.Clamp01(MasterIntensity);

        if (colorAdj != null)
        {
            colorAdj.postExposure.value = Mathf.Lerp(neutral.exposure, target.exposure, w);
            colorAdj.contrast.value = Mathf.Lerp(neutral.contrast, target.contrast, w);
            colorAdj.saturation.value = Mathf.Lerp(neutral.saturation, target.saturation, w);
            colorAdj.colorFilter.value = Color.Lerp(neutral.colorFilter, target.colorFilter, w);
        }

        if (vignette != null)
        {
            vignette.intensity.value = Mathf.Lerp(neutral.vignetteIntensity, target.vignetteIntensity, w);
            vignette.smoothness.value = Mathf.Lerp(neutral.vignetteSmoothness, target.vignetteSmoothness, w);
        }

        if (bloom != null)
        {
            bloom.intensity.value = Mathf.Lerp(neutral.bloomIntensity, target.bloomIntensity, w);
        }

        if (liftGammaGain != null)
        {
            liftGammaGain.lift.value = Color.Lerp(neutral.lift, target.lift, w);
            liftGammaGain.gamma.value = Color.Lerp(neutral.gamma, target.gamma, w);
            liftGammaGain.gain.value = Color.Lerp(neutral.gain, target.gain, w);
        }
    }

    private MoodPreset GetPreset(MoodId mood)
    {
        switch (mood)
        {
            case MoodId.Happiness: return Happiness;
            case MoodId.Sadness: return Sadness;
            case MoodId.Nostalgic: return Nostalgic;
            case MoodId.Furious: return Furious;
            case MoodId.Triggered: return Triggered;
            default: return Neutral;
        }
    }
}
