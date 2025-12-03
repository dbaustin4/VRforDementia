#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AudioMoodDirector))]
public class AudioFXDirectorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the normal inspector first
        DrawDefaultInspector();

        var dir = (AudioMoodDirector)target;

        if (!dir.enableTestingTools)
            return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Testing / Preview", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Use this section to preview moods without StoryGameManager.\n" +
            "• Select a Preview Mood and click 'Apply Preview Mood'.\n" +
            "• In Play Mode, press keys 1–6 to switch moods quickly.",
            MessageType.Info
        );

        // Preview mood picker & fade
        dir.previewMood = (AudioMoodDirector.MoodId)EditorGUILayout.EnumPopup("Preview Mood", dir.previewMood);
        dir.previewFadeSeconds = EditorGUILayout.Slider("Fade Seconds", dir.previewFadeSeconds, 0.1f, 10f);

        EditorGUILayout.Space();

        if (GUILayout.Button("Apply Preview Mood"))
        {
            if (Application.isPlaying)
            {
                dir.ApplyPreviewMood();
            }
            else
            {
                Debug.LogWarning("[AudioFXDirector] Preview only works in Play Mode.");
            }
        }

        EditorGUILayout.Space();
        dir.enableKeyboardShortcuts = EditorGUILayout.Toggle(
            "Enable Keyboard Shortcuts (1–6)",
            dir.enableKeyboardShortcuts
        );
    }
}
#endif
