#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VisualMoodDirector))]
public class VisualMoodDirectorEditor : Editor
{
    private SerializedProperty targetVolumeProp;
    private SerializedProperty masterIntensityProp;
    private SerializedProperty initialMoodProp;
    private SerializedProperty selectedMoodProp;

    private SerializedProperty neutralProp;
    private SerializedProperty happinessProp;
    private SerializedProperty sadnessProp;
    private SerializedProperty nostalgicProp;
    private SerializedProperty furiousProp;
    private SerializedProperty triggeredProp;

    private void OnEnable()
    {
        // NOTE: these names MUST match the field names in VisualMoodDirector
        targetVolumeProp = serializedObject.FindProperty("targetVolume");
        masterIntensityProp = serializedObject.FindProperty("masterIntensity");
        initialMoodProp = serializedObject.FindProperty("initialMood");
        selectedMoodProp = serializedObject.FindProperty("selectedMood");

        neutralProp = serializedObject.FindProperty("Neutral");
        happinessProp = serializedObject.FindProperty("Happiness");
        sadnessProp = serializedObject.FindProperty("Sadness");
        nostalgicProp = serializedObject.FindProperty("Nostalgic");
        furiousProp = serializedObject.FindProperty("Furious");
        triggeredProp = serializedObject.FindProperty("Triggered");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var director = (VisualMoodDirector)target;

        // Master controls
        EditorGUILayout.PropertyField(targetVolumeProp, new GUIContent("Target Volume"));
        EditorGUILayout.Slider(masterIntensityProp, 0f, 2f, new GUIContent("Master Intensity"));
        EditorGUILayout.PropertyField(initialMoodProp, new GUIContent("Initial Mood"));

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Quick Moods", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawMoodButton(director, VisualMoodDirector.MoodId.Neutral);
            DrawMoodButton(director, VisualMoodDirector.MoodId.Happiness);
            DrawMoodButton(director, VisualMoodDirector.MoodId.Sadness);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawMoodButton(director, VisualMoodDirector.MoodId.Nostalgic);
            DrawMoodButton(director, VisualMoodDirector.MoodId.Furious);
            DrawMoodButton(director, VisualMoodDirector.MoodId.Triggered);
        }

        // Selected mood dropdown + apply button
        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(selectedMoodProp, new GUIContent("Selected Mood"));
        if (GUILayout.Button("Apply Selected Mood (Instant)", GUILayout.Height(22)))
        {
            var mood = (VisualMoodDirector.MoodId)selectedMoodProp.enumValueIndex;
            director.ForceInstantApply(mood);
            EditorUtility.SetDirty(director);
        }
        EditorGUILayout.EndHorizontal();

        // Preset foldouts
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);

        DrawPresetFoldout("Neutral", neutralProp);
        DrawPresetFoldout("Happiness", happinessProp);
        DrawPresetFoldout("Sadness", sadnessProp);
        DrawPresetFoldout("Nostalgic", nostalgicProp);
        DrawPresetFoldout("Furious", furiousProp);
        DrawPresetFoldout("Triggered", triggeredProp);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawMoodButton(VisualMoodDirector director, VisualMoodDirector.MoodId mood)
    {
        if (GUILayout.Button(mood.ToString(), GUILayout.Height(22)))
        {
            // Set selectedMood to this mood
            selectedMoodProp.enumValueIndex = (int)mood;
            serializedObject.ApplyModifiedProperties();

            // Apply instantly in the scene view / play mode
            director.ForceInstantApply(mood);
            EditorUtility.SetDirty(director);
        }
    }

    private void DrawPresetFoldout(string title, SerializedProperty presetProp)
    {
        presetProp.isExpanded = EditorGUILayout.Foldout(presetProp.isExpanded, title, true);
        if (!presetProp.isExpanded) return;

        using (new EditorGUI.IndentLevelScope())
        {
            EditorGUILayout.PropertyField(presetProp.FindPropertyRelative("exposure"));
            EditorGUILayout.PropertyField(presetProp.FindPropertyRelative("contrast"));
            EditorGUILayout.PropertyField(presetProp.FindPropertyRelative("saturation"));
            EditorGUILayout.PropertyField(presetProp.FindPropertyRelative("colorFilter"));
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(presetProp.FindPropertyRelative("vignetteIntensity"));
            EditorGUILayout.PropertyField(presetProp.FindPropertyRelative("vignetteSmoothness"));
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(presetProp.FindPropertyRelative("bloomIntensity"));
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(presetProp.FindPropertyRelative("lift"));
            EditorGUILayout.PropertyField(presetProp.FindPropertyRelative("gamma"));
            EditorGUILayout.PropertyField(presetProp.FindPropertyRelative("gain"));
        }
    }
}
#endif
