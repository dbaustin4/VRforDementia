using UnityEngine;
using Cinemachine;
using System.Collections;

public class DisableDollyOnArrival : MonoBehaviour
{
    [Header("References")]
    public CinemachineDollyCart dollyCart;

    [Header("Settings")]
    [Tooltip("How close to the end of the path before disabling.")]
    public float endThreshold = 0.05f;

    [Tooltip("Optional delay before disabling after arrival (seconds).")]
    public float disableDelay = 0.5f;

    private CinemachinePathBase _path;
    private bool _hasStopped = false;

    void Start()
    {
        if (dollyCart == null)
        {
            dollyCart = GetComponent<CinemachineDollyCart>();
        }

        _path = dollyCart.m_Path;
    }

    void Update()
    {
        if (_hasStopped || dollyCart == null || _path == null)
            return;

        // Check if we're near the end of the path
        if (dollyCart.m_Position >= _path.PathLength - endThreshold)
        {
            _hasStopped = true;
            StartCoroutine(DisableAfterDelay());
        }
    }

    private IEnumerator DisableAfterDelay()
    {
        dollyCart.m_Speed = 0f;
        yield return new WaitForSeconds(disableDelay);

        // Optional: detach rig
        if (dollyCart.transform.childCount > 0)
        {
            var xrRig = dollyCart.transform.GetChild(0);
            xrRig.SetParent(null, true);
        }

        // Just stop the cart, don't disable its GameObject
        dollyCart.enabled = false;

        // Optional: refresh hands
        yield return RefreshHands();

        Debug.Log("✅ Dolly stopped safely, rig position preserved.");
    }



    private IEnumerator RefreshHands()
    {
        // This helps fix lost references after movement
        var hands = FindObjectsOfType<Oculus.Interaction.HandJoint>();
        foreach (var hand in hands)
        {
            hand.gameObject.SetActive(false);
            yield return null; // wait one frame
            hand.gameObject.SetActive(true);
        }
    }
}
