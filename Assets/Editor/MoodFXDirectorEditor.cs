#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MoodFXDirector))]
public class MoodFXDirectorEditor : Editor
{
    // Works with either "currentMood" or "startMood"
    SerializedProperty moodProp;             // current/start
    SerializedProperty targetVolumeProp;

    SerializedProperty NeutralProp;
    SerializedProperty HappinessProp;
    SerializedProperty SadnessProp;
    SerializedProperty NostalgicProp;
    SerializedProperty FuriousProp;
    SerializedProperty TriggeredProp;

    void OnEnable()
    {
        // Try both names so it matches whatever your runtime uses
        moodProp = serializedObject.FindProperty("currentMood");
        if (moodProp == null) moodProp = serializedObject.FindProperty("startMood");

        targetVolumeProp = serializedObject.FindProperty("targetVolume");

        NeutralProp = serializedObject.FindProperty("Neutral");
        HappinessProp = serializedObject.FindProperty("Happiness");
        SadnessProp = serializedObject.FindProperty("Sadness");
        NostalgicProp = serializedObject.FindProperty("Nostalgic");
        FuriousProp = serializedObject.FindProperty("Furious");
        TriggeredProp = serializedObject.FindProperty("Triggered");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (targetVolumeProp != null) EditorGUILayout.PropertyField(targetVolumeProp);

        // Mood selector (label adapts)
        if (moodProp != null)
        {
            string niceLabel = moodProp.name == "startMood" ? "Start / Current Mood" : "Current Mood";
            EditorGUILayout.PropertyField(moodProp, new GUIContent(niceLabel));

            // Quick row of buttons for instant testing
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Neutral")) SetMoodAndApply(0);
                if (GUILayout.Button("Happiness")) SetMoodAndApply(1);
                if (GUILayout.Button("Sadness")) SetMoodAndApply(2);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Nostalgic")) SetMoodAndApply(3);
                if (GUILayout.Button("Furious")) SetMoodAndApply(4);
                if (GUILayout.Button("Triggered")) SetMoodAndApply(5);
            }

            if (GUILayout.Button("Apply Selected Mood")) ApplyNow();
        }

        // Presets (only if your runtime exposes them)
        DrawPreset("Neutral", NeutralProp);
        DrawPreset("Happiness", HappinessProp);
        DrawPreset("Sadness", SadnessProp);
        DrawPreset("Nostalgic", NostalgicProp);
        DrawPreset("Furious", FuriousProp);
        DrawPreset("Triggered", TriggeredProp);

        serializedObject.ApplyModifiedProperties();
    }

    void SetMoodAndApply(int enumIndex)
    {
        if (moodProp == null) return;
        moodProp.enumValueIndex = enumIndex;
        serializedObject.ApplyModifiedProperties();
        ApplyNow();
    }

    void ApplyNow()
    {
        foreach (var t in targets)
        {
            var dir = (MoodFXDirector)t;
            // Your runtime already has ApplyCurrentMood(); call it to push values into the Volume
            dir.ApplyCurrentMood();
            EditorUtility.SetDirty(dir);
        }
    }

    void DrawPreset(string label, SerializedProperty preset)
    {
        if (preset == null) return;

        using (new EditorGUILayout.VerticalScope("box"))
        {
            preset.isExpanded = EditorGUILayout.Foldout(preset.isExpanded, label, true);
            if (!preset.isExpanded) return;

            EditorGUI.indentLevel++;

            DrawIfExists(preset, "exposure");
            DrawIfExists(preset, "contrast");
            DrawIfExists(preset, "saturation");
            DrawIfExists(preset, "colorFilter");

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Vignette", EditorStyles.boldLabel);
            DrawIfExists(preset, "vignetteIntensity");
            DrawIfExists(preset, "vignetteSmoothness");

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Bloom", EditorStyles.boldLabel);
            DrawIfExists(preset, "bloomIntensity");

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Tone (Lift / Gamma / Gain)", EditorStyles.boldLabel);
            DrawIfExists(preset, "lift");
            DrawIfExists(preset, "gamma");
            DrawIfExists(preset, "gain");

            EditorGUI.indentLevel--;
        }
    }

    void DrawIfExists(SerializedProperty parent, string child)
    {
        var p = parent.FindPropertyRelative(child);
        if (p != null) EditorGUILayout.PropertyField(p);
    }
}
#endif
