using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnvironmentSoundsMoodController : MonoBehaviour
{
    [Header("References")]
    public MoodFXDirector moodDirector;

    [Tooltip("Parent that contains ALL outside audio sources as children.")]
    public Transform outsideParent;

    [Tooltip("Parent that contains ALL inside audio sources as children.")]
    public Transform insideParent;

    [Header("Blend")]
    [Tooltip("Seconds to blend between mood audio settings.")]
    public float transitionSeconds = 1.0f;

    [Serializable]
    public class MoodAudioProfile
    {
        public MoodFXDirector.MoodId mood;

        [Range(0f, 2f)] public float outsideVolume = 1f;
        [Range(10f, 22000f)] public float outsideLowPassCutoff = 22000f;

        [Range(0f, 2f)] public float insideVolume = 1f;
        [Range(10f, 22000f)] public float insideLowPassCutoff = 22000f;
    }

    [Header("Mood Profiles (edit freely)")]
    public List<MoodAudioProfile> profiles = new List<MoodAudioProfile>()
    {
        new MoodAudioProfile{ mood = MoodFXDirector.MoodId.Neutral,   outsideVolume = 0.85f, outsideLowPassCutoff = 12000f, insideVolume = 0.85f, insideLowPassCutoff = 12000f },
        new MoodAudioProfile{ mood = MoodFXDirector.MoodId.Happiness, outsideVolume = 1.00f, outsideLowPassCutoff = 18000f, insideVolume = 0.80f, insideLowPassCutoff = 14000f },
        new MoodAudioProfile{ mood = MoodFXDirector.MoodId.Sadness,   outsideVolume = 0.55f, outsideLowPassCutoff =  1800f, insideVolume = 0.75f, insideLowPassCutoff =  3200f },
        new MoodAudioProfile{ mood = MoodFXDirector.MoodId.Nostalgic, outsideVolume = 0.70f, outsideLowPassCutoff =  4500f, insideVolume = 0.80f, insideLowPassCutoff =  6500f },
        new MoodAudioProfile{ mood = MoodFXDirector.MoodId.Furious,   outsideVolume = 0.90f, outsideLowPassCutoff =  2500f, insideVolume = 0.95f, insideLowPassCutoff =  2200f },
        new MoodAudioProfile{ mood = MoodFXDirector.MoodId.Triggered, outsideVolume = 0.35f, outsideLowPassCutoff =  1200f, insideVolume = 1.00f, insideLowPassCutoff =  1600f },
    };

    private readonly List<AudioSource> _outsideSources = new();
    private readonly List<AudioSource> _insideSources = new();

    private readonly Dictionary<AudioSource, float> _outsideBaseVolumes = new();
    private readonly Dictionary<AudioSource, float> _insideBaseVolumes = new();

    private Coroutine _blendRoutine;

    private void Awake()
    {
        CacheSources(outsideParent, _outsideSources, _outsideBaseVolumes);
        CacheSources(insideParent, _insideSources, _insideBaseVolumes);

        EnsureLowPassFilters(_outsideSources);
        EnsureLowPassFilters(_insideSources);
    }

    private void OnEnable()
    {
        if (moodDirector == null)
            moodDirector = FindFirstObjectByType<MoodFXDirector>();

        if (moodDirector != null)
            moodDirector.OnMoodApplied += HandleMoodApplied;
    }

    private void OnDisable()
    {
        if (moodDirector != null)
            moodDirector.OnMoodApplied -= HandleMoodApplied;
    }

    private void HandleMoodApplied(MoodFXDirector.MoodId mood)
    {
        var profile = GetProfile(mood);
        if (profile == null) return;

        if (_blendRoutine != null) StopCoroutine(_blendRoutine);
        _blendRoutine = StartCoroutine(BlendTo(profile, Mathf.Max(0.01f, transitionSeconds)));
    }

    private MoodAudioProfile GetProfile(MoodFXDirector.MoodId mood)
    {
        for (int i = 0; i < profiles.Count; i++)
            if (profiles[i] != null && profiles[i].mood == mood)
                return profiles[i];

        // fallback
        for (int i = 0; i < profiles.Count; i++)
            if (profiles[i] != null && profiles[i].mood == MoodFXDirector.MoodId.Neutral)
                return profiles[i];

        return profiles.Count > 0 ? profiles[0] : null;
    }

    private IEnumerator BlendTo(MoodAudioProfile profile, float seconds)
    {
        float startOutsideBus = GetCurrentBusMultiplier(_outsideSources, _outsideBaseVolumes);
        float startInsideBus = GetCurrentBusMultiplier(_insideSources, _insideBaseVolumes);

        float startOutsideCut = GetCurrentCutoff(_outsideSources);
        float startInsideCut = GetCurrentCutoff(_insideSources);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / seconds;
            float k = Mathf.SmoothStep(0f, 1f, t);

            SetBus(_outsideSources, _outsideBaseVolumes, Mathf.Lerp(startOutsideBus, profile.outsideVolume, k));
            SetCutoff(_outsideSources, Mathf.Lerp(startOutsideCut, profile.outsideLowPassCutoff, k));

            SetBus(_insideSources, _insideBaseVolumes, Mathf.Lerp(startInsideBus, profile.insideVolume, k));
            SetCutoff(_insideSources, Mathf.Lerp(startInsideCut, profile.insideLowPassCutoff, k));

            yield return null;
        }

        SetBus(_outsideSources, _outsideBaseVolumes, profile.outsideVolume);
        SetCutoff(_outsideSources, profile.outsideLowPassCutoff);

        SetBus(_insideSources, _insideBaseVolumes, profile.insideVolume);
        SetCutoff(_insideSources, profile.insideLowPassCutoff);

        _blendRoutine = null;
    }

    private void CacheSources(Transform parent, List<AudioSource> list, Dictionary<AudioSource, float> baseVolumes)
    {
        list.Clear();
        baseVolumes.Clear();

        if (parent == null) return;

        var sources = parent.GetComponentsInChildren<AudioSource>(true);
        foreach (var s in sources)
        {
            list.Add(s);
            baseVolumes[s] = s.volume; // store original per-source volume
        }
    }

    private void EnsureLowPassFilters(List<AudioSource> sources)
    {
        foreach (var s in sources)
        {
            if (s == null) continue;
            var lp = s.GetComponent<AudioLowPassFilter>();
            if (lp == null) lp = s.gameObject.AddComponent<AudioLowPassFilter>();
            lp.enabled = true;
            lp.cutoffFrequency = 22000f;
        }
    }

    private float GetCurrentBusMultiplier(List<AudioSource> sources, Dictionary<AudioSource, float> baseVolumes)
    {
        foreach (var s in sources)
        {
            if (s == null) continue;
            if (baseVolumes.TryGetValue(s, out float baseV) && baseV > 0.0001f)
                return s.volume / baseV;
            return 1f;
        }
        return 1f;
    }

    private void SetBus(List<AudioSource> sources, Dictionary<AudioSource, float> baseVolumes, float busMultiplier)
    {
        foreach (var s in sources)
        {
            if (s == null) continue;
            if (!baseVolumes.TryGetValue(s, out float baseV)) baseV = s.volume;
            s.volume = baseV * busMultiplier;
        }
    }

    private float GetCurrentCutoff(List<AudioSource> sources)
    {
        foreach (var s in sources)
        {
            if (s == null) continue;
            var lp = s.GetComponent<AudioLowPassFilter>();
            if (lp != null) return lp.cutoffFrequency;
        }
        return 22000f;
    }

    private void SetCutoff(List<AudioSource> sources, float cutoff)
    {
        cutoff = Mathf.Clamp(cutoff, 10f, 22000f);
        foreach (var s in sources)
        {
            if (s == null) continue;
            var lp = s.GetComponent<AudioLowPassFilter>();
            if (lp == null) lp = s.gameObject.AddComponent<AudioLowPassFilter>();
            lp.enabled = true;
            lp.cutoffFrequency = cutoff;
        }
    }
}
