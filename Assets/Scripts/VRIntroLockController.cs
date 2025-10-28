using UnityEngine;
using System.Collections;

public class VRIntroLockController : MonoBehaviour
{
    [Header("References")]
    public GameObject playerRoot;          // Your OVRPlayerController or XR Origin
    public GameObject[] handObjects;       // Left + right hand prefabs
    public MonoBehaviour[] interactionScripts; // E.g., grabbers, ray interactors, move providers
    public float introDuration = 15f;      // How long to lock controls

    private bool isLocked = false;

    void Start()
    {
        StartCoroutine(PlayIntroLock());
    }

    IEnumerator PlayIntroLock()
    {
        LockInteractions(true);
        yield return new WaitForSeconds(introDuration);
        LockInteractions(false);
    }

    public void LockInteractions(bool locked)
    {
        if (isLocked == locked) return;
        isLocked = locked;

        // Disable hand objects (hide models, grabbers, etc.)
        foreach (var hand in handObjects)
        {
            if (hand != null) hand.SetActive(!locked);
        }

        // Disable locomotion + interactivity scripts
        foreach (var script in interactionScripts)
        {
            if (script != null) script.enabled = !locked;
        }

        Debug.Log(locked ? "🎬 Cutscene mode enabled (hands & movement OFF)" : "✅ Cutscene ended (controls restored)");
    }
}
