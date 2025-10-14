// InputSmoothing.cs – add on your locomotion provider
using UnityEngine;
public class InputSmoothing : MonoBehaviour
{
    public float translationSmooth = 8f; public float rotationSmooth = 10f;
    Vector2 smoothed; float yawVel;
    public Vector2 GetSmoothedMove(Vector2 raw) { smoothed = Vector2.Lerp(smoothed, raw, Time.deltaTime * translationSmooth); return smoothed; }
    public float GetSmoothedTurn(float raw) { float cur = 0; cur = Mathf.SmoothDampAngle(cur, raw, ref yawVel, 1f / rotationSmooth); return cur; }
}