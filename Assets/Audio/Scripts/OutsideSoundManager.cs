using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OutsideSoundManager : MonoBehaviour
{
    [Header("Timing")]
    public float minDelay = 10f;
    public float maxDelay = 35f;

    [Header("How many sounds can stack per event?")]
    public int minSimultaneous = 1;
    public int maxSimultaneous = 2;

    [Tooltip("Chance that we play more than 1 sound in an event (0-1).")]
    [Range(0f, 1f)] public float multiSoundChance = 0.35f;

    [Header("Small human-like offset between stacked sounds")]
    public float maxStartOffsetSeconds = 0.35f;

    [Header("Volume + Pitch (OUTDOOR base)")]
    [Range(0f, 1f)] public float baseVolume = 0.25f;
    [Range(0f, 0.15f)] public float pitchVariation = 0.04f;

    [Header("Indoor muffling settings")]
    [Tooltip("When indoors, outside sounds become this much quieter (multiplies baseVolume).")]
    [Range(0f, 1f)] public float indoorVolumeMultiplier = 0.25f;

    [Tooltip("Low-pass cutoff when indoors (lower = more muffled).")]
    [Range(10f, 22000f)] public float indoorLowPassCutoff = 1200f;

    [Tooltip("Low-pass cutoff when outdoors (usually 22000 = basically no muffling).")]
    [Range(10f, 22000f)] public float outdoorLowPassCutoff = 22000f;

    [Header("Indoor / Outdoor state")]
    public bool playerIsIndoors = false;

    private readonly List<AudioSource> sources = new List<AudioSource>();
    private readonly List<AudioLowPassFilter> lowPassFilters = new List<AudioLowPassFilter>();

    void Awake()
    {
        sources.AddRange(GetComponentsInChildren<AudioSource>(true));

        lowPassFilters.Clear();

        foreach (var src in sources)
        {
            if (!src) continue;

            src.playOnAwake = false;
            src.loop = false;

            // Ensure a LowPass exists so we can muffle instantly
            var lp = src.GetComponent<AudioLowPassFilter>();
            if (lp == null) lp = src.gameObject.AddComponent<AudioLowPassFilter>();
            lp.enabled = true;

            lowPassFilters.Add(lp);
        }

        // Safety clamps
        minSimultaneous = Mathf.Max(1, minSimultaneous);
        maxSimultaneous = Mathf.Max(minSimultaneous, maxSimultaneous);

        // Apply initial state
        ApplyIndoorStateToAllSources();
    }

    void Start()
    {
        StartCoroutine(OutsideSoundRoutine());
    }

    IEnumerator OutsideSoundRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));

            // IMPORTANT: We DO NOT "continue" when indoors anymore.
            // We still play events, but they will be muffled/quieter.
            TriggerOutsideEvent();
        }
    }

    void TriggerOutsideEvent()
    {
        if (sources.Count == 0) return;

        // Decide how many to play this event
        int count = 1;
        if (Random.value < multiSoundChance)
            count = Random.Range(minSimultaneous, maxSimultaneous + 1);

        count = Mathf.Clamp(count, 1, sources.Count);

        // Pick unique sources
        List<int> indices = GetUniqueRandomIndices(count, sources.Count);

        // Determine current base multiplier depending on indoor/outdoor
        float stateVolumeMultiplier = playerIsIndoors ? indoorVolumeMultiplier : 1f;

        foreach (int idx in indices)
        {
            var src = sources[idx];
            if (!src || src.isPlaying) continue;

            // Apply volume + pitch
            src.volume = (baseVolume * stateVolumeMultiplier) * Random.Range(0.85f, 1.05f);
            src.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);

            // Apply current muffling before play
            ApplyIndoorStateToOneSource(idx);

            float offset = Random.Range(0f, maxStartOffsetSeconds);
            StartCoroutine(PlayWithDelay(src, offset));
        }
    }

    IEnumerator PlayWithDelay(AudioSource src, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (!src) yield break;

        // We allow playing even when indoors (it will be muffled via filters/volume).
        src.Play();
    }

    List<int> GetUniqueRandomIndices(int count, int maxExclusive)
    {
        HashSet<int> set = new HashSet<int>();
        while (set.Count < count)
            set.Add(Random.Range(0, maxExclusive));

        return new List<int>(set);
    }

    void ApplyIndoorStateToAllSources()
    {
        for (int i = 0; i < sources.Count; i++)
            ApplyIndoorStateToOneSource(i);
    }

    void ApplyIndoorStateToOneSource(int i)
    {
        if (i < 0 || i >= sources.Count) return;

        var src = sources[i];
        if (!src) return;

        var lp = (i < lowPassFilters.Count) ? lowPassFilters[i] : null;
        if (lp == null)
        {
            lp = src.GetComponent<AudioLowPassFilter>();
            if (lp == null) lp = src.gameObject.AddComponent<AudioLowPassFilter>();
            lp.enabled = true;
            if (i >= lowPassFilters.Count) lowPassFilters.Add(lp);
            else lowPassFilters[i] = lp;
        }

        // Set muffling cutoff
        lp.cutoffFrequency = playerIsIndoors ? indoorLowPassCutoff : outdoorLowPassCutoff;

        // If a sound is currently playing, scale it too so it instantly changes when you enter/exit.
        // NOTE: This assumes the source volume is meant to be based on baseVolume.
        // If you manually set per-source volumes in the Inspector, keep them at 1.0.
        if (src.isPlaying)
        {
            float stateVolumeMultiplier = playerIsIndoors ? indoorVolumeMultiplier : 1f;
            src.volume = baseVolume * stateVolumeMultiplier;
        }
    }

    // Hooks
    public void SetIndoors(bool indoors)
    {
        playerIsIndoors = indoors;
        ApplyIndoorStateToAllSources();
    }

    public void SetBaseVolume(float v)
    {
        baseVolume = Mathf.Clamp01(v);
        ApplyIndoorStateToAllSources();
    }
}
