using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

public class StoryGameManager : MonoBehaviour
{
    [Header("Data")]
    public List<Act> acts = new();

    [Header("UI Hook")]
    public DialogueUI dialogueUI;

    [Header("Audio (Voice)")]
    public AudioSource voiceSource;

    [Header("Mood / Lighting / Audio")]
    public MoodFXDirector moodFXDirector;   // assign in Inspector
    public AudioFXDirector audioFXDirector; // NEW: layered audio moods

    // Runtime indices
    int actIndex = 0;
    int chapterIndex = 0;
    int lineIndex = -1;

    enum StoryState { Idle, Dialogue, Exploration }
    StoryState state = StoryState.Idle;

    // Optional handle to MoodFXDirector.CrossfadeBlend if present
    MethodInfo _crossfadeBlendMI;

    void Awake()
    {
        if (moodFXDirector != null)
        {
            var t = typeof(MoodFXDirector);
            _crossfadeBlendMI = t.GetMethod(
                "CrossfadeBlend",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new Type[] {
                    typeof(MoodFXDirector.MoodId),
                    typeof(MoodFXDirector.MoodId),
                    typeof(float),
                    typeof(float)
                },
                null
            );
        }
    }

    void Start() => StartStory();

    void Update()
    {
        if (Input.GetKeyDown(debugAdvanceKey)) Advance();

        if (state == StoryState.Dialogue && autoAdvanceOnVoiceEnd && voiceSource != null && !voiceSource.isPlaying)
        {
            if (dialogueUI != null && dialogueUI.ReadyForAutoAdvance())
                Advance();
        }
    }

    [Header("Options")]
    public bool autoAdvanceOnVoiceEnd = true;
    public KeyCode debugAdvanceKey = KeyCode.Space;

    // ----- Public control -----
    public void StartStory()
    {
        actIndex = 0; chapterIndex = 0; lineIndex = -1; state = StoryState.Idle;

        if (acts.Count == 0)
        {
            Debug.LogWarning("StoryGameManager: No acts assigned.");
            return;
        }
        StartAct(actIndex);
    }

    public void Advance()
    {
        if (state == StoryState.Dialogue) NextLine();
        else if (state == StoryState.Exploration)
            Debug.Log("Exploration active. Call CompleteObjectiveForCurrentChapter() from your interactable.");
    }

    public void CompleteObjectiveForCurrentChapter()
    {
        if (state != StoryState.Exploration) return;

        var ch = CurrentChapter();

        if ((moodFXDirector || audioFXDirector) && ch != null)
            PlayCue(ch.onCompleteCue);

        ch?.onChapterEnd?.Invoke();
        GoToNextChapter();
    }

    // ----- Act / Chapter flow -----
    void StartAct(int index)
    {
        if (index < 0 || index >= acts.Count)
        {
            Debug.Log("Story finished.");
            state = StoryState.Idle;
            dialogueUI?.Hide();
            return;
        }
        chapterIndex = 0; lineIndex = -1;
        Debug.Log($"Starting Act: {acts[index].actName}");
        StartChapter(chapterIndex);
    }

    void StartChapter(int index)
    {
        var ch = CurrentChapter();
        if (ch == null) { GoToNextAct(); return; }

        // Cue: On Enter
        if (moodFXDirector || audioFXDirector)
            PlayCue(ch.onEnterCue);

        ch.onChapterStart?.Invoke();

        if (ch.dialogue != null && ch.dialogue.Count > 0)
        {
            state = StoryState.Dialogue; lineIndex = -1; NextLine();
        }
        else
        {
            if (ch.requiresExploration) EnterExploration(ch);
            else
            {
                if (moodFXDirector || audioFXDirector)
                    PlayCue(ch.onCompleteCue);

                ch.onChapterEnd?.Invoke();
                GoToNextChapter();
            }
        }
    }

    void NextLine()
    {
        var ch = CurrentChapter(); if (ch == null) return;

        lineIndex++;
        if (lineIndex >= ch.dialogue.Count)
        {
            dialogueUI?.Hide();
            if (voiceSource != null && voiceSource.isPlaying) voiceSource.Stop();

            // Cue: After Dialogue
            if (moodFXDirector || audioFXDirector)
                PlayCue(ch.afterDialogueCue);

            if (ch.requiresExploration) EnterExploration(ch);
            else
            {
                if (moodFXDirector || audioFXDirector)
                    PlayCue(ch.onCompleteCue);

                ch.onChapterEnd?.Invoke();
                GoToNextChapter();
            }
            return;
        }

        var line = ch.dialogue[lineIndex];
        dialogueUI?.ShowLine(line.speaker, line.text);

        if (voiceSource != null)
        {
            if (line.voice != null)
            {
                voiceSource.clip = line.voice;
                voiceSource.Play();
                dialogueUI?.MarkAutoAdvanceWindow();
            }
            else voiceSource.Stop();
        }
    }

    void EnterExploration(Chapter ch)
    {
        state = StoryState.Exploration;
        Debug.Log($"Exploration started for Chapter: {ch.chapterName}. Wait for objective completion.");

        // Cue: On Exploration Start
        if (moodFXDirector || audioFXDirector)
            PlayCue(ch.onExplorationStartCue);

        if (!string.IsNullOrWhiteSpace(ch.explorationHint))
            dialogueUI?.ShowHint(ch.explorationHint);
    }

    void GoToNextChapter()
    {
        chapterIndex++;
        var act = CurrentAct();
        if (act != null && chapterIndex < act.chapters.Count) StartChapter(chapterIndex);
        else GoToNextAct();
    }

    void GoToNextAct()
    {
        actIndex++;
        if (actIndex < acts.Count) StartAct(actIndex);
        else { Debug.Log("All acts finished."); state = StoryState.Idle; dialogueUI?.Hide(); }
    }

    // ----- Helpers -----
    Act CurrentAct() => (actIndex < 0 || actIndex >= acts.Count) ? null : acts[actIndex];

    Chapter CurrentChapter()
    {
        var act = CurrentAct(); if (act == null) return null;
        return (chapterIndex < 0 || chapterIndex >= act.chapters.Count) ? null : act.chapters[chapterIndex];
    }

    // ----- Cue player (Base + Overlays list) -----
    public void PlayCue(MoodProgramCue cue)
    {
        // If neither director exists, nothing to do
        if ((!moodFXDirector && !audioFXDirector) || cue == null || !cue.apply) return;

        // 1) Base mood
        if (cue.baseMood.enabled)
        {
            if (moodFXDirector)
                moodFXDirector.CrossfadeTo(cue.baseMood.mood, cue.baseMood.fadeSeconds);

            if (audioFXDirector)
                audioFXDirector.CrossfadeTo(
                    (AudioFXDirector.MoodId)cue.baseMood.mood,
                    cue.baseMood.fadeSeconds
                );
        }

        // 2) Overlays (sequential list)
        if (cue.overlays.enabled && cue.overlays.layers != null && cue.overlays.layers.Count > 0)
            StartCoroutine(OverlaySequenceRoutine(cue.baseMood.mood, cue.overlays.layers));
    }

    IEnumerator OverlaySequenceRoutine(MoodFXDirector.MoodId baseMood, List<OverlayLayer> layers)
    {
        bool canBlend = (_crossfadeBlendMI != null);

        foreach (var layer in layers)
        {
            if (layer == null) continue;

            float dur = Mathf.Max(0.05f, layer.durationSeconds);
            float step = Mathf.Clamp(layer.stepSeconds, 0.02f, 0.2f);
            float t = 0f;
            float lastW = -1f; // uninitialized

            while (t < dur)
            {
                float u = Mathf.Clamp01(t / dur);                      // normalized time 0..1
                float w = Mathf.Clamp01(layer.weightCurve.Evaluate(u)); // overlay weight 0..1

                // VISUALS
                if (moodFXDirector)
                {
                    if (canBlend)
                    {
                        // Use the high-fidelity blend if available in MoodFXDirector
                        float microFade = Mathf.Max(0.02f, layer.microFadeSeconds);
                        _crossfadeBlendMI.Invoke(moodFXDirector, new object[] { baseMood, layer.overlayMood, w, microFade });
                    }
                    else
                    {
                        // Fallback behavior: gentle nudge toward overlay when weight rises,
                        // and back to base when it falls below a threshold.
                        const float THRESH = 0.05f;
                        if (lastW < 0f) lastW = w;

                        if (w > THRESH && lastW <= THRESH)
                        {
                            // entering overlay zone
                            moodFXDirector.CrossfadeTo(layer.overlayMood, Mathf.Lerp(0.05f, layer.microFadeSeconds, w));
                        }
                        else if (w <= THRESH && lastW > THRESH)
                        {
                            // leaving overlay zone
                            moodFXDirector.CrossfadeTo(baseMood, Mathf.Lerp(0.05f, layer.microFadeSeconds, 1f - w));
                        }
                    }
                }

                // AUDIO (simple threshold-based follow of overlay weight)
                if (audioFXDirector)
                {
                    const float THRESH_AUDIO = 0.05f;
                    if (lastW < 0f) lastW = w;

                    if (w > THRESH_AUDIO && lastW <= THRESH_AUDIO)
                    {
                        // entering overlay zone (audio)
                        audioFXDirector.CrossfadeTo(
                            (AudioFXDirector.MoodId)layer.overlayMood,
                            Mathf.Lerp(0.05f, layer.microFadeSeconds, w)
                        );
                    }
                    else if (w <= THRESH_AUDIO && lastW > THRESH_AUDIO)
                    {
                        // leaving overlay zone (audio)
                        audioFXDirector.CrossfadeTo(
                            (AudioFXDirector.MoodId)baseMood,
                            Mathf.Lerp(0.05f, layer.microFadeSeconds, 1f - w)
                        );
                    }
                }

                lastW = w;
                t += step;
                yield return new WaitForSeconds(step);
            }

            // After each overlay, return to base quickly before the next overlay
            if (moodFXDirector)
                moodFXDirector.CrossfadeTo(baseMood, Mathf.Max(0.05f, layer.endReturnFadeSeconds));

            if (audioFXDirector)
                audioFXDirector.CrossfadeTo(
                    (AudioFXDirector.MoodId)baseMood,
                    Mathf.Max(0.05f, layer.endReturnFadeSeconds)
                );

            // Give it a frame to settle
            yield return null;
        }
    }

    // Optional direct helpers if you trigger from Timeline/Ink
    public void ApplyMood(MoodFXDirector.MoodId mood, float fadeSeconds = 1.0f)
    {
        if (moodFXDirector)
            moodFXDirector.CrossfadeTo(mood, fadeSeconds);

        if (audioFXDirector)
            audioFXDirector.CrossfadeTo(
                (AudioFXDirector.MoodId)mood,
                fadeSeconds
            );
    }

    public void ApplyMoodBlend(
        MoodFXDirector.MoodId baseMood,
        MoodFXDirector.MoodId overlayMood,
        float overlayWeight = 0.5f,
        float fadeSeconds = 0.5f)
    {
        if (!moodFXDirector && !audioFXDirector) return;

        overlayWeight = Mathf.Clamp01(overlayWeight);

        // VISUALS: use full blend system if available
        if (moodFXDirector)
        {
            if (_crossfadeBlendMI != null)
            {
                _crossfadeBlendMI.Invoke(moodFXDirector, new object[] { baseMood, overlayMood, overlayWeight, fadeSeconds });
            }
            else
            {
                StartCoroutine(BlendFallbackRoutine(baseMood, overlayMood, overlayWeight, fadeSeconds));
            }
        }

        // AUDIO: simple interpretation of overlay weight
        if (audioFXDirector)
        {
            if (overlayWeight <= 0.001f)
            {
                audioFXDirector.CrossfadeTo((AudioFXDirector.MoodId)baseMood, fadeSeconds);
            }
            else if (overlayWeight >= 0.999f)
            {
                audioFXDirector.CrossfadeTo((AudioFXDirector.MoodId)overlayMood, fadeSeconds);
            }
            else
            {
                float w = Mathf.Lerp(0.1f, 0.35f, overlayWeight);
                StartCoroutine(AudioBlendRoutine(baseMood, overlayMood, w, fadeSeconds));
            }
        }
    }

    IEnumerator BlendFallbackRoutine(MoodFXDirector.MoodId baseMood, MoodFXDirector.MoodId overlayMood, float w, float fade)
    {
        if (!moodFXDirector) yield break;

        moodFXDirector.CrossfadeTo(baseMood, Mathf.Max(0.05f, fade * 0.6f));
        yield return null;

        if (w > 0.001f)
        {
            float toOverlay = Mathf.Lerp(0.1f, 0.35f, w);
            moodFXDirector.CrossfadeTo(overlayMood, Mathf.Max(0.05f, fade * toOverlay));
            yield return new WaitForSeconds(Mathf.Max(0.01f, fade * toOverlay));
            moodFXDirector.CrossfadeTo(baseMood, Mathf.Max(0.05f, fade * (0.4f + (0.2f * (1f - w)))));
        }
    }

    IEnumerator AudioBlendRoutine(MoodFXDirector.MoodId baseMood, MoodFXDirector.MoodId overlayMood, float w, float fade)
    {
        if (!audioFXDirector) yield break;

        audioFXDirector.CrossfadeTo((AudioFXDirector.MoodId)baseMood, Mathf.Max(0.05f, fade * 0.6f));
        yield return null;

        if (w > 0.001f)
        {
            float toOverlay = Mathf.Lerp(0.1f, 0.35f, w);
            audioFXDirector.CrossfadeTo((AudioFXDirector.MoodId)overlayMood, Mathf.Max(0.05f, fade * toOverlay));
            yield return new WaitForSeconds(Mathf.Max(0.01f, fade * toOverlay));
            audioFXDirector.CrossfadeTo(
                (AudioFXDirector.MoodId)baseMood,
                Mathf.Max(0.05f, fade * (0.4f + (0.2f * (1f - w))))
            );
        }
    }
}

// ----- Data containers -----
[Serializable]
public class Act
{
    public string actName = "Act 1";
    public List<Chapter> chapters = new();
}

[Serializable]
public class Chapter
{
    public string chapterName = "Chapter 1";
    public List<DialogueLine> dialogue = new();
    public bool requiresExploration = false;
    [TextArea(2, 5)] public string explorationHint;

    [Header("Mood Cues")]
    public MoodProgramCue onEnterCue = new();
    public MoodProgramCue afterDialogueCue = new();
    public MoodProgramCue onExplorationStartCue = new();
    public MoodProgramCue onCompleteCue = new();

    public UnityEvent onChapterStart;
    public UnityEvent onChapterEnd;
}

[Serializable]
public class DialogueLine
{
    public string speaker;
    [TextArea(2, 5)] public string text;
    public AudioClip voice;
}

//
// ---------- Cue schema (Base + Overlays list) ----------
//

[Serializable]
public class MoodProgramCue
{
    [Tooltip("Enable/disable this cue without losing its settings.")]
    public bool apply = false;

    [Header("Base")]
    public BaseMood baseMood = new BaseMood();

    [Header("Overlays (run sequentially above base)")]
    public OverlayProgram overlays = new OverlayProgram();
}

[Serializable]
public class BaseMood
{
    public bool enabled = true;
    public MoodFXDirector.MoodId mood = MoodFXDirector.MoodId.Happiness;
    [Range(0.05f, 6f)] public float fadeSeconds = 1.2f;
}

[Serializable]
public class OverlayProgram
{
    public bool enabled = false;

    [Tooltip("Queue of overlay layers to play one after another, on top of the base mood.")]
    public List<OverlayLayer> layers = new List<OverlayLayer>();
}

[Serializable]
public class OverlayLayer
{
    [Header("Overlay Layer")]
    public string label = "Overlay";
    public MoodFXDirector.MoodId overlayMood = MoodFXDirector.MoodId.Sadness;

    [Tooltip("Total time this overlay layer runs over the base.")]
    [Range(0.1f, 20f)] public float durationSeconds = 3f;

    [Tooltip("Overlay weight over normalized time (0..1). Y=weight 0..1. This curve is ALWAYS applied on top of the base mood.")]
    public AnimationCurve weightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Smoothing")]
    [Tooltip("Micro-fade used for each incremental blend step (smooths updates).")]
    [Range(0.01f, 1.0f)] public float microFadeSeconds = 0.15f;

    [Tooltip("How often to update the overlay blend.")]
    [Range(0.02f, 0.2f)] public float stepSeconds = 0.05f;

    [Tooltip("When this overlay finishes, fade back to base by this amount (before next layer).")]
    [Range(0.01f, 3f)] public float endReturnFadeSeconds = 0.25f;
}
