using UnityEngine;
using System.Collections;
using Cinemachine;

[RequireComponent(typeof(CinemachineDollyCart))]
public class VRCutscenePathMover : MonoBehaviour
{
    [Header("References")]
    public Transform vrPlayerRoot;                 // the top-level object of your XR rig
    public VRCutsceneManager cutsceneManager;      // optional, will be used to disable interactions
    public bool parentRuntime = true;              // parent vrRoot to the cart at runtime while playing

    [Header("Timing")]
    public float duration = 4f;                    // total time along the entire path
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Movement")]
    public bool disableInteractions = true;        // disable hands & locomotion while moving
    public bool preserveY = false;                 // keep player's Y offset (useful if ground height differs)

    [Header("Humanization")]
    public bool enableHeadBob = true;
    public float bobAmplitude = 0.02f;
    public float bobFrequency = 1.5f;
    public AnimationCurve bobCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private CinemachineDollyCart cart;
    private float pathLength;
    private Coroutine running;

    void Awake()
    {
        cart = GetComponent<CinemachineDollyCart>();
        if (cart.m_Path != null)
            pathLength = cart.m_Path.PathLength;
    }

    public void Play()
    {
        if (running != null) return;
        running = StartCoroutine(MoveRoutine());
    }

    public void Stop()
    {
        if (running != null)
        {
            StopCoroutine(running);
            running = null;
        }
    }

    IEnumerator MoveRoutine()
    {
        if (cutsceneManager == null)
            cutsceneManager = VRCutsceneManager.Instance;

        if (disableInteractions && cutsceneManager != null)
            cutsceneManager.BeginCutscene();

        // runtime-parent to cart so movement is applied
        Transform originalParent = vrPlayerRoot.parent;
        Vector3 savedLocalPos = vrPlayerRoot.localPosition;
        Quaternion savedLocalRot = vrPlayerRoot.localRotation;

        if (parentRuntime)
        {
            vrPlayerRoot.SetParent(transform, true);
        }

        float startTime = Time.time;
        float elapsed = 0f;

        // preserve initial Y offset if desired
        float yOffset = preserveY ? vrPlayerRoot.position.y : 0f;

        while (elapsed < duration)
        {
            elapsed = Time.time - startTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eval = moveCurve.Evaluate(t);

            // drive cart position (distance)
            cart.m_Position = Mathf.Lerp(0f, pathLength, eval);

            // optional small head bob: add local offset to the vrPlayerRoot (parented), not overriding head tracking
            if (enableHeadBob)
            {
                float bobT = bobCurve.Evaluate(t);
                float bob = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude * bobT;
                // apply on local Y (works because vrPlayerRoot is parented to cart)
                vrPlayerRoot.localPosition = new Vector3(vrPlayerRoot.localPosition.x,
                                                        savedLocalPos.y + bob,
                                                        vrPlayerRoot.localPosition.z);
            }

            yield return null;
        }

        // make sure we finish at the end
        cart.m_Position = pathLength;

        // optionally restore parent
        if (parentRuntime)
        {
            // unparent, keeping world transform
            vrPlayerRoot.SetParent(originalParent, true);
        }

        if (disableInteractions && cutsceneManager != null)
            cutsceneManager.EndCutscene();

        running = null;
    }
}
