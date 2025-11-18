using UnityEngine;

public class TestTrigger : MonoBehaviour
{
    public VRCutscenePathMover mover;

    void Start()
    {
        // Automatically start cutscene 2 seconds after entering play mode
        Invoke(nameof(PlayCutscene), 2f);
    }

    void PlayCutscene()
    {
        mover.Play();
    }
}
