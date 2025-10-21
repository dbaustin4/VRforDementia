using System;
using System.Collections;
using UnityEngine;

public class MoodFXDirector : MonoBehaviour
{
    // -------- Types --------
    public enum ParamType { Float, Color }

    [Serializable]
    public class MaterialParam
    {
        [Tooltip("Which material (instance) to drive. Use a unique material per effect object (no shared material if you want per-scene control).")]
        public Material material;

        [Tooltip("Shader property to set (e.g. _Strength, _TintColor, _BlurAmount, _Afterimage).")]
        public string propertyName = "_Strength";

        [Tooltip("Type of property.")]
        public ParamType type = ParamType.Float;

        [Tooltip("Target value for this mood (float).")]
        public float floatValue = 0f;

        [Tooltip("Target value for this mood (color).")]
        public Color colorValue = Color.white;

        // Cache
        [NonSerialized] public int propId = -1;
    }

    [Serializable]
    public class Mood
    {
        [Tooltip("Unique id (e.g., Calm, Happy, Sadness, Fear, Mystery, Nostalgia).")]
        public string id = "Calm";

        [Header("Light")]
        public Color lightColor = Color.white;
        [Range(0.2f, 1.3f)] public float lightIntensity = 1.0f;
        [Range(2500, 9000)] public float lightTemperature = 6500f;

        [Header("Shader parameters to drive for this mood")]
        public MaterialParam[] materialParams;

        [Header("Timing")]
        [Range(0.05f, 6f)] public float defaultFadeSeconds = 1.2f;
    }

    // -------- Inspector --------
    [Header("Scene References")]
    public Light mainLight; // your single realtime Directional

    [Header("Fade")]
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Moods")]
    public Mood[] moods;
    public string initialMood = "Calm";

    // -------- Runtime --------
    Mood _current;
    Coroutine _fadeCo;

    void Awake()
    {
        if (!mainLight) mainLight = FindAnyObjectByType<Light>();
        // Cache property IDs (faster & safer)
        foreach (var mood in moods)
        {
            if (mood?.materialParams == null) continue;
            foreach (var mp in mood.materialParams)
                if (mp != null && !string.IsNullOrEmpty(mp.propertyName))
                    mp.propId = Shader.PropertyToID(mp.propertyName);
        }
    }

    void Start()
    {
        SetImmediate(initialMood);
    }

    // -------- Public API --------
    public void SetImmediate(string id)
    {
        var m = FindMood(id);
        if (m == null) return;

        ApplyLight(m);
        ApplyMaterialsImmediate(m);
        _current = m;
    }

    public void CrossfadeTo(string id, float seconds = -1f)
    {
        var target = FindMood(id);
        if (target == null || target == _current) return;

        float dur = (seconds > 0f) ? seconds : Mathf.Max(0.05f, target.defaultFadeSeconds);
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeRoutine(_current, target, dur));
        _current = target;
    }

    // Convenience “checkpoint” helpers
    public void CheckpointHappy(float s = 1.2f) => CrossfadeTo("Happy", s);
    public void CheckpointSadness(float s = 1.2f) => CrossfadeTo("Sadness", s);
    public void CheckpointFear(float s = 1.2f) => CrossfadeTo("Fear", s);
    public void CheckpointMystery(float s = 1.2f) => CrossfadeTo("Mystery", s);
    public void CheckpointNostalgia(float s = 1.2f) => CrossfadeTo("Nostalgia", s);

    /// <summary>
    /// Crossfade using an existing mood's shader set but override light values (cheap “custom”).
    /// </summary>
    public void CheckpointCustom(string baseMoodId, Color? lightColor = null, float? lightIntensity = null, float? lightTemp = null, float seconds = 1.2f)
    {
        var baseMood = FindMood(baseMoodId);
        if (baseMood == null) return;

        // Runtime copy for light only (materials come from baseMood)
        Mood runtime = new Mood
        {
            id = baseMood.id,
            materialParams = baseMood.materialParams,
            lightColor = lightColor ?? baseMood.lightColor,
            lightIntensity = Mathf.Clamp(lightIntensity ?? baseMood.lightIntensity, 0.2f, 1.3f),
            lightTemperature = Mathf.Clamp(lightTemp ?? baseMood.lightTemperature, 2500f, 9000f),
            defaultFadeSeconds = seconds
        };

        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeRoutine(_current, runtime, Mathf.Max(0.05f, seconds)));
        _current = baseMood; // after fade, consider we're in the base mood
    }

    // -------- Internals --------
    IEnumerator FadeRoutine(Mood from, Mood to, float dur)
    {
        // Cache start light
        Color c0 = mainLight ? mainLight.color : Color.white;
        float i0 = mainLight ? mainLight.intensity : 1f;
        float t0 = mainLight ? mainLight.colorTemperature : 6500f;

        // Cache start material values per target parameter
        // We’ll read current values from the first matching material instance (safe even if reused)
        var fromList = from?.materialParams;
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
                else // Color
                {
                    Color v = Color.Lerp(startColors[i], tp.colorValue, k);
                    tp.material.SetColor(tp.propId, v);
                }
            }

            yield return null;
        }

        // Snap to final
        ApplyLight(to);
        ApplyMaterialsImmediate(to);
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

    Mood FindMood(string id) => Array.Find(moods, x => x != null && x.id == id);
}
