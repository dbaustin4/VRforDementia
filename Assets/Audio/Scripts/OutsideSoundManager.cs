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

    [Header("Volume + Pitch")]
    [Range(0f, 1f)] public float baseVolume = 0.25f;
    [Range(0f, 0.15f)] public float pitchVariation = 0.04f;

    [Header("Indoor / Outdoor")]
    public bool playerIsIndoors = false;

    private readonly List<AudioSource> sources = new List<AudioSource>();

    void Awake()
    {
        sources.AddRange(GetComponentsInChildren<AudioSource>(true));

        foreach (var src in sources)
        {
            src.playOnAwake = false;
            src.loop = false;
        }

        // Safety clamps
        minSimultaneous = Mathf.Max(1, minSimultaneous);
        maxSimultaneous = Mathf.Max(minSimultaneous, maxSimultaneous);
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

            if (playerIsIndoors) continue;

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

        // Play them with slight offsets
        foreach (int idx in indices)
        {
            var src = sources[idx];
            if (!src || src.isPlaying) continue;

            src.volume = baseVolume * Random.Range(0.85f, 1.05f);
            src.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);

            float offset = Random.Range(0f, maxStartOffsetSeconds);
            StartCoroutine(PlayWithDelay(src, offset));
        }
    }

    IEnumerator PlayWithDelay(AudioSource src, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (!src || playerIsIndoors) yield break;
        src.Play();
    }

    List<int> GetUniqueRandomIndices(int count, int maxExclusive)
    {
        // Simple unique sampler (count is small, so this is fine)
        HashSet<int> set = new HashSet<int>();
        while (set.Count < count)
            set.Add(Random.Range(0, maxExclusive));

        return new List<int>(set);
    }

    // Hooks
    public void SetIndoors(bool indoors) => playerIsIndoors = indoors;

    public void SetBaseVolume(float v) => baseVolume = Mathf.Clamp01(v);
}
