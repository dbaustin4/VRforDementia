#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[ExecuteInEditMode]
public class TransformToClipRecorder : MonoBehaviour
{
    public string clipName = "CutsceneAnimation";
    public float sampleRate = 60f;

    bool isRecording = false;
    float timer = 0f;
    float nextSample = 0f;

    List<Keyframe> posX = new List<Keyframe>();
    List<Keyframe> posY = new List<Keyframe>();
    List<Keyframe> posZ = new List<Keyframe>();

    List<Keyframe> rotX = new List<Keyframe>();
    List<Keyframe> rotY = new List<Keyframe>();
    List<Keyframe> rotZ = new List<Keyframe>();
    List<Keyframe> rotW = new List<Keyframe>();

    void Update()
    {
        if (!Application.isPlaying) return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (!isRecording) StartRecording();
            else StopRecording();
        }

        if (!isRecording) return;

        timer += Time.deltaTime;
        if (timer >= nextSample)
        {
            RecordFrame(timer);
            nextSample += 1f / sampleRate;
        }
    }

    void StartRecording()
    {
        Debug.Log("🎥 Recording started (press R to stop).");
        isRecording = true;
        timer = 0f;
        nextSample = 0f;

        posX.Clear(); posY.Clear(); posZ.Clear();
        rotX.Clear(); rotY.Clear(); rotZ.Clear(); rotW.Clear();
    }

    void RecordFrame(float t)
    {
        // IMPORTANT: record localPosition/localRotation (not world)
        Vector3 p = transform.localPosition;
        Quaternion r = transform.localRotation;

        posX.Add(new Keyframe(t, p.x));
        posY.Add(new Keyframe(t, p.y));
        posZ.Add(new Keyframe(t, p.z));

        rotX.Add(new Keyframe(t, r.x));
        rotY.Add(new Keyframe(t, r.y));
        rotZ.Add(new Keyframe(t, r.z));
        rotW.Add(new Keyframe(t, r.w));
    }

    void StopRecording()
    {
        if (!isRecording) return;
        isRecording = false;

        AnimationClip clip = new AnimationClip();
        clip.frameRate = sampleRate;

        clip.SetCurve("", typeof(Transform), "localPosition.x", new AnimationCurve(posX.ToArray()));
        clip.SetCurve("", typeof(Transform), "localPosition.y", new AnimationCurve(posY.ToArray()));
        clip.SetCurve("", typeof(Transform), "localPosition.z", new AnimationCurve(posZ.ToArray()));

        clip.SetCurve("", typeof(Transform), "localRotation.x", new AnimationCurve(rotX.ToArray()));
        clip.SetCurve("", typeof(Transform), "localRotation.y", new AnimationCurve(rotY.ToArray()));
        clip.SetCurve("", typeof(Transform), "localRotation.z", new AnimationCurve(rotZ.ToArray()));
        clip.SetCurve("", typeof(Transform), "localRotation.w", new AnimationCurve(rotW.ToArray()));

        string path = $"Assets/{clipName}.anim";
        AssetDatabase.CreateAsset(clip, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"💾 Saved AnimationClip: {path}");
    }
}
#endif
