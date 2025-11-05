#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MoodFXDirector))]
public class MoodFXDirectorEditor : Editor
{
    SerializedProperty targetVolumeProp;
    SerializedProperty masterIntensityProp;
    SerializedProperty initialMoodProp;

    SerializedProperty neutralProp, happinessProp, sadnessProp, nostalgicProp, furiousProp, triggeredProp;

    void OnEnable()
    {
        targetVolumeProp = serializedObject.FindProperty("targetVolume");
        masterIntensityProp = serializedObject.FindProperty("masterIntensity");
        initialMoodProp = serializedObject.FindProperty("initialMood");

        neutralProp = serializedObject.FindProperty("Neutral");
        happinessProp = serializedObject.FindProperty("Happiness");
        sadnessProp = serializedObject.FindProperty("Sadness");
        nostalgicProp = serializedObject.FindProperty("Nostalgic");
        furiousProp = serializedObject.FindProperty("Furious");
        triggeredProp = serializedObject.FindProperty("Triggered");
    }

    public override void OnInspectorGUI()
    {
        var t = (MoodFXDirector)target;

        EditorGUILayout.PropertyField(targetVolumeProp, new GUIContent("Target Volume"));
        EditorGUILayout.Slider(masterIntensityProp, 0f, 1f, new GUIContent("Master Intensity"));
        EditorGUILayout.PropertyField(initialMoodProp, new GUIContent("Initial Mood"));

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Neutral")) initialMoodProp.enumValueIndex = (int)MoodFXDirector.MoodId.Neutral;
            if (GUILayout.Button("Happiness")) initialMoodProp.enumValueIndex = (int)MoodFXDirector.MoodId.Happiness;
            if (GUILayout.Button("Sadness")) initialMoodProp.enumValueIndex = (int)MoodFXDirector.MoodId.Sadness;
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Nostalgic")) initialMoodProp.enumValueIndex = (int)MoodFXDirector.MoodId.Nostalgic;
            if (GUILayout.Button("Furious")) initialMoodProp.enumValueIndex = (int)MoodFXDirector.MoodId.Furious;
            if (GUILayout.Button("Triggered")) initialMoodProp.enumValueIndex = (int)MoodFXDirector.MoodId.Triggered;
        }

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Apply Selected Mood (Instant)"))
        {
            serializedObject.ApplyModifiedProperties();
            t.ApplySelectedMoodImmediate(); // zero blend
            ShowToast($"Applied {t.initialMood} (Master {t.masterIntensity:0.00})");
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Click 'Load Built-in Presets' to auto-fill all moods. Then fine-tune and/or use Master Intensity.", MessageType.Info);

        if (GUILayout.Button("Load Built-in Presets"))
        {
            Undo.RecordObject(t, "Load Built-in Presets");
            t.LoadBuiltInPresets();
            EditorUtility.SetDirty(t);
            ShowToast("Mood presets loaded");
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.PropertyField(neutralProp, true);
        EditorGUILayout.PropertyField(happinessProp, true);
        EditorGUILayout.PropertyField(sadnessProp, true);
        EditorGUILayout.PropertyField(nostalgicProp, true);
        EditorGUILayout.PropertyField(furiousProp, true);
        EditorGUILayout.PropertyField(triggeredProp, true);

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(8);
        if (!t.targetVolume)
            EditorGUILayout.HelpBox("No Target Volume assigned. The script will auto-find/create one at runtime.", MessageType.Warning);
    }

    void ShowToast(string msg)
    {
        // tiny editor feedback
        Debug.Log($"[MoodFX] {msg}");
        SceneView.RepaintAll();
    }
}
#endif
