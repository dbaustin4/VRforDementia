using UnityEngine;

/// <summary>
/// Smooth locomotion for Meta/OVR using the left thumbstick.
/// - Movement aligned to the user's head yaw (camera forward on the horizontal plane)
/// - Uses CharacterController, with gravity and capsule auto-resize to head height
/// - Compatible with OVRCameraRig / Meta XR Rig (child of this Player)
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class SmoothLocomotionOVR : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign your OVRCameraRig or Meta XR Rig root (the object that has the tracking anchors).")]
    public Transform cameraRig; // OVRCameraRig or Meta XR Rig root

    [Tooltip("Optional: explicitly assign the CenterEyeAnchor (camera). If left empty, will auto-find by name.")]
    public Transform centerEye;

    [Header("Movement")]
    [Tooltip("Base walking speed in meters/second.")]
    public float moveSpeed = 2.0f;

    [Tooltip("Hold the A/X button to sprint at this speed (0 = disable sprint).")]
    public float sprintSpeed = 0f;

    [Tooltip("Thumbstick deadzone to ignore tiny drift.")]
    [Range(0f, 0.5f)] public float deadzone = 0.15f;

    [Header("Physics")]
    [Tooltip("Downward gravity (m/s^2).")]
    public float gravity = 9.81f;

    [Tooltip("Minimum Y velocity clamp so falling doesn't explode.")]
    public float terminalFallSpeed = 40f;

    [Tooltip("Extra downward force to keep feet planted on slopes.")]
    public float stickToGroundForce = 2f;

    [Header("Capsule Auto-Fit")]
    [Tooltip("Minimum capsule height.")]
    public float minHeight = 1.2f;

    [Tooltip("Maximum capsule height.")]
    public float maxHeight = 2.2f;

    [Tooltip("Vertical offset to place capsule center roughly at hips while head moves.")]
    public float heightToCenterOffset = 0.5f;

    private CharacterController _cc;
    private float _verticalVelocity;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();

        if (!cameraRig)
        {
            Debug.LogError("[SmoothLocomotionOVR] Please assign Camera Rig (OVRCameraRig / Meta XR Rig).");
        }

        if (!centerEye && cameraRig)
        {
            // Try common anchor names
            centerEye = cameraRig.Find("TrackingSpace/CenterEyeAnchor")
                     ?? cameraRig.Find("CenterEyeAnchor")
                     ?? cameraRig.GetComponentInChildren<Camera>()?.transform;

            if (!centerEye)
                Debug.LogWarning("[SmoothLocomotionOVR] Could not auto-find CenterEyeAnchor; assign it manually.");
        }

        // CharacterController sane defaults
        if (_cc != null)
        {
            _cc.stepOffset = 0.3f;
            _cc.slopeLimit = 55f;
            _cc.skinWidth = 0.02f;
            _cc.radius = 0.2f;
        }
    }

    void Update()
    {
        if (_cc == null || cameraRig == null || centerEye == null) return;

        // 1) Resize capsule to user height
        UpdateCapsuleToHead();

        // 2) Read move input from left thumbstick (OVRInput)
        // PrimaryThumbstick = Left stick on both Touch controllers.
        Vector2 move = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
        if (move.magnitude < deadzone) move = Vector2.zero;

        // 3) Get head yaw (ignore pitch/roll) to define forward/right on the horizontal plane
        Vector3 headForward = centerEye.forward;
        headForward.y = 0f;
        headForward.Normalize();

        Vector3 headRight = centerEye.right;
        headRight.y = 0f;
        headRight.Normalize();

        // 4) Compose world-space move direction
        Vector3 desired = headForward * move.y + headRight * move.x;
        desired.Normalize();

        // 5) Sprint (hold A on right controller or X on left; change as you prefer)
        float currentSpeed = moveSpeed;
        bool sprintRequested = OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch)
                               || OVRInput.Get(OVRInput.Button.Three, OVRInput.Controller.LTouch);
        if (sprintSpeed > 0f && sprintRequested) currentSpeed = sprintSpeed;

        // 6) Apply gravity
        if (_cc.isGrounded)
        {
            _verticalVelocity = -stickToGroundForce; // small downward force to stay grounded
        }
        else
        {
            _verticalVelocity -= gravity * Time.deltaTime;
            _verticalVelocity = Mathf.Max(_verticalVelocity, -terminalFallSpeed);
        }

        // 7) Move character
        Vector3 velocity = desired * currentSpeed;
        velocity.y = _verticalVelocity;

        _cc.Move(velocity * Time.deltaTime);
    }

    private void UpdateCapsuleToHead()
    {
        // Estimate standing height as camera Y relative to Player root
        float headHeight = Mathf.Clamp(centerEye.localPosition.y, minHeight, maxHeight);
        _cc.height = headHeight;

        // Center the capsule so it encloses the player; CharacterController.center is local
        Vector3 center = Vector3.zero;
        center.y = (_cc.height * 0.5f) - heightToCenterOffset;
        _cc.center = center;
    }
}
