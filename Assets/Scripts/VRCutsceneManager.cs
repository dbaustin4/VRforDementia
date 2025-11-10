using UnityEngine;

public class VRCutsceneManager : MonoBehaviour
{
    public static VRCutsceneManager Instance { get; private set; }

    [Header("OVR Player Components")]
    public OVRPlayerController ovrPlayerController;
    public OVRGrabber[] handGrabbers;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void BeginCutscene()
    {
        if (ovrPlayerController) ovrPlayerController.enabled = false;
        foreach (var grabber in handGrabbers)
        {
            if (grabber) grabber.enabled = false;
        }
    }

    public void EndCutscene()
    {
        if (ovrPlayerController) ovrPlayerController.enabled = true;
        foreach (var grabber in handGrabbers)
        {
            if (grabber) grabber.enabled = true;
        }
    }
}
