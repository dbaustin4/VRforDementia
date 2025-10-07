using System.Collections;
using UnityEngine;

public class BreezeHapticsOVR : MonoBehaviour
{
    [Header("Breeze shape")]
    [Tooltip("Total length of the breeze in seconds")]
    public float totalDuration = 3.5f;

    [Tooltip("Number of small impulses over the duration")]
    public int pulses = 40;

    [Range(0f, 1f)]
    [Tooltip("Keep low for a gentle breeze")]
    public float maxAmplitude = 0.25f;

    [Range(0f, 1f)]
    [Tooltip("Base vibration pitch (0..1). Lower feels softer.")]
    public float baseFrequency = 0.35f;

    [Tooltip("How wobbly/gusty the breeze feels")]
    public float perlinSpeed = 0.45f;

    [Tooltip("Fade-in/out curve")]
    public AnimationCurve envelope = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // 👇 This makes the haptic play automatically on scene start
    private void Start()
    {
        // Change to PlayBreezeRight() if you want the right controller
        PlayBreezeLeft();
    }

    public void PlayBreezeLeft() => StartCoroutine(Play(OVRInput.Controller.LTouch));
    public void PlayBreezeRight() => StartCoroutine(Play(OVRInput.Controller.RTouch));

    private IEnumerator Play(OVRInput.Controller controller)
    {
        float step = totalDuration / Mathf.Max(1, pulses);
        float t0 = Time.time;

        for (int i = 0; i < pulses; i++)
        {
            float t = Mathf.Clamp01((Time.time - t0) / Mathf.Max(0.0001f, totalDuration));
            float env = envelope.Evaluate(t);

            // Natural “gusts” using Perlin noise
            float noise = Mathf.PerlinNoise(Time.time * perlinSpeed, i * 0.19f);

            float amp = Mathf.Clamp01(env * (0.6f * noise + 0.4f)) * maxAmplitude;
            float freq = Mathf.Clamp01(baseFrequency + (noise - 0.5f) * 0.2f);

            OVRInput.SetControllerVibration(freq, amp, controller);
            yield return new WaitForSeconds(step);
        }

        // Stop haptics at the end
        OVRInput.SetControllerVibration(0f, 0f, controller);
    }
}
