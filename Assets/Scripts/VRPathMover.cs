using UnityEngine;
using Cinemachine;

/// <summary>
/// Moves a Cinemachine DollyCart along a track while keeping OVRCameraRig orientation fixed,
/// adding subtle "human-like" head motion (bounce, sway, lean) for realism.
/// </summary>
public class VRPathMover : MonoBehaviour
{
    [Header("Cinemachine & OVR Setup")]
    public CinemachineDollyCart[] dollyCarts;   // Cinemachine DollyCart component
    public Transform ovrRig;                // OVRCameraRig Transform (child of DollyCart)
    public Transform swayTarget;            // Child transform to receive sway/bounce

    [Header("Path Movement Settings")]
    public float duration = 5f;
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Range(0f, 1f)] public float startPathPercent = 0f; // Start point along path (0 = start, 1 = end)

    [Header("Human Motion Settings")]
    [Tooltip("Vertical bounce amplitude (meters)")]
    public float bobAmplitude = 0.01f;

    [Tooltip("Vertical bounce frequency (Hz)")]
    public float bobFrequency = 0.9f;

    [Tooltip("Side sway amplitude (meters)")]
    public float swayAmplitude = 0.015f;

    [Tooltip("Side sway frequency (Hz)")]
    public float swayFrequency = 0.6f;

    [Tooltip("Head lean angle (degrees)")]
    public float leanAngle = 0.1f;

    [Tooltip("Random noise intensity for small jitters")]
    public float noiseIntensity = 0.004f;

    [Tooltip("Seconds to ease-in/out the sway motion at start and end")]
    public float swayEaseTime = 0.83f;

    private int currentPathIndex = -1;
    private CinemachineDollyCart activeCart;

    // Internal state
    private float elapsedTime;
    private bool moving;
    private Quaternion lockedRotation;
    private Vector3 initialSwayLocalPos;

    //Handling Camera Jumping

    private void Start()
    {
        if (dollyCarts == null || dollyCarts.Length == 0)
        {
            Debug.LogError("[VRPathMoverHumanized] DollyCart not assigned!");
            enabled = false;
            return;
        }

        if (ovrRig == null)
            Debug.LogWarning("[VRPathMoverHumanized] OVRCameraRig not assigned (movement will still work).");

        // Default sway target = ovrRig if not set
        if (swayTarget == null && ovrRig != null)
            swayTarget = ovrRig;

        if (swayTarget != null)
            initialSwayLocalPos = swayTarget.localPosition;
    }

    private void Update()
    {
        // Test key
        if (Input.GetKeyDown(KeyCode.T) || OVRInput.GetDown(OVRInput.Button.One))
            StartNextPath();

        if (!moving) return;

        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / Mathf.Max(duration, 0.01f));
        float eased = easeCurve.Evaluate(t);

        // Move along path
        if (activeCart != null && activeCart.m_Path != null)
            activeCart.m_Position = eased * activeCart.m_Path.PathLength;

        // Human-like bounce, sway, lean
        ApplyHumanMotion(eased);

        if (elapsedTime >= duration)
            EndMovement();
    }

    private void LateUpdate()
    {
        // Enforce locked world rotation so player isn't rotated
        if (activeCart != null)
            activeCart.transform.rotation = lockedRotation;
    }

    public void StartNextPath()
    {
        currentPathIndex++;

        if (currentPathIndex >= dollyCarts.Length)
        {
            Debug.LogWarning("All paths complete");
            return;
        }

        activeCart = dollyCarts[currentPathIndex];

        if (activeCart == null || activeCart.m_Path == null)
        {
            Debug.LogError("[VRPathMoverHumanized] DollyCart or its path is not assigned!");
            return;
        }

        // ⭐ NEW — Move OVR rig to this dolly cart
        if (ovrRig != null)
        {
            ovrRig.SetParent(activeCart.transform, true);
            ovrRig.localPosition = Vector3.zero;
            ovrRig.localRotation = Quaternion.identity;
        }

        // Lock initial rotation to prevent turning (optional, can remove later)
        lockedRotation = activeCart.transform.rotation;

        // Reset start position along path
        float startPos = startPathPercent * activeCart.m_Path.PathLength;
        activeCart.m_Position = startPos;
        activeCart.transform.position = activeCart.m_Path.EvaluatePositionAtUnit(startPos, activeCart.m_PositionUnits);
        activeCart.transform.rotation = lockedRotation;

        elapsedTime = 0f;
        moving = true;
    }



    private void EndMovement()
    {
        moving = false;


        // Reset sway
        if (swayTarget != null)
        {
            swayTarget.localPosition = initialSwayLocalPos;
            swayTarget.localRotation = Quaternion.identity;
        }
    }

    private void ApplyHumanMotion(float eased)
    {
        if (swayTarget == null) return;

        // Calculate normalized intensity fade-in/out
        float fadeIn = Mathf.Clamp01(elapsedTime / swayEaseTime);
        float fadeOut = Mathf.Clamp01((duration - elapsedTime) / swayEaseTime);
        float intensity = Mathf.Min(fadeIn, fadeOut);

        // Base oscillations
        float phaseBob = Time.time * bobFrequency * Mathf.PI * 2f;
        float phaseSway = Time.time * swayFrequency * Mathf.PI * 2f;

        float bobY = Mathf.Sin(phaseBob) * bobAmplitude;
        float swayX = Mathf.Sin(phaseSway) * swayAmplitude;

        // Add random micro-noise
        float noiseX = (Mathf.PerlinNoise(Time.time * 1.2f, 0f) - 0.5f) * noiseIntensity;
        float noiseY = (Mathf.PerlinNoise(0f, Time.time * 1.7f) - 0.5f) * noiseIntensity;

        Vector3 offset = new Vector3((swayX + noiseX) * intensity, (bobY + noiseY) * intensity, 0f);
        swayTarget.localPosition = initialSwayLocalPos + offset;

        // Gentle lean with sway
        float leanZ = Mathf.Sin(phaseSway) * leanAngle * intensity;
        swayTarget.localRotation = Quaternion.Euler(0f, 0f, leanZ);
    }
}
