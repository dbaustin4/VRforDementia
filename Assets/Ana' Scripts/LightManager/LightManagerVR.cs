using System.Collections;
using UnityEngine;

public class LightManagerVR : MonoBehaviour
{
    [Header("References")]
    public Light mainLight;

    [Header("Presets")]
    public LightPresetSO[] presets;

    [Header("Blend Settings")]
    [Range(0f, 10f)] public float defaultBlendTime = 1.5f;

    [Header("Flicker Settings")]
    [Range(0f, 2f)] public float flickerAmplitude = 0.2f;
    [Range(0.1f, 10f)] public float flickerSpeed = 4f;

    Coroutine blendRoutine;
    Coroutine flickerRoutine;

    void Reset()
    {
        if (mainLight == null)
            mainLight = FindObjectOfType<Light>();
    }

    // --- Public Controls ---
    public void ApplyPreset(int index) => ApplyPreset(index, defaultBlendTime);

    public void ApplyPreset(int index, float blendTime)
    {
        if (index < 0 || index >= presets.Length) return;
        if (mainLight == null) return;

        if (blendRoutine != null) StopCoroutine(blendRoutine);
        blendRoutine = StartCoroutine(BlendToPreset(presets[index], blendTime));
    }

    public void StartFlicker(float duration)
    {
        if (mainLight == null) return;
        if (flickerRoutine != null) StopCoroutine(flickerRoutine);
        flickerRoutine = StartCoroutine(Flicker(duration));
    }

    public void StopFlicker()
    {
        if (flickerRoutine != null) StopCoroutine(flickerRoutine);
        flickerRoutine = null;
    }

    // --- Coroutines ---
    IEnumerator BlendToPreset(LightPresetSO preset, float duration)
    {
        Color startColor = mainLight.color;
        float startIntensity = mainLight.intensity;
        Vector3 startEuler = mainLight.transform.rotation.eulerAngles;

        float startSpot = mainLight.type == LightType.Spot ? mainLight.spotAngle : 0f;
        float startRange = (mainLight.type == LightType.Spot || mainLight.type == LightType.Point) ? mainLight.range : 0f;

        bool startFog = RenderSettings.fog;
        Color startFogColor = RenderSettings.fogColor;
        float startFogDensity = RenderSettings.fogDensity;

        float t = 0f;
        RenderSettings.fogMode = preset.fogMode;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.001f, duration);
            float s = Mathf.SmoothStep(0f, 1f, t);

            mainLight.color = Color.Lerp(startColor, preset.color, s);
            mainLight.intensity = Mathf.Lerp(startIntensity, preset.intensity, s);
            mainLight.transform.rotation = Quaternion.Euler(Vector3.Lerp(startEuler, preset.directionEuler, s));

            if (mainLight.type == LightType.Spot && preset.applyExtraSettings)
                mainLight.spotAngle = Mathf.Lerp(startSpot, preset.spotAngle, s);
            if ((mainLight.type == LightType.Spot || mainLight.type == LightType.Point) && preset.applyExtraSettings)
                mainLight.range = Mathf.Lerp(startRange, preset.range, s);

            RenderSettings.fog = preset.enableFog;
            RenderSettings.fogColor = Color.Lerp(startFogColor, preset.fogColor, s);
            RenderSettings.fogDensity = Mathf.Lerp(startFogDensity, preset.fogDensity, s);

            yield return null;
        }

        mainLight.color = preset.color;
        mainLight.intensity = preset.intensity;
        mainLight.transform.rotation = Quaternion.Euler(preset.directionEuler);

        if (mainLight.type == LightType.Spot && preset.applyExtraSettings)
            mainLight.spotAngle = preset.spotAngle;
        if ((mainLight.type == LightType.Spot || mainLight.type == LightType.Point) && preset.applyExtraSettings)
            mainLight.range = preset.range;

        RenderSettings.fog = preset.enableFog;
        RenderSettings.fogColor = preset.fogColor;
        RenderSettings.fogDensity = preset.fogDensity;
    }

    IEnumerator Flicker(float duration)
    {
        float baseIntensity = mainLight.intensity;
        float time = 0f;
        float seed = Random.value * 1000f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float noise = Mathf.PerlinNoise(seed, Time.time * flickerSpeed);
            float offset = (noise - 0.5f) * 2f * flickerAmplitude;
            mainLight.intensity = Mathf.Clamp(baseIntensity + offset, 0f, 5f);
            yield return null;
        }

        mainLight.intensity = baseIntensity;
        flickerRoutine = null;
    }
}
