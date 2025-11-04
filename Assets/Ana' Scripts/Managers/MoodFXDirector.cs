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
        [Header("Color")]
        [Range(-2f, 2f)] public float exposure;   // EV100
        [Range(-100f, 100f)] public float contrast;
        [Range(-100f, 100f)] public float saturation;
        [ColorUsage(false, true)] public Color colorFilter;

        [Header("Vignette")]
        [Range(0f, 1f)] public float vignetteIntensity;
        [Range(0f, 1f)] public float vignetteSmoothness;

        [Header("Bloom")]
        [Range(0f, 10f)] public float bloomIntensity;

        [Header("Tone (Lift/Gamma/Gain)")]
        public Color lift;   // shadows
        public Color gamma;  // midtones
        public Color gain;   // highlights
    }

    // ---------- Inspector ----------
    [Header("Volume (assign OR leave empty to auto-find/create)")]
    public Volume targetVolume;
    public VolumeProfile profileOverride;

    [Header("Blending")]
    [Tooltip("Default cross-fade time used by CrossfadeTo if none is specified.")]
    public float defaultBlendSeconds = 1.5f;
    public AnimationCurve blendCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Initial Mood")]
    public MoodId startMood = MoodId.Neutral;

    [Header("Presets")]
    public MoodPreset Neutral;
    public MoodPreset Happiness;
    public MoodPreset Sadness;
    public MoodPreset Nostalgic;
    public MoodPreset Furious;
    public MoodPreset Triggered;

    // ---------- Cache ----------
    ColorAdjustments _colorAdj;
    Vignette _vignette;
    Bloom _bloom;
    LiftGammaGain _lgg;

    Coroutine _fadeCo;

    void OnEnable()
    {
        EnsureVolume();
        ApplyCurrentMoodImmediate(startMood);
    }

    void OnValidate()
    {
        EnsureVolume();
    }

    // ---------- Public API used by StoryGameManager ----------

    /// <summary>Cross-fade from current volume values to the given mood.</summary>
    public void CrossfadeTo(MoodId mood, float seconds)
    {
        var target = GetPreset(mood);
        CrossfadeTo(target, seconds <= 0 ? defaultBlendSeconds : seconds);
    }

    /// <summary>
    /// Blend two moods by weight (0..1) and cross-fade to that mixed preset.
    /// Used by overlays: baseMood + overlayMood * weight.
    /// </summary>
    public void CrossfadeBlend(MoodId baseMood, MoodId overlayMood, float overlayWeight, float microFadeSeconds)
    {
        overlayWeight = Mathf.Clamp01(overlayWeight);
        var a = GetPreset(baseMood);
        var b = GetPreset(overlayMood);
        var mixed = LerpPreset(a, b, overlayWeight);
        CrossfadeTo(mixed, Mathf.Max(0.01f, microFadeSeconds));
    }

    /// <summary>For editor/testing: apply the selected mood instantly (no fade).</summary>
    public void ApplyCurrentMood()
    {
        ApplyCurrentMoodImmediate(startMood);
    }

    // ---------- Internals ----------

    void EnsureVolume()
    {
        if (!targetVolume)
        {
            // Try find an existing Volume on this GO or parent (e.g., CenterEyeAnchor)
            targetVolume = GetComponent<Volume>();
            if (!targetVolume) targetVolume = GetComponentInParent<Volume>();
            if (!targetVolume)
            {
                targetVolume = gameObject.AddComponent<Volume>();
                targetVolume.isGlobal = true;
            }
        }

        if (profileOverride)
        {
            targetVolume.profile = profileOverride;
        }
        if (!targetVolume.profile)
        {
            targetVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }

        // Ensure required overrides exist
        var p = targetVolume.profile;
        if (!p.TryGet(out _colorAdj)) { _colorAdj = p.Add<ColorAdjustments>(true); }
        if (!p.TryGet(out _vignette)) { _vignette = p.Add<Vignette>(true); }
        if (!p.TryGet(out _bloom)) { _bloom = p.Add<Bloom>(true); }
        if (!p.TryGet(out _lgg)) { _lgg = p.Add<LiftGammaGain>(true); }
    }

    MoodPreset GetPreset(MoodId id) => id switch
    {
        MoodId.Happiness => Happiness,
        MoodId.Sadness => Sadness,
        MoodId.Nostalgic => Nostalgic,
        MoodId.Furious => Furious,
        MoodId.Triggered => Triggered,
        _ => Neutral
    };

    void ApplyCurrentMoodImmediate(MoodId id)
    {
        EnsureVolume();
        var m = GetPreset(id);
        ApplyToVolume(m);
    }

    void CrossfadeTo(MoodPreset target, float seconds)
    {
        EnsureVolume();

        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeRoutine(ReadFromVolume(), target, Mathf.Max(0.0001f, seconds)));
    }

    IEnumerator FadeRoutine(MoodPreset from, MoodPreset to, float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            float u = (seconds <= 0f) ? 1f : Mathf.Clamp01(t / seconds);
            float w = blendCurve != null ? Mathf.Clamp01(blendCurve.Evaluate(u)) : u;

            var mix = LerpPreset(from, to, w);
            ApplyToVolume(mix);

            // In Edit Mode, yield one editor frame
            if (!Application.isPlaying) yield return null;
            else yield return null;

            t += (Application.isPlaying ? Time.deltaTime : 0.016f);
        }

        ApplyToVolume(to);
        _fadeCo = null;
    }

    MoodPreset ReadFromVolume()
    {
        MoodPreset m = default;

        if (_colorAdj != null)
        {
            m.exposure = _colorAdj.postExposure.value;
            m.contrast = _colorAdj.contrast.value;
            m.saturation = _colorAdj.saturation.value;
            m.colorFilter = _colorAdj.colorFilter.value;
        }

        if (_vignette != null)
        {
            m.vignetteIntensity = _vignette.intensity.value;
            m.vignetteSmoothness = _vignette.smoothness.value;
        }

        if (_bloom != null)
        {
            m.bloomIntensity = _bloom.intensity.value;
        }

        if (_lgg != null)
        {
            m.lift = FromVec4(_lgg.lift.value);
            m.gamma = FromVec4(_lgg.gamma.value);
            m.gain = FromVec4(_lgg.gain.value);
        }

        return m;
    }

    void ApplyToVolume(MoodPreset p)
    {
        if (_colorAdj != null)
        {
            _colorAdj.postExposure.Override(p.exposure);
            _colorAdj.contrast.Override(p.contrast);
            _colorAdj.saturation.Override(p.saturation);
            _colorAdj.colorFilter.Override(p.colorFilter);
        }
        if (_vignette != null)
        {
            _vignette.intensity.Override(p.vignetteIntensity);
            _vignette.smoothness.Override(p.vignetteSmoothness);
        }
        if (_bloom != null)
        {
            _bloom.intensity.Override(p.bloomIntensity);
        }
        if (_lgg != null)
        {
            _lgg.lift.Override(ToVec4(p.lift));
            _lgg.gamma.Override(ToVec4(p.gamma));
            _lgg.gain.Override(ToVec4(p.gain));
        }
    }

    static MoodPreset LerpPreset(MoodPreset a, MoodPreset b, float t)
    {
        MoodPreset r;
        r.exposure = Mathf.Lerp(a.exposure, b.exposure, t);
        r.contrast = Mathf.Lerp(a.contrast, b.contrast, t);
        r.saturation = Mathf.Lerp(a.saturation, b.saturation, t);
        r.colorFilter = Color.Lerp(a.colorFilter, b.colorFilter, t);

        r.vignetteIntensity = Mathf.Lerp(a.vignetteIntensity, b.vignetteIntensity, t);
        r.vignetteSmoothness = Mathf.Lerp(a.vignetteSmoothness, b.vignetteSmoothness, t);

        r.bloomIntensity = Mathf.Lerp(a.bloomIntensity, b.bloomIntensity, t);

        r.lift = Color.Lerp(a.lift, b.lift, t);
        r.gamma = Color.Lerp(a.gamma, b.gamma, t);
        r.gain = Color.Lerp(a.gain, b.gain, t);

        return r;
    }

    static Vector4 ToVec4(Color c) => new Vector4(c.r, c.g, c.b, 0f);
    static Color FromVec4(Vector4 v) => new Color(v.x, v.y, v.z, 1f);

    // --- QUICK PRESET AUTOFILL ---
    [ContextMenu("Auto Populate Presets")]
    public void AutoPopulatePresets()
    {
        Neutral = new MoodPreset
        {
            exposure = 0f,
            contrast = 0f,
            saturation = 0f,
            colorFilter = Color.white,
            vignetteIntensity = 0.18f,
            vignetteSmoothness = 0.40f,
            bloomIntensity = 0.30f,
            lift = RGB(0f, 0f, 0f),
            gamma = RGB(1f, 1f, 1f),
            gain = RGB(1f, 1f, 1f)
        };

        Happiness = new MoodPreset
        {
            exposure = 0.5f,
            contrast = 10f,
            saturation = 20f,
            colorFilter = Hex("#FFF2E0"),
            vignetteIntensity = 0.10f,
            vignetteSmoothness = 0.35f,
            bloomIntensity = 1.20f,
            lift = RGB(0f, 0f, 0f),
            gamma = RGB(1.06f, 1.06f, 1.02f),
            gain = RGB(1.06f, 1.06f, 1.02f)
        };

        Sadness = new MoodPreset
        {
            exposure = -0.30f,
            contrast = -10f,
            saturation = -35f,
            colorFilter = Hex("#DDE6FF"),
            vignetteIntensity = 0.25f,
            vignetteSmoothness = 0.55f,
            bloomIntensity = 0.10f,
            lift = RGB(0.03f, 0.03f, 0.04f),
            gamma = RGB(0.96f, 0.96f, 1.00f),
            gain = RGB(0.98f, 0.98f, 1.00f)
        };

        Nostalgic = new MoodPreset
        {
            exposure = 0f,
            contrast = -5f,
            saturation = -25f,
            colorFilter = Hex("#F3E2C0"),
            vignetteIntensity = 0.22f,
            vignetteSmoothness = 0.50f,
            bloomIntensity = 0.40f,
            lift = RGB(0.02f, 0.015f, 0.00f),
            gamma = RGB(1.02f, 0.98f, 0.94f),
            gain = RGB(1.03f, 0.99f, 0.95f)
        };

        Furious = new MoodPreset
        {
            exposure = 0.20f,
            contrast = 25f,
            saturation = 15f,
            colorFilter = Hex("#FF4D40"),
            vignetteIntensity = 0.30f,
            vignetteSmoothness = 0.60f,
            bloomIntensity = 0.20f,
            lift = RGB(0.00f, 0.00f, 0.00f),
            gamma = RGB(1.05f, 0.95f, 0.90f),
            gain = RGB(1.10f, 0.90f, 0.85f)
        };

        Triggered = new MoodPreset
        {
            exposure = 0.10f,
            contrast = 40f,
            saturation = -10f,
            colorFilter = Hex("#F2FFFF"),
            vignetteIntensity = 0.40f,
            vignetteSmoothness = 0.85f,
            bloomIntensity = 0.00f,
            lift = RGB(0.02f, 0.02f, 0.02f),
            gamma = RGB(0.95f, 0.98f, 1.02f),
            gain = RGB(1.02f, 1.02f, 1.02f)
        };

        // push the current start mood so you see the effect immediately
        ApplyCurrentMoodImmediate(startMood);
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    static Color Hex(string hex)
    {
        if (!ColorUtility.TryParseHtmlString(hex, out var c)) c = Color.white;
        c.a = 1f; return c;
    }
    static Color RGB(float r, float g, float b) => new Color(r, g, b, 1f);

}
