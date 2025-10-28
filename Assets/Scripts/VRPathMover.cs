using UnityEngine;
using Cinemachine;

public class VRPathMover : MonoBehaviour
{
    [Header("Cinemachine Components")]
    public CinemachineDollyCart dollyCart; // Cart that moves along the path

    [Header("OVR Rig")]
    public Transform ovrRig; // Assign the OVRCameraRig here (child of dollyCart)

    [Header("Movement Settings")]
    public float duration = 5f;
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Player Control")]
    public GameObject playerLocomotion; // e.g. your movement controller

    [Header("Sway Settings")]
    public float lateralSway = 0.02f;
    public float verticalBob = 0.01f;

    private float elapsedTime = 0f;
    private bool moving = false;

    public void StartMovement()
    {
        if (playerLocomotion != null)
            playerLocomotion.SetActive(false); // disable player input

        elapsedTime = 0f;
        moving = true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
            StartMovement();

        if (!moving) return;

        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / duration);
        t = easeCurve.Evaluate(t);

        dollyCart.m_Position = t * dollyCart.m_Path.PathLength;

        // Optional small head sway to feel more human
        if (ovrRig != null)
        {
            ovrRig.localPosition = new Vector3(
                Mathf.Sin(t * Mathf.PI * 2f) * lateralSway,
                Mathf.Sin(t * Mathf.PI * 4f) * verticalBob,
                0f
            );
        }

        if (elapsedTime >= duration)
            EndMovement();
    }

    private void EndMovement()
    {
        moving = false;

        if (playerLocomotion != null)
            playerLocomotion.SetActive(true);

        if (ovrRig != null)
            ovrRig.localPosition = Vector3.zero;
    }
}
