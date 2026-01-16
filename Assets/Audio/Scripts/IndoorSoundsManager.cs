using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class IndoorSoundsManager : MonoBehaviour
{
    [Header("Enabled state")]
    public bool indoorActive = false;

    [Header("Timing (one-shot events)")]
    public float minDelay = 6f;
    public float maxDelay = 22f;

    [Header("Stacking (how many sounds per event)")]
    public int minSimultaneous = 1;
    public int maxSimultaneous = 2;
    [Range(0f, 1f)] public float multiSoundChance = 0.35f;
    public float maxStartOffsetSeconds = 0.25f;

    [Header("Volume + Pitch")]
    [Range(0f, 1f)] public float baseOneShotVolume = 0.22f;
    [Range(0f, 0.15f)] public float pitchVariation = 0.04f;

    [Header("Optional ambience loops (children with loop=true)")]
    public bool playLoopingAmbienceWhenActive = true;
    [Range(0f, 1f)] public float loopVolume = 0.15f;
    public float loopFadeTime = 0.6f;

    private readonly List<AudioSource> oneShots = new List<AudioSource>();
    private readonly List<AudioSource> loops = new List<AudioSource>();
    private Coroutine routine;

    void Awake()
    {
        var all = GetComponentsInChildren<AudioSource>(true);

        foreach (var src in all)
        {
            if (!src) continue;

            // Normalize defaults
            src.playOnAwake = false;

            if (src.loop) loops.Add(src);
            else oneShots.Add(src);
        }

        // Safety clamps
        minSimultaneous = Mathf.Max(1, minSimultaneous);
        maxSimultaneous = Mathf.Max(minSimultaneous, maxSimultaneous);

        // Start disabled
        StopAllSoundImmediate();
    }

    // Call this from trigger
    public void SetIndoorActive(bool active)
    {
        if (indoorActive == active) return;
        indoorActive = active;

        if (indoorActive)
        {
            if (playLoopingAmbienceWhenActive) StartCoroutine(FadeLoopsTo(loopVolume, loopFadeTime));
            if (routine == null) routine = StartCoroutine(IndoorRoutine());
        }
        else
        {
            if (routine != null) { StopCoroutine(routine); routine = null; }
            StartCoroutine(FadeLoopsTo(0f, loopFadeTime));
            // Optional: stop one-shots too (usually yes)
            StopOneShots();
        }
    }

    IEnumerator IndoorRoutine()
    {
        while (indoorActive)
        {
            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));
            if (!indoorActive) yield break;

            TriggerIndoorEvent();
        }
    }

    void TriggerIndoorEvent()
    {
        if (oneShots.Count == 0) return;

        int count = 1;
        if (Random.value < multiSoundChance)
            count = Random.Range(minSimultaneous, maxSimultaneous + 1);

        count = Mathf.Clamp(count, 1, oneShots.Count);

        List<int> indices = GetUniqueRandomIndices(count, oneShots.Count);

        foreach (int idx in indices)
        {
            var src = oneShots[idx];
            if (!src || src.isPlaying) continue;

            src.volume = baseOneShotVolume * Random.Range(0.85f, 1.05f);
            src.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);

            float offset = Random.Range(0f, maxStartOffsetSeconds);
            StartCoroutine(PlayWithDelay(src, offset));
        }
    }

    IEnumerator PlayWithDelay(AudioSource src, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (!indoorActive || !src) yield break;
        src.Play();
    }

    IEnumerator FadeLoopsTo(float target, float time)
    {
        // Ensure loops are playing before fading in
        if (target > 0f)
        {
            foreach (var l in loops)
                if (l && !l.isPlaying) l.Play();
        }

        float t = 0f;
        // Capture start volumes per loop
        var startVolumes = new float[loops.Count];
        for (int i = 0; i < loops.Count; i++)
            startVolumes[i] = loops[i] ? loops[i].volume : 0f;

        while (t < time)
        {
            t += Time.deltaTime;
            float k = (time <= 0f) ? 1f : Mathf.Clamp01(t / time);

            for (int i = 0; i < loops.Count; i++)
            {
                var l = loops[i];
                if (!l) continue;
                l.volume = Mathf.Lerp(startVolumes[i], target, k);
            }
            yield return null;
        }

        // Snap to target, then stop if faded out
        foreach (var l in loops)
        {
            if (!l) continue;
            l.volume = target;
            if (Mathf.Approximately(target, 0f) && l.isPlaying) l.Stop();
        }
    }

    List<int> GetUniqueRandomIndices(int count, int maxExclusive)
    {
        HashSet<int> set = new HashSet<int>();
        while (set.Count < count)
            set.Add(Random.Range(0, maxExclusive));

        return new List<int>(set);
    }

    void StopOneShots()
    {
        foreach (var s in oneShots)
            if (s && s.isPlaying) s.Stop();
    }

    void StopAllSoundImmediate()
    {
        foreach (var s in oneShots)
            if (s) { s.Stop(); s.volume = baseOneShotVolume; }

        foreach (var l in loops)
            if (l) { l.Stop(); l.volume = 0f; }
    }
}
