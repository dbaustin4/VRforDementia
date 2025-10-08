using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class StoryGameManager : MonoBehaviour
{
    [Header("Data")]
    public List<Act> acts = new();         // Fill your acts/chapters/dialogues here in Inspector

    [Header("UI Hook")]
    public DialogueUI dialogueUI;           // Assign a DialogueUI in scene

    [Header("Audio")]
    public AudioSource voiceSource;         // Optional: play voice per line (assign in Inspector)

    [Header("Options")]
    public bool autoAdvanceOnVoiceEnd = true;
    public KeyCode debugAdvanceKey = KeyCode.Space; // for quick desktop testing

    // Runtime indices
    int actIndex = 0;
    int chapterIndex = 0;
    int lineIndex = -1;

    enum StoryState { Idle, Dialogue, Exploration }
    StoryState state = StoryState.Idle;

    void Start()
    {
        // Start automatically, or call StartStory() from elsewhere.
        StartStory();
    }

    void Update()
    {
        // Optional desktop debug advance
        if (Input.GetKeyDown(debugAdvanceKey))
            Advance();

        // If we auto-advance based on voice ending
        if (state == StoryState.Dialogue && autoAdvanceOnVoiceEnd && voiceSource != null && !voiceSource.isPlaying)
        {
            // Ensure we only advance once per line (basic guard)
            if (dialogueUI != null && dialogueUI.ReadyForAutoAdvance())
            {
                Advance();
            }
        }
    }

    // ========== Public API ==========

    public void StartStory()
    {
        actIndex = 0;
        chapterIndex = 0;
        lineIndex = -1;
        state = StoryState.Idle;

        if (acts.Count == 0)
        {
            Debug.LogWarning("StoryGameManager: No acts assigned.");
            return;
        }
        StartAct(actIndex);
    }

    public void Advance()
    {
        if (state == StoryState.Dialogue)
        {
            NextLine();
        }
        else if (state == StoryState.Exploration)
        {
            // In exploration we don’t auto-advance; completion should be signaled via CompleteObjectiveForCurrentChapter()
            Debug.Log("Exploration active. Call CompleteObjectiveForCurrentChapter() from your interactable.");
        }
    }

    /// <summary>
    /// Call this from an interactable, trigger, or script to finish the current exploration chapter.
    /// </summary>
    public void CompleteObjectiveForCurrentChapter()
    {
        if (state != StoryState.Exploration) return;

        CurrentChapter()?.onChapterEnd?.Invoke();
        GoToNextChapter();
    }

    // ========== Internals ==========

    void StartAct(int index)
    {
        if (index < 0 || index >= acts.Count)
        {
            Debug.Log("Story finished.");
            state = StoryState.Idle;
            dialogueUI?.Hide();
            return;
        }
        chapterIndex = 0;
        lineIndex = -1;
        Debug.Log($"Starting Act: {acts[index].actName}");
        StartChapter(chapterIndex);
    }

    void StartChapter(int index)
    {
        var ch = CurrentChapter();
        if (ch == null)
        {
            // no chapters in act — go to next act
            GoToNextAct();
            return;
        }

        // Invoke start event for the chapter we are entering
        ch.onChapterStart?.Invoke();

        // If chapter has dialogue, enter Dialogue state; else jump straight to exploration or end
        if (ch.dialogue != null && ch.dialogue.Count > 0)
        {
            state = StoryState.Dialogue;
            lineIndex = -1;
            NextLine(); // show first line
        }
        else
        {
            // No dialogue → either exploration or end immediately
            if (ch.requiresExploration)
            {
                EnterExploration(ch);
            }
            else
            {
                ch.onChapterEnd?.Invoke();
                GoToNextChapter();
            }
        }
    }

    void NextLine()
    {
        var ch = CurrentChapter();
        if (ch == null) return;

        lineIndex++;
        if (lineIndex >= ch.dialogue.Count)
        {
            // Dialogue finished
            dialogueUI?.Hide();

            if (voiceSource != null && voiceSource.isPlaying)
                voiceSource.Stop();

            if (ch.requiresExploration)
            {
                EnterExploration(ch);
            }
            else
            {
                ch.onChapterEnd?.Invoke();
                GoToNextChapter();
            }
            return;
        }

        // Show current line
        var line = ch.dialogue[lineIndex];
        dialogueUI?.ShowLine(line.speaker, line.text);

        // Play voice if present
        if (voiceSource != null)
        {
            if (line.voice != null)
            {
                voiceSource.clip = line.voice;
                voiceSource.Play();
                dialogueUI?.MarkAutoAdvanceWindow(); // allow auto-advance when clip ends
            }
            else
            {
                voiceSource.Stop();
            }
        }
    }

    void EnterExploration(Chapter ch)
    {
        state = StoryState.Exploration;
        Debug.Log($"Exploration started for Chapter: {ch.chapterName}. Wait for objective completion.");
        // Optionally show a small exploration hint in UI
        if (!string.IsNullOrWhiteSpace(ch.explorationHint))
            dialogueUI?.ShowHint(ch.explorationHint);
    }

    void GoToNextChapter()
    {
        chapterIndex++;
        var act = CurrentAct();
        if (act != null && chapterIndex < act.chapters.Count)
        {
            StartChapter(chapterIndex);
        }
        else
        {
            // Move to next act
            GoToNextAct();
        }
    }

    void GoToNextAct()
    {
        actIndex++;
        if (actIndex < acts.Count)
        {
            StartAct(actIndex);
        }
        else
        {
            // Story end
            Debug.Log("All acts finished.");
            state = StoryState.Idle;
            dialogueUI?.Hide();
        }
    }

    Act CurrentAct()
    {
        if (actIndex < 0 || actIndex >= acts.Count) return null;
        return acts[actIndex];
    }

    Chapter CurrentChapter()
    {
        var act = CurrentAct();
        if (act == null) return null;
        if (chapterIndex < 0 || chapterIndex >= act.chapters.Count) return null;
        return act.chapters[chapterIndex];
    }
}

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

    [Tooltip("Lines shown before exploration (if any).")]
    public List<DialogueLine> dialogue = new();

    [Tooltip("If true, chapter waits for an objective completion signal after dialogue.")]
    public bool requiresExploration = false;

    [Tooltip("Optional hint to show during exploration (world-space UI).")]
    [TextArea(2, 5)] public string explorationHint;

    [Tooltip("Events fired when chapter starts/ends.")]
    public UnityEvent onChapterStart;
    public UnityEvent onChapterEnd;
}

[Serializable]
public class DialogueLine
{
    public string speaker;
    [TextArea(2, 5)] public string text;
    public AudioClip voice; // optional
}
