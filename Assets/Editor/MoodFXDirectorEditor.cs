#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MoodFXDirector))]
public class MoodFXDirectorEditor : Editor
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
        targetVolumeProp = serializedObject.FindProperty("targetVolume");
        masterIntensityProp = serializedObject.FindProperty("MasterIntensity");
        initialMoodProp = serializedObject.FindProperty("InitialMood");
        selectedMoodProp = serializedObject.FindProperty("_selectedMood");

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
        var t = (MoodFXDirector)target;

        EditorGUILayout.PropertyField(targetVolumeProp, new GUIContent("Target Volume"));
        EditorGUILayout.Slider(masterIntensityProp, 0f, 1f);
        EditorGUILayout.PropertyField(initialMoodProp, new GUIContent("Initial Mood"));

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Quick Moods", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawMoodButton(t, MoodFXDirector.MoodId.Neutral);
            DrawMoodButton(t, MoodFXDirector.MoodId.Happiness);
            DrawMoodButton(t, MoodFXDirector.MoodId.Sadness);
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawMoodButton(t, MoodFXDirector.MoodId.Nostalgic);
            DrawMoodButton(t, MoodFXDirector.MoodId.Furious);
            DrawMoodButton(t, MoodFXDirector.MoodId.Triggered);
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(selectedMoodProp, new GUIContent("Selected Mood"));
        if (GUILayout.Button("Apply Selected Mood (Instant)", GUILayout.Height(22)))
        {
            t.forceInstantApply();
            EditorUtility.SetDirty(t);
        }
        EditorGUILayout.EndHorizontal();

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

    private void DrawMoodButton(MoodFXDirector dir, MoodFXDirector.MoodId mood)
    {
        if (GUILayout.Button(mood.ToString(), GUILayout.Height(22)))
        {
            dir.SetSelectedMood(mood);
            dir.forceInstantApply(mood);
            EditorUtility.SetDirty(dir);
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
