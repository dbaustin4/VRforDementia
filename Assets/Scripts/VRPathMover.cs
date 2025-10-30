using UnityEngine;
using Cinemachine;

/// <summary>
/// Moves a Cinemachine DollyCart along a track while keeping OVRCameraRig orientation fixed,
/// adding subtle "human-like" head motion (bounce, sway, lean) for realism.
/// </summary>
public class VRPathMover : MonoBehaviour
{
    [Header("Cinemachine & OVR Setup")]
    public CinemachineDollyCart dollyCart;   // Cinemachine DollyCart component
    public Transform ovrRig;                // OVRCameraRig Transform (child of DollyCart)
    public Transform swayTarget;            // Child transform to receive sway/bounce
    public GameObject playerLocomotion;     // Player movement controller to disable during auto-move

    [Header("Path Movement Settings")]
    public float duration = 5f;
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Range(0f, 1f)] public float startPathPercent = 0f; // Start point along path (0 = start, 1 = end)

    [Header("Human Motion Settings")]
    [Tooltip("Vertical bounce amplitude (meters)")]
    public float bobAmplitude = 0.03f;

    [Tooltip("Vertical bounce frequency (Hz)")]
    public float bobFrequency = 2f;

    [Tooltip("Side sway amplitude (meters)")]
    public float swayAmplitude = 0.025f;

    [Tooltip("Side sway frequency (Hz)")]
    public float swayFrequency = 1f;

    [Tooltip("Head lean angle (degrees)")]
    public float leanAngle = 2f;

    [Tooltip("Random noise intensity for small jitters")]
    public float noiseIntensity = 0.004f;

    [Tooltip("Seconds to ease-in/out the sway motion at start and end")]
    public float swayEaseTime = 0.5f;

    // Internal state
    private float elapsedTime;
    private bool moving;
    private Quaternion lockedRotation;
    private Vector3 initialSwayLocalPos;

    private void Start()
    {
        if (dollyCart == null)
        {
            Debug.LogError("[VRPathMoverHumanized] DollyCart not assigned!");
            enabled = false;
            return;
        }

        if (ovrRig == null)
            Debug.LogWarning("[VRPathMoverHumanized] OVRCameraRig not assigned (movement will still work).");

        // Store locked rotation
        lockedRotation = dollyCart.transform.rotation;

        // Default sway target = ovrRig if not set
        if (swayTarget == null && ovrRig != null)
            swayTarget = ovrRig;

        if (swayTarget != null)
            initialSwayLocalPos = swayTarget.localPosition;
    }

    private void Update()
    {
        // Test key
        if (Input.GetKeyDown(KeyCode.T))
            StartMovement();

        if (!moving) return;

        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / Mathf.Max(duration, 0.01f));
        float eased = easeCurve.Evaluate(t);

        // Move along path
        if (dollyCart.m_Path != null)
            dollyCart.m_Position = eased * dollyCart.m_Path.PathLength;
        else
        {
            Debug.LogError("[VRPathMoverHumanized] DollyCart has no path!");
            EndMovement();
            return;
        }

        // Human-like bounce, sway, lean
        ApplyHumanMotion(eased);

        if (elapsedTime >= duration)
            EndMovement();
    }

    private void LateUpdate()
    {
        // Enforce locked world rotation so player isn't rotated
        if (dollyCart != null)
            dollyCart.transform.rotation = lockedRotation;
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

    public void StartMovement()
    {
        if (playerLocomotion != null)
            playerLocomotion.SetActive(false);

        // Reset and align to start of path
        if (dollyCart != null && dollyCart.m_Path != null)
        {
            float startPos = startPathPercent * dollyCart.m_Path.PathLength;
            dollyCart.m_Position = startPos;
            dollyCart.transform.position = dollyCart.m_Path.EvaluatePositionAtUnit(startPos, dollyCart.m_PositionUnits);
            dollyCart.transform.rotation = lockedRotation;
        }

        elapsedTime = 0f;
        moving = true;
    }

    private void EndMovement()
    {
        moving = false;

        if (playerLocomotion != null)
            playerLocomotion.SetActive(true);

        // Reset sway
        if (swayTarget != null)
        {
            swayTarget.localPosition = initialSwayLocalPos;
            swayTarget.localRotation = Quaternion.identity;
        }
    }

    private void OnValidate()
    {
        if (duration < 0.1f) duration = 0.1f;
        if (swayEaseTime < 0.1f) swayEaseTime = 0.1f;
    }
}
