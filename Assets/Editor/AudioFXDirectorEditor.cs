#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AudioMoodDirector))]
public class AudioFXDirectorEditor : Editor
{
    // Serialized fields that exist on AudioMoodDirector
    SerializedProperty ambienceSourceProp;
    SerializedProperty musicSourceProp;
    SerializedProperty sfxSourceProp;

    SerializedProperty moodPresetsProp;
    SerializedProperty startMoodProp;
    SerializedProperty defaultFadeDurationProp;
    SerializedProperty logTransitionsProp;

    // Local (editor-only) test controls
    private AudioMoodDirector.MoodId previewMood = AudioMoodDirector.MoodId.Neutral;
    private float previewFadeSeconds = 1.5f;

    void OnEnable()
    {
        ambienceSourceProp = serializedObject.FindProperty("ambienceSource");
        musicSourceProp = serializedObject.FindProperty("musicSource");
        sfxSourceProp = serializedObject.FindProperty("sfxSource");

        moodPresetsProp = serializedObject.FindProperty("moodPresets");
        startMoodProp = serializedObject.FindProperty("startMood");
        defaultFadeDurationProp = serializedObject.FindProperty("defaultFadeDuration");
        logTransitionsProp = serializedObject.FindProperty("logTransitions");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var director = (AudioMoodDirector)target;

        EditorGUILayout.LabelField("Audio Sources", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(ambienceSourceProp);
        EditorGUILayout.PropertyField(musicSourceProp);
        EditorGUILayout.PropertyField(sfxSourceProp);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Mood Presets", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(moodPresetsProp, true);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Defaults", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(startMoodProp, new GUIContent("Start Mood"));
        EditorGUILayout.PropertyField(defaultFadeDurationProp, new GUIContent("Default Fade Duration (s)"));

        EditorGUILayout.Space(8);
        EditorGUILayout.PropertyField(logTransitionsProp, new GUIContent("Log Transitions"));

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Testing (Editor Only)", EditorStyles.boldLabel);

        previewMood = (AudioMoodDirector.MoodId)
            EditorGUILayout.EnumPopup("Preview Mood", previewMood);
        previewFadeSeconds = EditorGUILayout.FloatField("Preview Fade Seconds", previewFadeSeconds);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Apply Preview (Instant)"))
            {
                director.SetMoodInstant(previewMood);
                EditorUtility.SetDirty(director);
            }

            if (GUILayout.Button("Crossfade Preview"))
            {
                director.CrossfadeTo(previewMood, previewFadeSeconds);
                EditorUtility.SetDirty(director);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
