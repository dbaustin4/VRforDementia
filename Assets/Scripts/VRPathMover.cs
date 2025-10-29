using UnityEngine;
using Cinemachine;

/// <summary>
/// Moves a Cinemachine DollyCart along its path while preventing the cart from rotating the player.
/// Designed for OVRCameraRig (assign the whole OVRCameraRig Transform as ovrRig).
/// Applies subtle sway to a child offsetTransform (create a child under OVRCameraRig to receive sway).
/// </summary>
public class VRPathMover : MonoBehaviour
{
    [Header("Dolly + OVR")]
    public CinemachineDollyCart dollyCart;   // The Cinemachine DollyCart component
    public Transform ovrRig;                // The OVRCameraRig transform (child of the dollyCart)

    [Header("Optional: the child used to receive sway (must be a child of ovrRig)")]
    public Transform swayTarget;            // e.g. a small empty under OVRCameraRig to get local sway (if null, sway applied to ovrRig)

    [Header("Movement Settings")]
    public float duration = 5f;
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Player Control")]
    public GameObject playerLocomotion;     // object to disable while cinematic is running

    [Header("Sway Settings")]
    public float lateralSway = 0.02f;
    public float verticalBob = 0.01f;

    // Internal
    private float elapsedTime = 0f;
    private bool moving = false;
    private Quaternion lockedRotation;
    private Vector3 initialSwayLocalPos;
    private bool hasLoggedWarnings = false;

    private void Start()
    {
        // Safety checks
        if (dollyCart == null)
        {
            Debug.LogError("[VRPathMoverFixedRotation] dollyCart is not assigned.");
            enabled = false;
            return;
        }

        if (ovrRig == null)
        {
            Debug.LogWarning("[VRPathMoverFixedRotation] ovrRig is not assigned. Assign your OVRCameraRig (child of dollyCart).");
            // we won't disable the script; movement can still occur, but sway won't be applied
        }

        // Store initial rotation of the cart so we can enforce it each frame in LateUpdate
        lockedRotation = dollyCart.transform.rotation;

        // If no specific sway target given, use the ovrRig to apply sway (but using a child is preferred)
        if (swayTarget == null && ovrRig != null)
            swayTarget = ovrRig;

        if (swayTarget != null)
            initialSwayLocalPos = swayTarget.localPosition;

        // If this warnings haven't been logged before, tell the user to check hierarchy
        if (!hasLoggedWarnings)
        {
            if (ovrRig != null && dollyCart.transform != ovrRig.parent)
            {
                Debug.LogWarning("[VRPathMoverFixedRotation] Make sure your OVRCameraRig is a child of the DollyCart in the hierarchy so it follows the cart movement.");
            }
            hasLoggedWarnings = true;
        }
    }

    private void Update()
    {
        // For quick testing - Start on T press
        if (Input.GetKeyDown(KeyCode.T))
            StartMovement();

        if (!moving) return;

        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / Mathf.Max(0.0001f, duration));
        float eased = easeCurve.Evaluate(t);

        // Set the dolly position (units: the dolly uses PathLength by default)
        if (dollyCart.m_Path != null)
        {
            dollyCart.m_Position = eased * dollyCart.m_Path.PathLength;
        }
        else
        {
            Debug.LogError("[VRPathMoverFixedRotation] dollyCart.m_Path is null. Assign a track with waypoints.");
            EndMovement();
            return;
        }

        // Apply sway to a child (so we don't fight Cinemachine). Using localPosition is intentional.
        if (swayTarget != null)
        {
            swayTarget.localPosition = initialSwayLocalPos + new Vector3(
                Mathf.Sin(eased * Mathf.PI * 2f) * lateralSway,
                Mathf.Sin(eased * Mathf.PI * 4f) * verticalBob,
                0f
            );
        }

        if (elapsedTime >= duration)
            EndMovement();
    }

    // Important: override the cart rotation in LateUpdate to ensure Cinemachine or other systems don't overwrite it after Update
    private void LateUpdate()
    {
        if (dollyCart != null)
        {
            // Enforce the stored rotation (keeps world orientation constant)
            dollyCart.transform.rotation = lockedRotation;
        }
    }

    public void StartMovement()
    {
        if (playerLocomotion != null)
            playerLocomotion.SetActive(false);

        elapsedTime = 0f;
        moving = true;
    }

    private void EndMovement()
    {
        moving = false;

        if (playerLocomotion != null)
            playerLocomotion.SetActive(true);

        // Reset sway target local position
        if (swayTarget != null)
            swayTarget.localPosition = initialSwayLocalPos;
    }

    // Utility for debugging in the editor
    private void OnValidate()
    {
        if (duration < 0.01f) duration = 0.01f;
    }
}
