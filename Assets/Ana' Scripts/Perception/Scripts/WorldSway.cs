// WorldSway.cs – put on your level root (NOT on the camera)
using UnityEngine;
public class WorldSway : MonoBehaviour
{
    public XRPerceptionEffectsController ctrl; public float maxTilt = 1.2f; public float freq = 0.25f;
    Quaternion baseRot; void Awake() { baseRot = transform.rotation; }
    void LateUpdate()
    {
        if (!ctrl) return; float a = ctrl.swayByIntensity.Evaluate(ctrl.symptomIntensity) * maxTilt;
        float t = Time.time; Quaternion r = Quaternion.Euler(Mathf.Sin(t * freq) * a, 0, Mathf.Cos(t * freq * 0.8f) * a * 0.6f);
        transform.rotation = baseRot * r;
    }
}