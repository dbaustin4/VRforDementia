using UnityEngine;
using System.Collections.Generic;

public class VRCutsceneManager : MonoBehaviour
{
    public static VRCutsceneManager Instance { get; private set; }

    [Header("Core References")]
    [Tooltip("Your player root (e.g. OVRPlayerController or XROrigin)")]
    public GameObject playerRoot;

    [Tooltip("Hand GameObjects (Left + Right)")]
    public List<GameObject> handObjects = new List<GameObject>();

    [Tooltip("Scripts controlling movement, grabbing, teleport, etc.")]
    public List<MonoBehaviour> interactionScripts = new List<MonoBehaviour>();

    [Tooltip("If true, hides hands instead of disabling them.")]
    public bool hideHandsInstead = false;

    private bool isCutsceneActive = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void BeginCutscene()
    {
        if (isCutsceneActive) return;
        isCutsceneActive = true;
        LockInteractions(true);
        Debug.Log("🎬 Cutscene started — controls disabled");
    }

    public void EndCutscene()
    {
        if (!isCutsceneActive) return;
        isCutsceneActive = false;
        LockInteractions(false);
        Debug.Log("✅ Cutscene ended — controls restored");
    }

    private void LockInteractions(bool locked)
    {
        foreach (var script in interactionScripts)
        {
            if (script != null)
                script.enabled = !locked;
        }

        foreach (var hand in handObjects)
        {
            if (hand == null) continue;

            if (hideHandsInstead)
            {
                foreach (var renderer in hand.GetComponentsInChildren<Renderer>(true))
                    renderer.enabled = !locked;
            }
            else
            {
                hand.SetActive(!locked);
            }
        }
    }
}
