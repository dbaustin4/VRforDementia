using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
[RequireComponent(typeof(Volume))]
public class VisualMoodDirector : MonoBehaviour
{
    // ---------------------------------------------------------
    // ENUM
    // ---------------------------------------------------------
    public enum MoodId
    {
        Neutral,
        Happiness,
        Sadness,
        Nostalgic,
        Furious,
        Triggered
    }

    // ---------------------------------------------------------
    // PRESET STRUCT
    // ---------------------------------------------------------
    [Serializable]
    public struct MoodPreset
    {
        [Header("Color Adjustments")]
        public float exposure;
        public float contrast;
        public float saturation;
        public Color colorFilter;

        [Header("Vignette")]
        public float vignetteIntensity;
        public float vignetteSmoothness;

        [Header("Bloom")]
        public float bloomIntensity;

        [Header("Lift / Gamma / Gain")]
        public Vector4 lift;
        public Vector4 gamma;
        public Vector4 gain;
    }

    // ---------------------------------------------------------
    // INSPECTOR
    // ---------------------------------------------------------
    [Header("Master Controls")]
    [Range(0.1f, 2f)] public float targetVolume = 1.0f;
    [Range(0f, 2f)] public float masterIntensity = 1.0f;
    public MoodId initialMood = MoodId.Neutral;

    [Header("Presets")]
    public MoodPreset Neutral;
    public MoodPreset Happiness;
    public MoodPreset Sadness;
    public MoodPreset Nostalgic;
    public MoodPreset Furious;
    public MoodPreset Triggered;

    [Header("Debug")]
    public MoodId selectedMood = MoodId.Neutral;
    public bool logTransitions = false;

    // ---------------------------------------------------------
    // RUNTIME
    // ---------------------------------------------------------
    private Volume _volume;

    private ColorAdjustments _colorAdj;
    private Vignette _vignette;
    private Bloom _bloom;
    private LiftGammaGain _liftGammaGain;

    private MoodPreset _currentPreset;
    private Coroutine _fadeRoutine;

    // ---------------------------------------------------------
    // UNITY
    // ---------------------------------------------------------
    private void Reset()
    {
        CacheVolume();
        GenerateDefaultPresets();
        ApplyPresetInstant(Neutral);
    }

    private void Awake()
    {
        CacheVolume();

        if (AllPresetsAreEmpty())
            GenerateDefaultPresets();

        _currentPreset = GetPreset(initialMood);
        ApplyPresetInstant(_currentPreset);
    }

    // ---------------------------------------------------------
    // PUBLIC API
    // ---------------------------------------------------------
    public void ForceInstantApply(MoodId mood)
    {
        // make sure volume refs are valid even in edit mode
        CacheVolume();

        _currentPreset = GetPreset(mood);
        ApplyPresetInstant(_currentPreset);

        if (logTransitions)
            Debug.Log($"[VisualMoodDirector] Instant mood applied: {mood}");
    }

    public void CrossfadeTo(MoodId mood, float seconds)
    {
        // make sure volume refs are valid even in edit mode
        CacheVolume();

        if (seconds <= 0f)
        {
            ForceInstantApply(mood);
            return;
        }

        var target = GetPreset(mood);

        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _fadeRoutine = StartCoroutine(FadeRoutine(_currentPreset, target, seconds));

        if (logTransitions)
            Debug.Log($"[VisualMoodDirector] Crossfading to {mood} over {seconds}s");
    }

    public MoodId GetCurrentMood() => selectedMood;

    // ---------------------------------------------------------
    // COMPATIBILITY WRAPPERS (old scripts)
    // ---------------------------------------------------------
    public void forceInstantApply(MoodId mood)
    {
        ForceInstantApply(mood); // old → new
    }

    public void crossfadeTo(MoodId mood, float seconds)
    {
        CrossfadeTo(mood, seconds); // old → new
    }

    // ---------------------------------------------------------
    // INTERNAL HELPERS
    // ---------------------------------------------------------
    private void CacheVolume()
    {
        if (_volume == null)
            _volume = GetComponent<Volume>();

        if (_volume == null || _volume.profile == null)
            return;

        _volume.profile.TryGet(out _colorAdj);
        _volume.profile.TryGet(out _vignette);
        _volume.profile.TryGet(out _bloom);
        _volume.profile.TryGet(out _liftGammaGain);
    }

    private MoodPreset GetPreset(MoodId m)
    {
        return m switch
        {
            MoodId.Happiness => Happiness,
            MoodId.Sadness => Sadness,
            MoodId.Nostalgic => Nostalgic,
            MoodId.Furious => Furious,
            MoodId.Triggered => Triggered,
            _ => Neutral
        };
    }

    private void ApplyPresetInstant(MoodPreset p)
    {
        ApplyBlendedPreset(p, 1f);
    }

    private IEnumerator FadeRoutine(MoodPreset from, MoodPreset to, float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            float lerp = t / duration;
            ApplyBlendedPreset(LerpPresets(from, to, lerp), 1f);
            t += Time.deltaTime;
            yield return null;
        }

        ApplyBlendedPreset(to, 1f);
        _currentPreset = to;
        _fadeRoutine = null;
    }

    private MoodPreset LerpPresets(MoodPreset a, MoodPreset b, float t)
    {
        MoodPreset r = new MoodPreset();

        r.exposure = Mathf.Lerp(a.exposure, b.exposure, t);
        r.contrast = Mathf.Lerp(a.contrast, b.contrast, t);
        r.saturation = Mathf.Lerp(a.saturation, b.saturation, t);
        r.colorFilter = Color.Lerp(a.colorFilter, b.colorFilter, t);

        r.vignetteIntensity = Mathf.Lerp(a.vignetteIntensity, b.vignetteIntensity, t);
        r.vignetteSmoothness = Mathf.Lerp(a.vignetteSmoothness, b.vignetteSmoothness, t);

        r.bloomIntensity = Mathf.Lerp(a.bloomIntensity, b.bloomIntensity, t);

        r.lift = Vector4.Lerp(a.lift, b.lift, t);
        r.gamma = Vector4.Lerp(a.gamma, b.gamma, t);
        r.gain = Vector4.Lerp(a.gain, b.gain, t);

        return r;
    }

    private void ApplyBlendedPreset(MoodPreset p, float blend)
    {
        if (_volume == null || _volume.profile == null)
            return;

        float m = masterIntensity * blend;
        float exp = p.exposure * targetVolume;

        if (_colorAdj != null)
        {
            _colorAdj.active = true;
            _colorAdj.postExposure.Override(exp);
            _colorAdj.contrast.Override(p.contrast * m);
            _colorAdj.saturation.Override(p.saturation * m);
            _colorAdj.colorFilter.Override(Color.Lerp(Color.white, p.colorFilter, m));
        }

        if (_vignette != null)
        {
            _vignette.active = true;
            _vignette.intensity.Override(p.vignetteIntensity * m);
            _vignette.smoothness.Override(p.vignetteSmoothness);
        }

        if (_bloom != null)
        {
            _bloom.active = true;
            _bloom.intensity.Override(p.bloomIntensity * m);
        }

        if (_liftGammaGain != null)
        {
            _liftGammaGain.active = true;
            _liftGammaGain.lift.Override(p.lift);
            _liftGammaGain.gamma.Override(p.gamma);
            _liftGammaGain.gain.Override(p.gain);
        }
    }

    private bool AllPresetsAreEmpty()
    {
        return Neutral.colorFilter == default &&
               Happiness.colorFilter == default &&
               Sadness.colorFilter == default &&
               Nostalgic.colorFilter == default &&
               Furious.colorFilter == default &&
               Triggered.colorFilter == default;
    }

    // ---------------------------------------------------------
    // DEFAULT AUTOMATIC PRESETS
    // ---------------------------------------------------------
    [ContextMenu("Generate Default Mood Presets")]
    public void GenerateDefaultPresets()
    {
        Neutral = MakeNeutral();
        Happiness = MakeHappiness();
        Sadness = MakeSadness();
        Nostalgic = MakeNostalgic();
        Furious = MakeFurious();
        Triggered = MakeTriggered();
    }

    private MoodPreset MakeNeutral()
    {
        return new MoodPreset
        {
            exposure = 0f,
            contrast = 0f,
            saturation = 0f,
            colorFilter = Color.white,
            vignetteIntensity = 0.15f,
            vignetteSmoothness = 0.3f,
            bloomIntensity = 0.3f,
            lift = Vector4.zero,
            gamma = Vector4.zero,
            gain = Vector4.zero
        };
    }

    private MoodPreset MakeHappiness()
    {
        return new MoodPreset
        {
            exposure = 0.4f,
            contrast = 10f,
            saturation = 20f,
            colorFilter = new Color(1f, 0.95f, 0.85f),
            vignetteIntensity = 0.1f,
            vignetteSmoothness = 0.25f,
            bloomIntensity = 1.1f,
            lift = new Vector4(-0.02f, -0.02f, -0.02f, 0),
            gamma = new Vector4(0.05f, 0.05f, 0.05f, 0),
            gain = new Vector4(0.05f, 0.05f, 0.05f, 0)
        };
    }

    private MoodPreset MakeSadness()
    {
        return new MoodPreset
        {
            exposure = -0.3f,
            contrast = -12f,
            saturation = -35f,
            colorFilter = new Color(0.75f, 0.85f, 1f),
            vignetteIntensity = 0.4f,
            vignetteSmoothness = 0.45f,
            bloomIntensity = 0.1f,
            lift = new Vector4(-0.05f, -0.05f, -0.05f, 0),
            gamma = new Vector4(-0.05f, -0.05f, -0.05f, 0),
            gain = new Vector4(-0.02f, -0.02f, -0.02f, 0)
        };
    }

    private MoodPreset MakeNostalgic()
    {
        return new MoodPreset
        {
            exposure = 0.1f,
            contrast = -5f,
            saturation = -10f,
            colorFilter = new Color(1f, 0.9f, 0.7f),
            vignetteIntensity = 0.3f,
            vignetteSmoothness = 0.4f,
            bloomIntensity = 0.7f,
            lift = new Vector4(0.03f, 0.02f, 0, 0),
            gamma = new Vector4(0.02f, 0.01f, -0.01f, 0),
            gain = new Vector4(0.01f, 0.01f, 0, 0)
        };
    }

    private MoodPreset MakeFurious()
    {
        return new MoodPreset
        {
            exposure = 0.2f,
            contrast = 18f,
            saturation = 10f,
            colorFilter = new Color(1f, 0.6f, 0.6f),
            vignetteIntensity = 0.55f,
            vignetteSmoothness = 0.5f,
            bloomIntensity = 0.8f,
            lift = new Vector4(-0.1f, -0.05f, -0.05f, 0),
            gamma = new Vector4(0.1f, 0.05f, 0, 0),
            gain = new Vector4(0.1f, 0.05f, 0, 0)
        };
    }

    private MoodPreset MakeTriggered()
    {
        return new MoodPreset
        {
            exposure = -0.2f,
            contrast = 25f,
            saturation = -40f,
            colorFilter = Color.white,
            vignetteIntensity = 0.7f,
            vignetteSmoothness = 0.7f,
            bloomIntensity = 1.5f,
            lift = new Vector4(-0.15f, -0.15f, -0.15f, 0),
            gamma = new Vector4(0.15f, 0.15f, 0.15f, 0),
            gain = new Vector4(0.15f, 0.15f, 0.15f, 0)
        };
    }
}
