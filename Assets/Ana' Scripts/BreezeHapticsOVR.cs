using System.Collections;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
#endif

/// <summary>
/// Breeze-style haptics for Meta Quest Touch controllers (OpenXR).
/// Works with OVRInput and (optionally) Unity Input System fallback.
/// Attach to any active GameObject in your scene.
/// </summary>
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

    [Header("Behaviour")]
    [Tooltip("Auto-play a test buzz + breeze on Start")]
    public bool playOnStart = true;

    [Tooltip("Extra wait so the app has input focus before haptics")]
    public float startDelay = 0.35f;

    [Tooltip("Also send haptics through the Unity Input System (OpenXR)")]
    public bool useInputSystemFallback = true;

    [Tooltip("Log what the script is doing")]
    public bool logDebug = true;

    private void Start()
    {
        if (playOnStart)
            StartCoroutine(PlayWhenReady());
    }

    private IEnumerator PlayWhenReady()
    {
        // Initial delay so Quest gains input focus
        yield return new WaitForSeconds(startDelay);

        // If OVRManager exists, wait briefly for input focus (safeguard)
        if (OVRManager.instance != null)
        {
            float t = 0f, timeout = 2f;
            while (!OVRManager.hasInputFocus && t < timeout)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        var ctrl = GetBestController();
        if (ctrl == OVRInput.Controller.None)
        {
            if (logDebug) Debug.LogWarning("[BreezeHapticsOVR] No Touch controller connected. Press any button to wake a controller.");
            yield break;
        }

        if (logDebug) Debug.Log("[BreezeHapticsOVR] Using controller: " + ctrl);

        // Strong test buzz so you can't miss it
        SendHaptics(ctrl, 0.9f, 0.9f, 0.20f);
        yield return new WaitForSeconds(0.22f);
        SendHaptics(ctrl, 0f, 0f, 0f); // stop

        // Then play the breeze
        yield return StartCoroutine(Play(ctrl));
    }

    // Public triggers (e.g., from UI Buttons)
    public void PlayBreezeLeft() => StartCoroutine(Play(OVRInput.Controller.LTouch));
    public void PlayBreezeRight() => StartCoroutine(Play(OVRInput.Controller.RTouch));

    private IEnumerator Play(OVRInput.Controller controller)
    {
        float step = totalDuration / Mathf.Max(1, pulses);
        float t0 = Time.time;

        for (int i = 0; i < pulses; i++)
        {
            float tNorm = Mathf.Clamp01((Time.time - t0) / Mathf.Max(0.0001f, totalDuration));
            float env = envelope.Evaluate(tNorm);

            // Natural “gusts”
            float noise = Mathf.PerlinNoise(Time.time * perlinSpeed, i * 0.19f);

            float amp = Mathf.Clamp01(env * (0.6f * noise + 0.4f)) * maxAmplitude; // 0..1
            float freq = Mathf.Clamp01(baseFrequency + (noise - 0.5f) * 0.2f);     // 0..1

            // Send haptics for this pulse slice
            SendHaptics(controller, freq, amp, step * 0.95f);
            yield return new WaitForSeconds(step);
        }

        // Ensure stop
        SendHaptics(controller, 0f, 0f, 0f);
    }

    // --- Helpers ----------------------------------------------------------------

    private OVRInput.Controller GetBestController()
    {
        // Prefer the active one
        var active = OVRInput.GetActiveController();
        if (IsControllerUsable(active)) return active;

        // Try right then left
        if (IsControllerUsable(OVRInput.Controller.RTouch)) return OVRInput.Controller.RTouch;
        if (IsControllerUsable(OVRInput.Controller.LTouch)) return OVRInput.Controller.LTouch;

        return OVRInput.Controller.None;
    }

    private bool IsControllerUsable(OVRInput.Controller c)
    {
        if (c == OVRInput.Controller.None) return false;
        return OVRInput.IsControllerConnected(c);
    }

    private void SendHaptics(OVRInput.Controller ctrl, float frequency, float amplitude, float duration)
    {
        // OVR path (Meta SDK)
        OVRInput.SetControllerVibration(frequency, amplitude, ctrl);

        // Input System fallback (OpenXR path)
        if (useInputSystemFallback)
            SendHapticFallback(amplitude, Mathf.Max(0f, duration), IsLeft(ctrl));

        if (logDebug && amplitude > 0f)
            Debug.Log($"[BreezeHapticsOVR] Buzz -> ctrl:{ctrl} amp:{amplitude:F2} freq:{frequency:F2} dur:{duration:F2}");
    }

    private static bool IsLeft(OVRInput.Controller c)
        => (c & OVRInput.Controller.LTouch) == OVRInput.Controller.LTouch;

    private void SendHapticFallback(float amplitude, float duration, bool left)
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            XRController dev = left ? XRController.leftHand : XRController.rightHand;
            if (dev != null)
            {
                dev.SendHapticImpulse(0, Mathf.Clamp01(amplitude), duration);
            }
#endif
    }
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        catch { /* ignore */ }
#endif
}

