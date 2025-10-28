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

        [Header("Light (optional but cheap)")]
        public Color lightColor = Color.white;
        [Range(0.2f, 1.3f)] public float lightIntensity = 1.0f;
        [Range(2500, 9000)] public float lightTemperature = 6500f;

        [Header("Shader parameters driven for this mood")]
        public MaterialParam[] materialParams;

        [Header("Post-Processing (optional)")]
        public PostFXParams postFX;

        [Header("Timing")]
        [Range(0.05f, 6f)] public float defaultFadeSeconds = 1.2f;
    }

    // -------- Inspector --------
    [Header("Scene References")]
    [Tooltip("Your single realtime Directional Light.")]
    public Light mainLight;

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
        if (!mainLight) mainLight = FindAnyObjectByType<Light>();
        CachePropIds();
    }

    void Start()
    {
        SetImmediate(initialMood);
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

        ApplyLight(m);
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
        // Light start
        Color c0 = mainLight ? mainLight.color : Color.white;
        float i0 = mainLight ? mainLight.intensity : 1f;
        float t0 = mainLight ? mainLight.colorTemperature : 6500f;

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

            // Light
            if (mainLight)
            {
                mainLight.color = Color.Lerp(c0, to.lightColor, k);
                mainLight.intensity = Mathf.Lerp(i0, to.lightIntensity, k);
                mainLight.colorTemperature = Mathf.Lerp(t0, to.lightTemperature, k);
            }

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
        ApplyLight(to);
        ApplyMaterialsImmediate(to);
        ApplyPostFXImmediate(to);
    }

    void ApplyLight(Mood m)
    {
        if (!mainLight) return;
        mainLight.color = m.lightColor;
        mainLight.intensity = m.lightIntensity;
        mainLight.colorTemperature = m.lightTemperature;
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
            // material left null -> assign in Inspector per mood entry
        };
        MaterialParam CP(string prop, Color c) => new MaterialParam
        {
            propertyName = prop,
            type = ParamType.Color,
            colorValue = c
            // material left null -> assign in Inspector per mood entry
        };
        Color HEX(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var col)) return col;
            return Color.white;
        }

        // ---- Mood tints (also used for _TintColor) ----
        var T_Happy = HEX("#FFD9E6"); // soft pink-rose
        var T_Sad = HEX("#8BB4FF"); // cool blue
        var T_Nost = HEX("#D8BFA2"); // sepia beige
        var T_Fury = HEX("#FF6A6A"); // hot red
        var T_Trig = HEX("#FF9EDB"); // punchy pink-magenta
        var T_Pity = HEX("#CABEFF"); // soft lavender
        var T_Joy = HEX("#FFC7DA"); // calm pink

        moods = new Mood[]
        {
            // -------------------- HAPPINESS --------------------
            new Mood {
                id = MoodId.Happiness,
                lightColor = new Color(1.00f, 0.95f, 0.85f),
                lightIntensity = 1.10f, lightTemperature = 6800f,
                defaultFadeSeconds = 1.0f,
                postFX = new PostFXParams {
                    usePostFX = true,
                    postExposure = 0.20f,
                    saturation = 12f,    // -100..100
                    contrast = 6f,       // -100..100
                    colorFilter = T_Happy,
                    bloomIntensity = 0.55f,
                    bloomThreshold = 1.05f,
                    vignetteIntensity = 0.10f,
                    vignetteSmoothness = 0.22f
                },
                materialParams = new MaterialParam[] {
                    CP("_TintColor",   T_Happy),
                    FP("_TintStrength",0.35f),
                    FP("_Desaturate",  0.00f),
                    FP("_LensWarp",    0.00f),
                    FP("_Glitch",      0.00f),
                    FP("_TimeWarp",    0.00f),
                    FP("_VignetteBoost",0.00f),
                    FP("_BloomBoost",  0.10f)
                }
            },

            // -------------------- SADNESS --------------------
            new Mood {
                id = MoodId.Sadness,
                lightColor = new Color(0.75f, 0.85f, 1.00f),
                lightIntensity = 0.70f, lightTemperature = 6500f,
                defaultFadeSeconds = 1.2f,
                postFX = new PostFXParams {
                    usePostFX = true,
                    postExposure = -0.10f,
                    saturation = -18f,
                    contrast = -4f,
                    colorFilter = T_Sad,
                    bloomIntensity = 0.20f,
                    bloomThreshold = 1.20f,
                    vignetteIntensity = 0.30f,
                    vignetteSmoothness = 0.48f
                },
                materialParams = new MaterialParam[] {
                    CP("_TintColor",   T_Sad),
                    FP("_TintStrength",0.45f),
                    FP("_Desaturate",  0.25f),
                    FP("_LensWarp",    0.02f),
                    FP("_ColorSplit",  0.03f),
                    FP("_Glitch",      0.00f),
                    FP("_TimeWarp",    0.00f),
                    FP("_VignetteBoost",0.10f),
                    FP("_BloomBoost",  0.00f)
                }
            },

            // -------------------- NOSTALGIC --------------------
            new Mood {
                id = MoodId.Nostalgic,
                lightColor = new Color(1.00f, 0.90f, 0.75f),
                lightIntensity = 0.90f, lightTemperature = 5200f,
                defaultFadeSeconds = 1.4f,
                postFX = new PostFXParams {
                    usePostFX = true,
                    postExposure = 0.05f,
                    saturation = -10f,
                    contrast = 5f,
                    colorFilter = T_Nost,
                    bloomIntensity = 0.35f,
                    bloomThreshold = 1.10f,
                    vignetteIntensity = 0.20f,
                    vignetteSmoothness = 0.38f
                },
                materialParams = new MaterialParam[] {
                    CP("_TintColor",   T_Nost),
                    FP("_TintStrength",0.50f),
                    FP("_Desaturate",  0.15f),
                    FP("_Dither",      0.10f),
                    FP("_FilmFlicker", 0.10f),
                    FP("_LensWarp",    0.00f),
                    FP("_ColorSplit",  0.00f),
                    FP("_Glitch",      0.00f),
                    FP("_VignetteBoost",0.06f),
                    FP("_BloomBoost",  0.08f)
                }
            },

            // -------------------- FURIOUS --------------------
            new Mood {
                id = MoodId.Furious,
                lightColor = new Color(1.00f, 0.55f, 0.45f),
                lightIntensity = 1.20f, lightTemperature = 5000f,
                defaultFadeSeconds = 0.6f,
                postFX = new PostFXParams {
                    usePostFX = true,
                    postExposure = 0.25f,
                    saturation = 10f,
                    contrast = 14f,
                    colorFilter = T_Fury,
                    bloomIntensity = 0.15f,
                    bloomThreshold = 1.30f,
                    vignetteIntensity = 0.40f,
                    vignetteSmoothness = 0.52f
                },
                materialParams = new MaterialParam[] {
                    CP("_TintColor",   T_Fury),
                    FP("_TintStrength",0.55f),
                    FP("_Desaturate",  0.00f),
                    FP("_LensWarp",    0.08f),
                    FP("_ColorSplit",  0.10f),
                    FP("_Glitch",      0.20f),
                    FP("_ScanlineJitter",0.12f),
                    FP("_TimeWarp",    0.06f),
                    FP("_VignetteBoost",0.15f),
                    FP("_BloomBoost",  0.00f)
                }
            },

            // -------------------- TRIGGERED --------------------
            new Mood {
                id = MoodId.Triggered,
                lightColor = new Color(0.90f, 0.80f, 1.00f),
                lightIntensity = 0.95f, lightTemperature = 7000f,
                defaultFadeSeconds = 0.8f,
                postFX = new PostFXParams {
                    usePostFX = true,
                    postExposure = 0.00f,
                    saturation = -5f,
                    contrast = 10f,
                    colorFilter = T_Trig,
                    bloomIntensity = 0.00f,
                    bloomThreshold = 1.40f,
                    vignetteIntensity = 0.50f,
                    vignetteSmoothness = 0.55f
                },
                materialParams = new MaterialParam[] {
                    CP("_TintColor",   T_Trig),
                    FP("_TintStrength",0.60f),
                    FP("_Desaturate",  0.05f),
                    FP("_LensWarp",    0.15f),
                    FP("_ColorSplit",  0.25f),
                    FP("_Glitch",      0.14f),
                    FP("_TimeWarp",    0.12f),
                    FP("_VignetteBoost",0.20f),
                    FP("_BloomBoost",  0.00f)
                }
            },

            // -------------------- PITY --------------------
            new Mood {
                id = MoodId.Pity,
                lightColor = new Color(0.88f, 0.90f, 1.00f),
                lightIntensity = 0.80f, lightTemperature = 6800f,
                defaultFadeSeconds = 1.1f,
                postFX = new PostFXParams {
                    usePostFX = true,
                    postExposure = -0.05f,
                    saturation = -12f,
                    contrast = -2f,
                    colorFilter = T_Pity,
                    bloomIntensity = 0.15f,
                    bloomThreshold = 1.25f,
                    vignetteIntensity = 0.22f,
                    vignetteSmoothness = 0.40f
                },
                materialParams = new MaterialParam[] {
                    CP("_TintColor",   T_Pity),
                    FP("_TintStrength",0.40f),
                    FP("_Desaturate",  0.10f),
                    FP("_LensWarp",    0.00f),
                    FP("_ColorSplit",  0.03f),
                    FP("_Glitch",      0.00f),
                    FP("_TimeWarp",    0.00f),
                    FP("_VignetteBoost",0.06f),
                    FP("_BloomBoost",  0.04f)
                }
            },

            // -------------------- JOYFUL --------------------
            new Mood {
                id = MoodId.Joyful,
                lightColor = new Color(1.00f, 0.98f, 0.90f),
                lightIntensity = 1.15f, lightTemperature = 7000f,
                defaultFadeSeconds = 0.9f,
                postFX = new PostFXParams {
                    usePostFX = true,
                    postExposure = 0.18f,
                    saturation = 16f,
                    contrast = 8f,
                    colorFilter = T_Joy,
                    bloomIntensity = 0.50f,
                    bloomThreshold = 1.05f,
                    vignetteIntensity = 0.10f,
                    vignetteSmoothness = 0.22f
                },
                materialParams = new MaterialParam[] {
                    CP("_TintColor",   T_Joy),
                    FP("_TintStrength",0.38f),
                    FP("_Desaturate",  0.00f),
                    FP("_LensWarp",    0.00f),
                    FP("_ColorSplit",  0.03f),
                    FP("_Glitch",      0.00f),
                    FP("_TimeWarp",    0.00f),
                    FP("_VignetteBoost",0.00f),
                    FP("_BloomBoost",  0.10f)
                }
            },
        };

        CachePropIds();
    }

    // Optional: assign materials once in the first mood, then copy to all others by property name
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

                // find matching property in the first mood
                for (int j = 0; j < first.Length; j++)
                {
                    var fp = first[j];
                    if (fp != null && fp.propertyName == tp.propertyName && fp.type == tp.type && fp.material != null)
                    {
                        tp.material = fp.material; // copy reference
                        break;
                    }
                }
            }
        }
        CachePropIds();
        Debug.Log("MoodFXDirector: Copied materials from first mood to all moods (by property name).");
    }
}
