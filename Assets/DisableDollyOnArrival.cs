using UnityEngine;
using Cinemachine;
using System.Collections;

public class DisableDollyOnArrival : MonoBehaviour
{
    [Tooltip("Reference to the Cinemachine Dolly Cart that moves the XR rig.")]
    public CinemachineDollyCart dollyCart;

    [Tooltip("Set this to the path length where the cart should stop.")]
    public float stopAtPosition = 0f;

    [Tooltip("Optional: delay before disabling (seconds)")]
    public float disableDelay = 0.5f;

    private bool _hasStopped = false;

    void Update()
    {
        if (dollyCart == null) return;

        // Check if we've reached (or passed) the end
        if (!_hasStopped && dollyCart.m_Position >= stopAtPosition)
        {
            _hasStopped = true;
            StartCoroutine(DisableAfterDelay());
        }
    }

    private IEnumerator DisableAfterDelay()
    {
        yield return new WaitForSeconds(disableDelay);

        // Stop motion
        dollyCart.m_Speed = 0f;

        // Disable the dolly components
        dollyCart.enabled = false;
        gameObject.SetActive(false); // optional: disable the whole dollyCart

        // Optional: re-enable the hands if they broke
        var hands = FindObjectsOfType<Oculus.Interaction.HandJoint>(); // or your own hand component
        foreach (var hand in hands)
        {
            hand.gameObject.SetActive(false);
            yield return null; // one frame
            hand.gameObject.SetActive(true);
        }

        Debug.Log("Dolly disabled and hands refreshed.");
    }
}
