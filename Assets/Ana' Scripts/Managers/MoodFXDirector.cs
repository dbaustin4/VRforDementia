using System;
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
        [Tooltip("Exposure (EV100). 0 = neutral")] [Range(-2f, 2f)] public float exposure;
        [Tooltip("Contrast in %")] [Range(-100f, 100f)] public float contrast;
        [Tooltip("Saturation in %")] [Range(-100f, 100f)] public float saturation;
        [ColorUsage(false, true)] public Color colorFilter;

        [Header("Vignette")]
        [Range(0f, 1f)] public float vignetteIntensity;
        [Range(0f, 1f)] public float vignetteSmoothness;

        [Header("Bloom")]
        [Range(0f, 10f)] public float bloomIntensity;

        [Header("Tone (Lift / Gamma / Gain)")]
        public Color lift;   // shadows
        public Color gamma;  // midtones
        public Color gain;   // highlights

        [Header("Misc")]
        [Tooltip("Default blend time when switching to this preset")]
        [Min(0f)] public float defaultBlendSeconds;
    }

    // ---------- Inspector ----------
    [Header("Volume (assign OR leave empty to auto-find/create)")]
    public Volume targetVolume;

    [Header("Global Controls")]
    [Tooltip("0 = identical to Neutral. 1 = full preset. Applies to ALL moods.")]
    [Range(0f, 1f)] public float masterIntensity = 0.55f;

    [Space(4)]
    [Tooltip("Mood used on Start (Play Mode) or when pressing 'Apply Selected Mood'")]
    public MoodId initialMood = MoodId.Neutral;

    [NonSerialized] public MoodId currentMood;

    [Header("Presets (you can auto-fill from the editor)")]
    public MoodPreset Neutral;
    public MoodPreset Happiness;
    public MoodPreset Sadness;
    public MoodPreset Nostalgic;
    public MoodPreset Furious;
    public MoodPreset Triggered;

    // ---------- Runtime state ----------
    ColorAdjustments _color;
    Vignette _vignette;
    Bloom _bloom;
    LiftGammaGain _lgg;

    bool _hasSetup;
    bool _isBlending;
    float _blendT;
    float _blendDuration;
    MoodPreset _from;
    MoodPreset _to;

    // ---------- Lifecycle ----------
    void OnEnable()
    {
        SafeSetup();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            ApplyPreset(MixWithMaster(GetPreset(initialMood)));
            currentMood = initialMood;
        }
#endif
    }

    void Start()
    {
        SafeSetup();

        if (Application.isPlaying)
        {
            var p = GetPreset(initialMood);
            SetMood(initialMood, p.defaultBlendSeconds > 0f ? p.defaultBlendSeconds : 0f);
        }
    }

    void Update()
    {
        if (!_hasSetup) return;

        if (_isBlending)
        {
            _blendT += Application.isPlaying ? Time.deltaTime : 0f;
            float w = (_blendDuration <= 0f) ? 1f : Mathf.Clamp01(_blendT / _blendDuration);
            var lerped = LerpPreset(_from, _to, w);
            ApplyPreset(MixWithMaster(lerped));

            if (w >= 1f) _isBlending = false;
        }
#if UNITY_EDITOR
        // live react to Master Intensity changes in edit mode
        if (!Application.isPlaying && !_isBlending)
            ApplyPreset(MixWithMaster(GetPreset(currentMood)));
#endif
    }

    // ---------- Public API ----------
    public void SetMood(MoodId id, float blendSeconds = -1f)
    {
        SafeSetup();

        var target = GetPreset(id);
        float dur = (blendSeconds >= 0f) ? blendSeconds : Mathf.Max(0f, target.defaultBlendSeconds);

        if (dur <= 0f)
        {
            ApplyPreset(MixWithMaster(target));
            currentMood = id;
            _isBlending = false;
            return;
        }

        _from = MixWithMaster(GetPreset(currentMood));
        _to = MixWithMaster(target);
        _blendDuration = dur;
        _blendT = 0f;
        _isBlending = true;
        currentMood = id;
    }

    // Back-compat shims
    [Obsolete("Use SetMood(MoodId, float) instead.")]
    public void CrossfadeTo(MoodId id, float duration) => SetMood(id, duration);
    [Obsolete("Use SetMood(MoodId) instead.")]
    public void CrossfadeTo(MoodId id) => SetMood(id, -1f);
    [Obsolete("Use SetMood(MoodId, float).")]
    public void CrossfadeTo(string moodName, float duration = -1f)
    { if (Enum.TryParse(moodName, true, out MoodId id)) SetMood(id, duration); else Debug.LogWarning($"Unknown mood '{moodName}'."); }
    [Obsolete("Use SetMood(MoodId, float).")]
    public void CrossfadeTo(int moodIndex, float duration = -1f)
    { if (Enum.IsDefined(typeof(MoodId), moodIndex)) SetMood((MoodId)moodIndex, duration); else Debug.LogWarning($"Invalid mood index {moodIndex}."); }

    // Editor button uses this for instant preview
    public void ApplySelectedMoodImmediate()
    {
        SetMood(initialMood, 0f); // no blend
    }

    // Built-in preset package (softened)
    public void LoadBuiltInPresets()
    {
        // Neutral – as clean as possible
        Neutral = new MoodPreset
        {
            exposure = 0f,
            contrast = 0f,
            saturation = 0f,
            colorFilter = Color.white,
            vignetteIntensity = 0.05f,
            vignetteSmoothness = 0.3f,
            bloomIntensity = 0.25f,
            lift = new Color(1f, 1f, 1f, 0f),
            gamma = new Color(1f, 1f, 1f, 0f),
            gain = new Color(1f, 1f, 1f, 0f),
            defaultBlendSeconds = 0.3f
        };

        // Happiness – gentle warmth & pop
        Happiness = new MoodPreset
        {
            exposure = 0.20f,
            contrast = 6f,
            saturation = 12f,
            colorFilter = new Color(1.02f, 0.99f, 0.96f, 1f),
            vignetteIntensity = 0.06f,
            vignetteSmoothness = 0.35f,
            bloomIntensity = 1.1f,
            lift = new Color(1.01f, 1.00f, 0.99f, 0f),
            gamma = new Color(1.01f, 1.00f, 0.99f, 0f),
            gain = new Color(1.03f, 1.01f, 0.99f, 0f),
            defaultBlendSeconds = 0.5f
        };

        // Sadness – mild cool & desat
        Sadness = new MoodPreset
        {
            exposure = -0.20f,
            contrast = -4f,
            saturation = -18f,
            colorFilter = new Color(0.98f, 0.99f, 1.02f, 1f),
            vignetteIntensity = 0.18f,
            vignetteSmoothness = 0.5f,
            bloomIntensity = 0.15f,
            lift = new Color(0.99f, 1.00f, 1.02f, 0f),
            gamma = new Color(0.99f, 1.00f, 1.02f, 0f),
            gain = new Color(0.98f, 0.99f, 1.02f, 0f),
            defaultBlendSeconds = 0.7f
        };

        // Nostalgic – subtle sepia
        Nostalgic = new MoodPreset
        {
            exposure = -0.05f,
            contrast = -6f,
            saturation = -8f,
            colorFilter = new Color(1.02f, 0.99f, 0.94f, 1f),
            vignetteIntensity = 0.12f,
            vignetteSmoothness = 0.5f,
            bloomIntensity = 0.8f,
            lift = new Color(1.01f, 0.99f, 0.97f, 0f),
            gamma = new Color(1.01f, 0.99f, 0.97f, 0f),
            gain = new Color(1.02f, 1.00f, 0.97f, 0f),
            defaultBlendSeconds = 0.8f
        };

        // Furious – punchy but controlled
        Furious = new MoodPreset
        {
            exposure = 0.10f,
            contrast = 14f,
            saturation = 6f,
            colorFilter = new Color(1.03f, 0.98f, 0.97f, 1f),
            vignetteIntensity = 0.22f,
            vignetteSmoothness = 0.55f,
            bloomIntensity = 0.25f,
            lift = new Color(1.02f, 0.99f, 0.98f, 0f),
            gamma = new Color(1.03f, 0.99f, 0.98f, 0f),
            gain = new Color(1.05f, 1.00f, 0.98f, 0f),
            defaultBlendSeconds = 0.45f
        };

        // Triggered – cold, hazy, but not washed out
        Triggered = new MoodPreset
        {
            exposure = 0.05f,
            contrast = -8f,
            saturation = -22f,
            colorFilter = new Color(0.98f, 0.99f, 1.03f, 1f),
            vignetteIntensity = 0.24f,
            vignetteSmoothness = 0.6f,
            bloomIntensity = 1.4f,
            lift = new Color(1.00f, 1.01f, 1.03f, 0f),
            gamma = new Color(1.00f, 1.01f, 1.03f, 0f),
            gain = new Color(0.99f, 1.01f, 1.05f, 0f),
            defaultBlendSeconds = 0.6f
        };
    }

    // ---------- Internal ----------
    void SafeSetup()
    {
        if (_hasSetup) return;

        // Ensure a Volume exists
        if (!targetVolume)
        {
            targetVolume = GetComponentInParent<Volume>();
            if (!targetVolume)
            {
                var found = FindObjectOfType<Volume>();
                if (found && found.isGlobal) targetVolume = found;
            }
            if (!targetVolume)
            {
                var go = GameObject.Find("Global Volume") ?? new GameObject("Global Volume");
                targetVolume = go.GetComponent<Volume>() ?? go.AddComponent<Volume>();
                targetVolume.isGlobal = true;
                targetVolume.priority = 0f;
            }
        }

        // Ensure a profile exists (in-memory for runtime safety)
        if (!targetVolume.profile)
            targetVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();

        var profile = targetVolume.profile;

        // Ensure and cache overrides
        if (!profile.TryGet(out _color)) _color = profile.Add<ColorAdjustments>(true);
        if (!profile.TryGet(out _vignette)) _vignette = profile.Add<Vignette>(true);
        if (!profile.TryGet(out _bloom)) _bloom = profile.Add<Bloom>(true);
        if (!profile.TryGet(out _lgg)) _lgg = profile.Add<LiftGammaGain>(true);

        _color.active = true; _vignette.active = true; _bloom.active = true; _lgg.active = true;

        _hasSetup = true;
        currentMood = initialMood;
    }

    MoodPreset GetPreset(MoodId id)
    {
        switch (id)
        {
            case MoodId.Happiness: return Happiness;
            case MoodId.Sadness: return Sadness;
            case MoodId.Nostalgic: return Nostalgic;
            case MoodId.Furious: return Furious;
            case MoodId.Triggered: return Triggered;
            default: return Neutral;
        }
    }

    static MoodPreset LerpPreset(in MoodPreset a, in MoodPreset b, float w)
    {
        return new MoodPreset
        {
            exposure = Mathf.Lerp(a.exposure, b.exposure, w),
            contrast = Mathf.Lerp(a.contrast, b.contrast, w),
            saturation = Mathf.Lerp(a.saturation, b.saturation, w),
            colorFilter = Color.Lerp(a.colorFilter, b.colorFilter, w),
            vignetteIntensity = Mathf.Lerp(a.vignetteIntensity, b.vignetteIntensity, w),
            vignetteSmoothness = Mathf.Lerp(a.vignetteSmoothness, b.vignetteSmoothness, w),
            bloomIntensity = Mathf.Lerp(a.bloomIntensity, b.bloomIntensity, w),
            lift = Color.Lerp(a.lift, b.lift, w),
            gamma = Color.Lerp(a.gamma, b.gamma, w),
            gain = Color.Lerp(a.gain, b.gain, w),
            defaultBlendSeconds = Mathf.Lerp(a.defaultBlendSeconds, b.defaultBlendSeconds, w)
        };
    }

    // Pulls the given preset back toward Neutral by masterIntensity
    MoodPreset MixWithMaster(in MoodPreset p)
    {
        return LerpPreset(Neutral, p, Mathf.Clamp01(masterIntensity));
    }

    void ApplyPreset(in MoodPreset p)
    {
        if (_color != null)
        {
            _color.postExposure.Override(p.exposure);
            _color.contrast.Override(p.contrast);
            _color.saturation.Override(p.saturation);
            _color.colorFilter.Override(p.colorFilter);
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
            _lgg.lift.Override((Vector4)p.lift);
            _lgg.gamma.Override((Vector4)p.gamma);
            _lgg.gain.Override((Vector4)p.gain);
        }
    }
}
