using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MoodFXDirector : MonoBehaviour
{
    // -------- Types --------
    public enum ParamType { Float, Color }

    public enum MoodId
    {
        Happiness,
        Sadness,
        Nostalgic,
        Furious,
        Triggered,
        Pity,
        Joyful
    }

    [Serializable]
    public class MaterialParam
    {
        [Tooltip("Material instance to drive (use unique instances, not sharedMaterial, for per-scene control).")]
        public Material material;

        [Tooltip("Shader property to set (e.g. _Strength, _TintColor, _BlurAmount).")]
        public string propertyName = "_Strength";

        [Tooltip("Type of property to set.")]
        public ParamType type = ParamType.Float;

        [Tooltip("Target value for this mood (float).")]
        public float floatValue = 0f;

        [Tooltip("Target value for this mood (color).")]
        public Color colorValue = Color.white;

        // Cache
        [NonSerialized] public int propId = -1;
    }

    [Serializable]
    public class PostFXParams
    {
        [Tooltip("Apply post-processing changes for this mood (requires Volume with the relevant overrides).")]
        public bool usePostFX = false;

        [Header("Color Adjustments")]
        public float postExposure = 0f;
        [Range(-100f, 100f)] public float saturation = 0f;
        [Range(-100f, 100f)] public float contrast = 0f;
        public Color colorFilter = Color.white;

        [Header("Bloom")]
        [Min(0f)] public float bloomIntensity = 0f;
        [Min(0f)] public float bloomThreshold = 1f;

        [Header("Vignette")]
        [Range(0f, 1f)] public float vignetteIntensity = 0f;
        [Range(0f, 1f)] public float vignetteSmoothness = 0.2f;
    }

    [Serializable]
    public class Mood
    {
        [Tooltip("Mood identifier.")]
        public MoodId id = MoodId.Happiness;

        [Header("Light (relative to each lamp's baseline)")]
        [Tooltip("0 = keep original lamp color; 1 = fully tint towards this color.")]
        [Range(0, 1)] public float lightColorBlend = 0.75f;
        public Color lightColor = Color.white;

        [Tooltip("Multiply each lamp's intensity by this factor (1 = keep original).")]
        [Range(0.2f, 1.8f)] public float lightIntensityMul = 1.0f;

        [Tooltip("Blend lamp color temperature towards this value (0 = keep, 1 = use this). Only affects lights using temperature.")]
        [Range(0, 1)] public float lightTemperatureBlend = 0f;
        [Range(2500, 9000)] public float lightTemperature = 6500f;

        [Header("Shader parameters driven for this mood")]
        public MaterialParam[] materialParams;

        [Header("Post-Processing (optional)")]
        public PostFXParams postFX;

        [Header("Timing")]
        [Range(0.05f, 6f)] public float defaultFadeSeconds = 1.2f;
    }

    // -------- Inspector --------
    [Header("Lighting Control")]
    [Tooltip("If true, we auto-collect all scene lights that are Realtime or Mixed and control them. If false, only the 'controlledLights' list is used.")]
    public bool autoCollectLights = true;

    [Tooltip("Optional manual list. If Auto Collect is ON, these are added on top (and de-duplicated).")]
    public List<Light> controlledLights = new List<Light>();

    [Tooltip("Affect these Unity Light types when auto-collecting.")]
    public bool affectDirectional = true;
    public bool affectPoint = true;
    public bool affectSpot = true;
    public bool affectArea = false; // (URP doesn't render built-in Area lights; keep off by default)

    [Tooltip("Ignore baked-only lights. Recommended ON for environment kits with baked lighting.")]
    public bool ignoreBakedLights = true;

    [Header("Scene References")]
    [Tooltip("Global Volume used for URP post-processing. Optional.")]
    public Volume globalVolume;

    [Header("Fade")]
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Moods")]
    public Mood[] moods;
    public MoodId initialMood = MoodId.Happiness;

    [Header("Quick Toggles (tick to apply)")]
    public bool toggleHappiness;
    public bool toggleSadness;
    public bool toggleNostalgic;
    public bool toggleFurious;
    public bool toggleTriggered;
    public bool togglePity;
    public bool toggleJoyful;

    // -------- Runtime --------
    Mood _current;
    Coroutine _fadeCo;

    // ----- Internal lighting state -----
    class LightEntry
    {
        public Light light;
        public Color baseColor;
        public float baseIntensity;
        public float baseTemperature;
        public bool usesTemp;
    }
    readonly List<LightEntry> _lightEntries = new List<LightEntry>();

    // Cached post fx start values during fade
    struct PostFXStart
    {
        public float exposure, saturation, contrast;
        public Color colorFilter;
        public float bloomIntensity, bloomThreshold;
        public float vignetteIntensity, vignetteSmoothness;
        public bool hasCA, hasBloom, hasVignette;
    }

    void Awake()
    {
        CachePropIds();
        RefreshLightEntries(); // <-- new
    }

    void Start()
    {
        SetImmediate(initialMood);
    }

    // -------- Utilities to collect lights --------
    [ContextMenu("Refresh Light Entries (Auto Collect)")]
    public void RefreshLightEntries()
    {
        _lightEntries.Clear();

        HashSet<Light> set = new HashSet<Light>();
        if (autoCollectLights)
        {
            var all = FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var l in all)
            {
                if (!l) continue;
                if (ignoreBakedLights && l.lightmapBakeType == LightmapBakeType.Baked) continue;
                if ((!affectDirectional && l.type == LightType.Directional) ||
                    (!affectPoint && l.type == LightType.Point) ||
                    (!affectSpot && l.type == LightType.Spot) ||
                    (!affectArea && l.type == LightType.Area))
                    continue;
                set.Add(l);
            }
        }
        // include manual list
        foreach (var l in controlledLights) if (l) set.Add(l);

        foreach (var l in set)
        {
            _lightEntries.Add(new LightEntry
            {
                light = l,
                baseColor = l.color,
                baseIntensity = l.intensity,
                baseTemperature = l.colorTemperature,
                usesTemp = l.useColorTemperature
            });
        }
    }

    void CachePropIds()
    {
        if (moods == null) return;
        foreach (var mood in moods)
        {
            if (mood?.materialParams == null) continue;
            foreach (var mp in mood.materialParams)
                if (mp != null && !string.IsNullOrEmpty(mp.propertyName))
                    mp.propId = Shader.PropertyToID(mp.propertyName);
        }
    }

    // -------- Quick Toggle Handling in Inspector --------
    void OnValidate()
    {
        CachePropIds();

        if (toggleHappiness) { FireToggle(MoodId.Happiness); toggleHappiness = false; }
        if (toggleSadness) { FireToggle(MoodId.Sadness); toggleSadness = false; }
        if (toggleNostalgic) { FireToggle(MoodId.Nostalgic); toggleNostalgic = false; }
        if (toggleFurious) { FireToggle(MoodId.Furious); toggleFurious = false; }
        if (toggleTriggered) { FireToggle(MoodId.Triggered); toggleTriggered = false; }
        if (togglePity) { FireToggle(MoodId.Pity); togglePity = false; }
        if (toggleJoyful) { FireToggle(MoodId.Joyful); toggleJoyful = false; }
    }

    void FireToggle(MoodId id)
    {
        if (Application.isPlaying) CrossfadeTo(id);
        else SetImmediate(id);
    }

    // -------- Public API (ENUM) --------
    public void SetImmediate(MoodId id)
    {
        var m = FindMood(id);
        if (m == null) return;

        ApplyLightImmediate(m);
        ApplyMaterialsImmediate(m);
        ApplyPostFXImmediate(m);
        _current = m;
    }

    public void CrossfadeTo(MoodId id, float seconds = -1f)
    {
        var target = FindMood(id);
        if (target == null || target == _current) return;

        float dur = (seconds > 0f) ? seconds : Mathf.Max(0.05f, target.defaultFadeSeconds);
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeRoutine(_current, target, dur));
        _current = target;
    }

    // -------- Public API (STRING overloads) --------
    public void SetImmediate(string id) => SetImmediate(ParseMoodId(id));
    public void CrossfadeTo(string id, float seconds = -1f) => CrossfadeTo(ParseMoodId(id), seconds);

    static MoodId ParseMoodId(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return MoodId.Happiness;
        s = s.Trim().ToLowerInvariant();
        switch (s)
        {
            case "happy":
            case "happiness": return MoodId.Happiness;
            case "sad":
            case "sadness":
            case "saddness": return MoodId.Sadness;
            case "nostalgic":
            case "nostalgia": return MoodId.Nostalgic;
            case "furious":
            case "anger":
            case "angry": return MoodId.Furious;
            case "triggered":
            case "anxious":
            case "anxiety": return MoodId.Triggered;
            case "pity":
            case "compassion": return MoodId.Pity;
            case "joy":
            case "joyful": return MoodId.Joyful;
            default: return MoodId.Happiness;
        }
    }

    // -------- Internals --------
    IEnumerator FadeRoutine(Mood from, Mood to, float dur)
    {
        // Lighting starts captured from baselines at Awake (per lamp)
        // We'll lerp relative multipliers / blends here.

        // Material starts (read current from 'to' targets)
        var toList = to?.materialParams;
        int count = (toList != null) ? toList.Length : 0;
        float[] startFloats = new float[count];
        Color[] startColors = new Color[count];

        for (int i = 0; i < count; i++)
        {
            var tp = toList[i];
            if (tp == null || tp.material == null || tp.propId == -1) continue;

            if (tp.type == ParamType.Float)
                startFloats[i] = tp.material.HasProperty(tp.propId) ? tp.material.GetFloat(tp.propId) : 0f;
            else
                startColors[i] = tp.material.HasProperty(tp.propId) ? tp.material.GetColor(tp.propId) : Color.black;
        }

        // PostFX starts
        PostFXStart p0 = CapturePostFXStart();

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = fadeCurve.Evaluate(Mathf.Clamp01(t / dur));

            // Lights: apply relative to baseline for every lamp
            ApplyLightLerped(to, k);

            // Materials
            for (int i = 0; i < count; i++)
            {
                var tp = toList[i];
                if (tp == null || tp.material == null || tp.propId == -1) continue;

                if (tp.type == ParamType.Float)
                {
                    float v = Mathf.Lerp(startFloats[i], tp.floatValue, k);
                    tp.material.SetFloat(tp.propId, v);
                }
                else
                {
                    Color v = Color.Lerp(startColors[i], tp.colorValue, k);
                    tp.material.SetColor(tp.propId, v);
                }
            }

            // PostFX
            LerpPostFX(p0, to.postFX, k);

            yield return null;
        }

        // Snap to final
        ApplyLightImmediate(to);
        ApplyMaterialsImmediate(to);
        ApplyPostFXImmediate(to);
    }

    // ---------- Lighting application (multi-lamp, relative) ----------
    void ApplyLightLerped(Mood target, float k)
    {
        if (_lightEntries.Count == 0 || target == null) return;

        float cBlend = target.lightColorBlend * k; // ease color blend by k
        float iMul = Mathf.Lerp(1f, target.lightIntensityMul, k);
        float tBlend = target.lightTemperatureBlend * k;

        foreach (var e in _lightEntries)
        {
            if (!e.light) continue;

            // Intensity: base * multiplier
            e.light.intensity = e.baseIntensity * iMul;

            // Color: blend from base color towards target tint
            var tinted = Color.Lerp(e.baseColor, target.lightColor, cBlend);
            e.light.color = tinted;

            // Temperature: blend if used
            if (e.usesTemp && tBlend > 0f)
                e.light.colorTemperature = Mathf.Lerp(e.baseTemperature, target.lightTemperature, tBlend);
        }
    }

    void ApplyLightImmediate(Mood m)
    {
        if (_lightEntries.Count == 0 || m == null) return;

        foreach (var e in _lightEntries)
        {
            if (!e.light) continue;

            e.light.intensity = e.baseIntensity * m.lightIntensityMul;

            var tinted = Color.Lerp(e.baseColor, m.lightColor, m.lightColorBlend);
            e.light.color = tinted;

            if (e.usesTemp && m.lightTemperatureBlend > 0f)
                e.light.colorTemperature = Mathf.Lerp(e.baseTemperature, m.lightTemperature, m.lightTemperatureBlend);
        }
    }

    void ApplyMaterialsImmediate(Mood m)
    {
        if (m?.materialParams == null) return;
        foreach (var tp in m.materialParams)
        {
            if (tp == null || tp.material == null) continue;
            if (tp.propId == -1 && !string.IsNullOrEmpty(tp.propertyName))
                tp.propId = Shader.PropertyToID(tp.propertyName);

            if (tp.propId == -1 || !tp.material.HasProperty(tp.propId)) continue;

            if (tp.type == ParamType.Float) tp.material.SetFloat(tp.propId, tp.floatValue);
            else tp.material.SetColor(tp.propId, tp.colorValue);
        }
    }

    // -------- PostFX helpers (URP) --------
    PostFXStart CapturePostFXStart()
    {
        PostFXStart s = new PostFXStart();
        if (!globalVolume || !globalVolume.profile) return s;

        if (globalVolume.profile.TryGet(out ColorAdjustments ca))
        {
            s.hasCA = true;
            s.exposure = ca.postExposure.value;
            s.saturation = ca.saturation.value;
            s.contrast = ca.contrast.value;
            s.colorFilter = ca.colorFilter.value;
        }

        if (globalVolume.profile.TryGet(out Bloom bloom))
        {
            s.hasBloom = true;
            s.bloomIntensity = bloom.intensity.value;
            s.bloomThreshold = bloom.threshold.value;
        }

        if (globalVolume.profile.TryGet(out Vignette vig))
        {
            s.hasVignette = true;
            s.vignetteIntensity = vig.intensity.value;
            s.vignetteSmoothness = vig.smoothness.value;
        }

        return s;
    }

    void LerpPostFX(PostFXStart s, PostFXParams target, float k)
    {
        if (!globalVolume || !globalVolume.profile || target == null || !target.usePostFX) return;

        if (globalVolume.profile.TryGet(out ColorAdjustments ca) && s.hasCA)
        {
            ca.postExposure.overrideState = true;
            ca.saturation.overrideState = true;
            ca.contrast.overrideState = true;
            ca.colorFilter.overrideState = true;

            ca.postExposure.value = Mathf.Lerp(s.exposure, target.postExposure, k);
            ca.saturation.value = Mathf.Lerp(s.saturation, target.saturation, k);
            ca.contrast.value = Mathf.Lerp(s.contrast, target.contrast, k);
            ca.colorFilter.value = Color.Lerp(s.colorFilter, target.colorFilter, k);
        }

        if (globalVolume.profile.TryGet(out Bloom bloom) && s.hasBloom)
        {
            bloom.intensity.overrideState = true;
            bloom.threshold.overrideState = true;

            bloom.intensity.value = Mathf.Lerp(s.bloomIntensity, target.bloomIntensity, k);
            bloom.threshold.value = Mathf.Lerp(s.bloomThreshold, target.bloomThreshold, k);
        }

        if (globalVolume.profile.TryGet(out Vignette vig) && s.hasVignette)
        {
            vig.intensity.overrideState = true;
            vig.smoothness.overrideState = true;

            vig.intensity.value = Mathf.Lerp(s.vignetteIntensity, target.vignetteIntensity, k);
            vig.smoothness.value = Mathf.Lerp(s.vignetteSmoothness, target.vignetteSmoothness, k);
        }
    }

    void ApplyPostFXImmediate(Mood m)
    {
        if (!globalVolume || !globalVolume.profile || m.postFX == null || !m.postFX.usePostFX) return;

        if (globalVolume.profile.TryGet(out ColorAdjustments ca))
        {
            ca.postExposure.overrideState = true;
            ca.saturation.overrideState = true;
            ca.contrast.overrideState = true;
            ca.colorFilter.overrideState = true;

            ca.postExposure.value = m.postFX.postExposure;
            ca.saturation.value = m.postFX.saturation;
            ca.contrast.value = m.postFX.contrast;
            ca.colorFilter.value = m.postFX.colorFilter;
        }

        if (globalVolume.profile.TryGet(out Bloom bloom))
        {
            bloom.intensity.overrideState = true;
            bloom.threshold.overrideState = true;

            bloom.intensity.value = m.postFX.bloomIntensity;
            bloom.threshold.value = m.postFX.bloomThreshold;
        }

        if (globalVolume.profile.TryGet(out Vignette vig))
        {
            vig.intensity.overrideState = true;
            vig.smoothness.overrideState = true;

            vig.intensity.value = m.postFX.vignetteIntensity;
            vig.smoothness.value = m.postFX.vignetteSmoothness;
        }
    }

    Mood FindMood(MoodId id) => Array.Find(moods, x => x != null && x.id == id);

    // -------- Utilities --------
    [ContextMenu("Seed Seven Default Mood Packages")]
    void SeedDefaults()
    {
        // Helpers just for seeding (scoped here)
        MaterialParam FP(string prop, float v) => new MaterialParam
        {
            propertyName = prop,
            type = ParamType.Float,
            floatValue = v
        };
        MaterialParam CP(string prop, Color c) => new MaterialParam
        {
            propertyName = prop,
            type = ParamType.Color,
            colorValue = c
        };
        Color HEX(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var col)) return col;
            return Color.white;
        }

        // ---- Mood tints (also used for _TintColor) ----
        var T_Happy = HEX("#FFD9E6"); // soft pink-rose
        var T_Sad = HEX("#8BB4FF");   // cool blue
        var T_Nost = HEX("#D8BFA2");  // sepia beige
        var T_Fury = HEX("#FF6A6A");  // hot red
        var T_Trig = HEX("#FF9EDB");  // punchy pink-magenta
        var T_Pity = HEX("#CABEFF");  // soft lavender
        var T_Joy = HEX("#FFC7DA");   // calm pink

        moods = new Mood[]
        {
            new Mood {
                id = MoodId.Happiness,
                lightColorBlend = 0.5f, lightColor = new Color(1.00f, 0.95f, 0.85f),
                lightIntensityMul = 1.10f, lightTemperatureBlend = 0.5f, lightTemperature = 6800f,
                defaultFadeSeconds = 1.0f,
                postFX = new PostFXParams { usePostFX = true, postExposure = 0.20f, saturation = 12f, contrast = 6f, colorFilter = T_Happy, bloomIntensity = 0.55f, bloomThreshold = 1.05f, vignetteIntensity = 0.10f, vignetteSmoothness = 0.22f },
                materialParams = new MaterialParam[] { CP("_TintColor",T_Happy), FP("_TintStrength",0.35f), FP("_Desaturate",0.00f), FP("_LensWarp",0.00f), FP("_Glitch",0.00f), FP("_TimeWarp",0.00f), FP("_VignetteBoost",0.00f), FP("_BloomBoost",0.10f) }
            },
            new Mood {
                id = MoodId.Sadness,
                lightColorBlend = 0.6f, lightColor = new Color(0.75f, 0.85f, 1.00f),
                lightIntensityMul = 0.70f, lightTemperatureBlend = 0.3f, lightTemperature = 6500f,
                defaultFadeSeconds = 1.2f,
                postFX = new PostFXParams { usePostFX = true, postExposure = -0.10f, saturation = -18f, contrast = -4f, colorFilter = T_Sad, bloomIntensity = 0.20f, bloomThreshold = 1.20f, vignetteIntensity = 0.30f, vignetteSmoothness = 0.48f },
                materialParams = new MaterialParam[] { CP("_TintColor",T_Sad), FP("_TintStrength",0.45f), FP("_Desaturate",0.25f), FP("_LensWarp",0.02f), FP("_ColorSplit",0.03f), FP("_Glitch",0.00f), FP("_TimeWarp",0.00f), FP("_VignetteBoost",0.10f), FP("_BloomBoost",0.00f) }
            },
            new Mood {
                id = MoodId.Nostalgic,
                lightColorBlend = 0.65f, lightColor = new Color(1.00f, 0.90f, 0.75f),
                lightIntensityMul = 0.90f, lightTemperatureBlend = 0.5f, lightTemperature = 5200f,
                defaultFadeSeconds = 1.4f,
                postFX = new PostFXParams { usePostFX = true, postExposure = 0.05f, saturation = -10f, contrast = 5f, colorFilter = T_Nost, bloomIntensity = 0.35f, bloomThreshold = 1.10f, vignetteIntensity = 0.20f, vignetteSmoothness = 0.38f },
                materialParams = new MaterialParam[] { CP("_TintColor",T_Nost), FP("_TintStrength",0.50f), FP("_Desaturate",0.15f), FP("_Dither",0.10f), FP("_FilmFlicker",0.10f), FP("_LensWarp",0.00f), FP("_ColorSplit",0.00f), FP("_Glitch",0.00f), FP("_VignetteBoost",0.06f), FP("_BloomBoost",0.08f) }
            },
            new Mood {
                id = MoodId.Furious,
                lightColorBlend = 0.85f, lightColor = new Color(1.00f, 0.55f, 0.45f),
                lightIntensityMul = 1.20f, lightTemperatureBlend = 0.6f, lightTemperature = 5000f,
                defaultFadeSeconds = 0.6f,
                postFX = new PostFXParams { usePostFX = true, postExposure = 0.25f, saturation = 10f, contrast = 14f, colorFilter = T_Fury, bloomIntensity = 0.15f, bloomThreshold = 1.30f, vignetteIntensity = 0.40f, vignetteSmoothness = 0.52f },
                materialParams = new MaterialParam[] { CP("_TintColor",T_Fury), FP("_TintStrength",0.55f), FP("_Desaturate",0.00f), FP("_LensWarp",0.08f), FP("_ColorSplit",0.10f), FP("_Glitch",0.20f), FP("_ScanlineJitter",0.12f), FP("_TimeWarp",0.06f), FP("_VignetteBoost",0.15f), FP("_BloomBoost",0.00f) }
            },
            new Mood {
                id = MoodId.Triggered,
                lightColorBlend = 0.75f, lightColor = new Color(0.90f, 0.80f, 1.00f),
                lightIntensityMul = 0.95f, lightTemperatureBlend = 0.6f, lightTemperature = 7000f,
                defaultFadeSeconds = 0.8f,
                postFX = new PostFXParams { usePostFX = true, postExposure = 0.00f, saturation = -5f, contrast = 10f, colorFilter = T_Trig, bloomIntensity = 0.00f, bloomThreshold = 1.40f, vignetteIntensity = 0.50f, vignetteSmoothness = 0.55f },
                materialParams = new MaterialParam[] { CP("_TintColor",T_Trig), FP("_TintStrength",0.60f), FP("_Desaturate",0.05f), FP("_LensWarp",0.15f), FP("_ColorSplit",0.25f), FP("_Glitch",0.14f), FP("_TimeWarp",0.12f), FP("_VignetteBoost",0.20f), FP("_BloomBoost",0.00f) }
            },
            new Mood {
                id = MoodId.Pity,
                lightColorBlend = 0.6f, lightColor = new Color(0.88f, 0.90f, 1.00f),
                lightIntensityMul = 0.80f, lightTemperatureBlend = 0.5f, lightTemperature = 6800f,
                defaultFadeSeconds = 1.1f,
                postFX = new PostFXParams { usePostFX = true, postExposure = -0.05f, saturation = -12f, contrast = -2f, colorFilter = T_Pity, bloomIntensity = 0.15f, bloomThreshold = 1.25f, vignetteIntensity = 0.22f, vignetteSmoothness = 0.40f },
                materialParams = new MaterialParam[] { CP("_TintColor",T_Pity), FP("_TintStrength",0.40f), FP("_Desaturate",0.10f), FP("_LensWarp",0.00f), FP("_ColorSplit",0.03f), FP("_Glitch",0.00f), FP("_TimeWarp",0.00f), FP("_VignetteBoost",0.06f), FP("_BloomBoost",0.04f) }
            },
            new Mood {
                id = MoodId.Joyful,
                lightColorBlend = 0.5f, lightColor = new Color(1.00f, 0.98f, 0.90f),
                lightIntensityMul = 1.15f, lightTemperatureBlend = 0.6f, lightTemperature = 7000f,
                defaultFadeSeconds = 0.9f,
                postFX = new PostFXParams { usePostFX = true, postExposure = 0.18f, saturation = 16f, contrast = 8f, colorFilter = T_Joy, bloomIntensity = 0.50f, bloomThreshold = 1.05f, vignetteIntensity = 0.10f, vignetteSmoothness = 0.22f },
                materialParams = new MaterialParam[] { CP("_TintColor",T_Joy), FP("_TintStrength",0.38f), FP("_Desaturate",0.00f), FP("_LensWarp",0.00f), FP("_ColorSplit",0.03f), FP("_Glitch",0.00f), FP("_TimeWarp",0.00f), FP("_VignetteBoost",0.00f), FP("_BloomBoost",0.10f) }
            },
        };

        CachePropIds();
    }

    [ContextMenu("Copy Materials From First Mood To All")]
    void CopyMaterialsFromFirstMoodToAll()
    {
        if (moods == null || moods.Length == 0 || moods[0]?.materialParams == null) return;

        var first = moods[0].materialParams;
        for (int m = 1; m < moods.Length; m++)
        {
            var mm = moods[m];
            if (mm == null || mm.materialParams == null) continue;

            for (int i = 0; i < mm.materialParams.Length; i++)
            {
                var tp = mm.materialParams[i];
                if (tp == null || string.IsNullOrEmpty(tp.propertyName)) continue;

                for (int j = 0; j < first.Length; j++)
                {
                    var fp = first[j];
                    if (fp != null && fp.propertyName == tp.propertyName && fp.type == tp.type && fp.material != null)
                    {
                        tp.material = fp.material;
                        break;
                    }
                }
            }
        }
        CachePropIds();
        Debug.Log("MoodFXDirector: Copied materials from first mood to all moods (by property name).");
    }
}
