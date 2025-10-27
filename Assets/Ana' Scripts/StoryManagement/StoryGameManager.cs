using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class StoryGameManager : MonoBehaviour
{
    [Header("Data")]
    public List<Act> acts = new();

    [Header("UI Hook")]
    public DialogueUI dialogueUI;

    [Header("Audio")]
    public AudioSource voiceSource;

    [Header("Options")]
    public bool autoAdvanceOnVoiceEnd = true;
    public KeyCode debugAdvanceKey = KeyCode.Space;

    [Header("Mood / Lighting")]
    public MoodFXDirector moodFXDirector;   // assign in Inspector

    // Runtime indices
    int actIndex = 0;
    int chapterIndex = 0;
    int lineIndex = -1;

    enum StoryState { Idle, Dialogue, Exploration }
    StoryState state = StoryState.Idle;

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

        if (moodFXDirector && ch != null && ch.onComplete.apply)
            moodFXDirector.CrossfadeTo(ch.onComplete.moodId, ch.onComplete.fadeSeconds);

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

        // Mood when entering chapter
        if (moodFXDirector && ch.onEnter.apply)
            moodFXDirector.CrossfadeTo(ch.onEnter.moodId, ch.onEnter.fadeSeconds);

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
                if (moodFXDirector && ch.onComplete.apply)
                    moodFXDirector.CrossfadeTo(ch.onComplete.moodId, ch.onComplete.fadeSeconds);

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

            // Mood after dialogue block
            if (moodFXDirector && ch.afterDialogue.apply)
                moodFXDirector.CrossfadeTo(ch.afterDialogue.moodId, ch.afterDialogue.fadeSeconds);

            if (ch.requiresExploration) EnterExploration(ch);
            else
            {
                if (moodFXDirector && ch.onComplete.apply)
                    moodFXDirector.CrossfadeTo(ch.onComplete.moodId, ch.onComplete.fadeSeconds);

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

        if (moodFXDirector && ch.onExplorationStart.apply)
            moodFXDirector.CrossfadeTo(ch.onExplorationStart.moodId, ch.onExplorationStart.fadeSeconds);

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

    // Optional: public helper to trigger moods from UI/Timeline/Ink tags
    public void ApplyMood(MoodFXDirector.MoodId mood, float fadeSeconds = 1.0f)
    {
        if (moodFXDirector) moodFXDirector.CrossfadeTo(mood, fadeSeconds);
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
public class MoodCue
{
    public bool apply = false;
    public MoodFXDirector.MoodId moodId = MoodFXDirector.MoodId.Happiness; // enum dropdown
    [Range(0.1f, 5f)] public float fadeSeconds = 1.2f;
}

[Serializable]
public class Chapter
{
    public string chapterName = "Chapter 1";
    public List<DialogueLine> dialogue = new();
    public bool requiresExploration = false;
    [TextArea(2, 5)] public string explorationHint;

    [Header("Mood Cues")]
    public MoodCue onEnter = new();
    public MoodCue afterDialogue = new();
    public MoodCue onExplorationStart = new();
    public MoodCue onComplete = new();

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
