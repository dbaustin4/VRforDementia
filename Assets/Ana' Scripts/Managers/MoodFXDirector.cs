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
        public Color lift;   // shadows
        public Color gamma;  // midtones
        public Color gain;   // highlights
    }

    // ---------- Inspector ----------
    [Header("Volume (assign OR leave empty to auto-find/create)")]
    public Volume targetVolume;

    [Range(0f, 1f)] public float MasterIntensity = 0.55f; // bump to ~0.65 if you want stronger separation
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
        // ===== BASELINE (very light touch) =====
        Neutral = new MoodPreset
        {
            exposure = 0f,
            contrast = 0f,
            saturation = 0f,
            colorFilter = Color.white,
            vignetteIntensity = 0.10f,
            vignetteSmoothness = 0.25f,
            bloomIntensity = 0.30f,
            lift = new Color(0f, 0f, 0f, 0f),
            gamma = new Color(0f, 0f, 0f, 0f),
            gain = new Color(0f, 0f, 0f, 0f)
        };

        // HAPPINESS — vibrant, sunlit, summer-day brightness
        Happiness = Neutral;
        Happiness.exposure = 0.35f;                      // bright daylight
        Happiness.contrast = 20f;                        // crisp, clear edges
        Happiness.saturation = 45f;                      // rich colors, popping like summer
        Happiness.bloomIntensity = 0.90f;                // sunlight glow
        Happiness.vignetteIntensity = 0.05f;             // open and airy
        Happiness.vignetteSmoothness = 0.65f;
        Happiness.colorFilter = new Color(1.10f, 1.05f, 0.95f, 1f);  // gentle warm tone, clean whites
        Happiness.lift = new Color(0.01f, 0.01f, 0.00f, 0f);        // subtle brightness in shadows
        Happiness.gamma = new Color(0.02f, 0.02f, 0.00f, 0f);        // mids keep warmth
        Happiness.gain = new Color(0.06f, 0.04f, 0.02f, 0f);        // radiant highlights



        // ===== SADNESS — darker/colder, clearly muted =====
        // Feedback: make it darker, more muted, more distinct from Triggered.
        Sadness = Neutral;
        Sadness.exposure = -0.35f;
        Sadness.contrast = -5f;
        Sadness.saturation = -35f;
        Sadness.bloomIntensity = 0.15f;
        Sadness.vignetteIntensity = 0.32f;
        Sadness.vignetteSmoothness = 0.65f;
        Sadness.colorFilter = new Color(0.82f, 0.90f, 1.08f, 1f);    // cool, slightly bluish
        Sadness.lift = new Color(-0.03f, -0.03f, -0.02f, 0f);       // heavier shadows
        Sadness.gamma = new Color(-0.02f, -0.02f, -0.02f, 0f);       // dimmer mids

        // NOSTALGIC — deep sepia memory filter
        Nostalgic = Neutral;
        Nostalgic.exposure = -0.05f;                       // slightly dimmed
        Nostalgic.contrast = -35f;                         // faded film contrast
        Nostalgic.saturation = -40f;                       // near-monochrome
        Nostalgic.bloomIntensity = 0.65f;                  // dreamy light bleed
        Nostalgic.vignetteIntensity = 0.28f;               // soft edge darkening
        Nostalgic.vignetteSmoothness = 0.70f;
        Nostalgic.colorFilter = new Color(1.20f, 1.05f, 0.80f, 1f);  // strong golden sepia
        Nostalgic.lift = new Color(0.06f, 0.04f, 0.02f, 0f);        // lifted blacks (film fade)
        Nostalgic.gamma = new Color(0.02f, 0.01f, -0.01f, 0f);       // soft mids, warm
        Nostalgic.gain = new Color(0.04f, 0.02f, -0.02f, 0f);       // warm, creamy highlights



        // FURIOUS — brutal, cold-red (crimson), oppressive
        Furious = Neutral;
        Furious.exposure = 0.20f;
        Furious.contrast = 65f;                            // very punchy
        Furious.saturation = 30f;
        Furious.bloomIntensity = 0.05f;                    // no cozy glow
        Furious.vignetteIntensity = 0.55f;                 // heavy tunnel
        Furious.vignetteSmoothness = 0.60f;
        Furious.colorFilter = new Color(1.12f, 0.78f, 0.92f, 1f);   // cold red (leaning magenta/blue)
        Furious.lift = new Color(-0.07f, -0.06f, -0.08f, 0f);      // cold dark shadows
        Furious.gamma = new Color(-0.01f, -0.02f, 0.00f, 0f);       // damp greens a touch
        Furious.gain = new Color(0.10f, -0.02f, 0.02f, 0f);        // searing crimson highlights



        // TRIGGERED — anxious, purple-leaning tension
        Triggered = Neutral;
        Triggered.exposure = -0.30f;                       // constricted/airless
        Triggered.contrast = 45f;                          // tense micro-contrast
        Triggered.saturation = -18f;                       // muted but not grey
        Triggered.bloomIntensity = 0.06f;                  // crisp, no softness
        Triggered.vignetteIntensity = 0.50f;               // claustrophobic
        Triggered.vignetteSmoothness = 0.62f;
        Triggered.colorFilter = new Color(0.96f, 0.88f, 1.10f, 1f); // purple bias (uneasy)
        Triggered.lift = new Color(-0.05f, -0.03f, -0.01f, 0f);    // shadows closing in
        Triggered.gamma = new Color(0.00f, 0.01f, 0.03f, 0f);       // blue/purple mids
        Triggered.gain = new Color(0.02f, 0.00f, 0.06f, 0f);       // nervous highlights

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
