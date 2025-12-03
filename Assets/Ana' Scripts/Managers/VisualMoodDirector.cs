using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Visual mood director:
/// - 6 mood presets (Neutral, Happiness, Sadness, Nostalgic, Furious, Triggered)
/// - Each preset controls exposure/contrast/saturation/color, vignette, bloom, lift/gamma/gain
/// - Supports instant apply, crossfade, and blend (base + overlay)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Volume))]
public class VisualMoodDirector : MonoBehaviour
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
    public struct MoodPreset
    {
        [Header("Color Adjustments")]
        public float exposure;
        public float contrast;
        public float saturation;
        public Color colorFilter;

        [Header("Vignette")]
        [Range(0f, 1f)] public float vignetteIntensity;
        [Range(0.05f, 1f)] public float vignetteSmoothness;

        [Header("Bloom")]
        public float bloomIntensity;

        [Header("Lift / Gamma / Gain")]
        public Vector4 lift;
        public Vector4 gamma;
        public Vector4 gain;
    }

    // ---------- Inspector ----------

    [Header("Master Controls")]
    [Tooltip("Extra global multiplier for exposure-like effects.")]
    public float targetVolume = 1f;

    [Tooltip("Scales overall effect intensity (0 = off, 1 = full).")]
    [Range(0f, 1f)] public float MasterIntensity = 1f;

    [Tooltip("Mood applied on Start().")]
    public MoodId InitialMood = MoodId.Neutral;

    [SerializeField, Tooltip("Selected mood in inspector (used by editor buttons).")]
    private MoodId _selectedMood = MoodId.Neutral;

    [Header("Mood Presets (Visual Packages)")]
    public MoodPreset Neutral;
    public MoodPreset Happiness;
    public MoodPreset Sadness;
    public MoodPreset Nostalgic;
    public MoodPreset Furious;
    public MoodPreset Triggered;

    [Header("Debug")]
    public bool logTransitions = false;

    // ---------- Private state ----------

    private Volume _volume;
    private ColorAdjustments _colorAdj;
    private Vignette _vignette;
    private Bloom _bloom;
    private LiftGammaGain _liftGammaGain;

    private MoodPreset _currentPreset;
    private Coroutine _activeRoutine;

    // ---------- Unity ----------

    private void Awake()
    {
        _volume = GetComponent<Volume>();
        CacheVolumeComponents();

        _currentPreset = GetPreset(InitialMood);
        ApplyPresetInstant(_currentPreset);
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            _volume = GetComponent<Volume>();
            CacheVolumeComponents();

            var preset = GetPreset(_selectedMood);
            _currentPreset = preset;
            ApplyPresetToVolume(preset, MasterIntensity);
        }
    }

    // ---------- Public API (used by editor + other scripts) ----------

    // called from custom inspector
    public void SetSelectedMood(MoodId mood)
    {
        _selectedMood = mood;
    }

    // called from custom inspector
    public void forceInstantApply()
    {
        forceInstantApply(_selectedMood);
    }

    public void forceInstantApply(MoodId mood)
    {
        var preset = GetPreset(mood);
        _currentPreset = preset;
        ApplyPresetInstant(preset);

        if (logTransitions)
            Debug.Log($"[VisualMoodDirector] Instant apply mood: {mood}");
    }

    /// <summary>
    /// Crossfade from current mood to target over fadeSeconds.
    /// </summary>
    public void CrossfadeTo(MoodId mood, float fadeSeconds)
    {
        if (fadeSeconds <= 0.001f)
        {
            forceInstantApply(mood);
            return;
        }

        var toPreset = GetPreset(mood);

        if (_activeRoutine != null)
            StopCoroutine(_activeRoutine);

        _activeRoutine = StartCoroutine(CrossfadeRoutine(_currentPreset, toPreset, fadeSeconds));

        if (logTransitions)
            Debug.Log($"[VisualMoodDirector] CrossfadeTo: {mood} over {fadeSeconds:0.00}s");
    }

    /// <summary>
    /// Blend between a base mood and overlay mood by weight (0..1),
    /// then micro-fade to that blended preset.
    /// This is used by NarrativeDirector for overlay programs.
    /// </summary>
    public void CrossfadeBlend(MoodId baseMood, MoodId overlayMood, float overlayWeight, float microFadeSeconds)
    {
        overlayWeight = Mathf.Clamp01(overlayWeight);

        var basePreset = GetPreset(baseMood);
        var overlayPreset = GetPreset(overlayMood);

        var blended = LerpPresets(basePreset, overlayPreset, overlayWeight);

        if (microFadeSeconds <= 0.001f)
        {
            ApplyPresetInstant(blended);
            return;
        }

        if (_activeRoutine != null)
            StopCoroutine(_activeRoutine);

        _activeRoutine = StartCoroutine(CrossfadeRoutine(_currentPreset, blended, microFadeSeconds));

        if (logTransitions)
            Debug.Log($"[VisualMoodDirector] Blend base={baseMood}, overlay={overlayMood}, w={overlayWeight:0.00}, microFade={microFadeSeconds:0.00}");
    }

    /// <summary>
    /// Change global strength of the current mood (0..1).
    /// </summary>
    public void SetMasterIntensity(float intensity)
    {
        MasterIntensity = Mathf.Clamp01(intensity);
        ApplyPresetToVolume(_currentPreset, MasterIntensity);
    }

    // ---------- Core logic ----------

    private void CacheVolumeComponents()
    {
        if (_volume == null || _volume.profile == null) return;

        _volume.profile.TryGet(out _colorAdj);
        _volume.profile.TryGet(out _vignette);
        _volume.profile.TryGet(out _bloom);
        _volume.profile.TryGet(out _liftGammaGain);
    }

    private MoodPreset GetPreset(MoodId mood)
    {
        return mood switch
        {
            MoodId.Happiness => Happiness,
            MoodId.Sadness => Sadness,
            MoodId.Nostalgic => Nostalgic,
            MoodId.Furious => Furious,
            MoodId.Triggered => Triggered,
            _ => Neutral
        };
    }

    private void ApplyPresetInstant(MoodPreset preset)
    {
        _currentPreset = preset;
        ApplyPresetToVolume(preset, MasterIntensity);
    }

    private IEnumerator CrossfadeRoutine(MoodPreset from, MoodPreset to, float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            float u = Mathf.Clamp01(t / duration);
            float ease = Mathf.SmoothStep(0f, 1f, u);

            var blended = LerpPresets(from, to, ease);
            _currentPreset = blended;
            ApplyPresetToVolume(blended, MasterIntensity);

            t += Time.deltaTime;
            yield return null;
        }

        _currentPreset = to;
        ApplyPresetToVolume(to, MasterIntensity);
        _activeRoutine = null;
    }

    private MoodPreset LerpPresets(MoodPreset a, MoodPreset b, float t)
    {
        MoodPreset p = new MoodPreset
        {
            exposure = Mathf.Lerp(a.exposure, b.exposure, t),
            contrast = Mathf.Lerp(a.contrast, b.contrast, t),
            saturation = Mathf.Lerp(a.saturation, b.saturation, t),
            colorFilter = Color.Lerp(a.colorFilter, b.colorFilter, t),

            vignetteIntensity = Mathf.Lerp(a.vignetteIntensity, b.vignetteIntensity, t),
            vignetteSmoothness = Mathf.Lerp(a.vignetteSmoothness, b.vignetteSmoothness, t),

            bloomIntensity = Mathf.Lerp(a.bloomIntensity, b.bloomIntensity, t),

            lift = Vector4.Lerp(a.lift, b.lift, t),
            gamma = Vector4.Lerp(a.gamma, b.gamma, t),
            gain = Vector4.Lerp(a.gain, b.gain, t)
        };

        return p;
    }

    private void ApplyPresetToVolume(MoodPreset preset, float masterIntensity)
    {
        if (_volume == null || _volume.profile == null) return;

        float m = Mathf.Clamp01(masterIntensity);

        // Color Adjustments
        if (_colorAdj != null)
        {
            _colorAdj.postExposure.Override(preset.exposure * m * targetVolume);
            _colorAdj.contrast.Override(preset.contrast * m);
            _colorAdj.saturation.Override(preset.saturation * m);
            _colorAdj.colorFilter.Override(Color.Lerp(Color.white, preset.colorFilter, m));
        }

        // Vignette
        if (_vignette != null)
        {
            _vignette.intensity.Override(preset.vignetteIntensity * m);
            _vignette.smoothness.Override(preset.vignetteSmoothness);
        }

        // Bloom
        if (_bloom != null)
        {
            _bloom.intensity.Override(preset.bloomIntensity * m);
        }

        // Lift / Gamma / Gain
        if (_liftGammaGain != null)
        {
            _liftGammaGain.lift.Override(Vector4.Lerp(Vector4.zero, preset.lift, m));
            _liftGammaGain.gamma.Override(Vector4.Lerp(Vector4.zero, preset.gamma, m));
            _liftGammaGain.gain.Override(Vector4.Lerp(Vector4.zero, preset.gain, m));
        }
    }
}
