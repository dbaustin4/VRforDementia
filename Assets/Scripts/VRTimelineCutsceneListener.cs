using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(PlayableDirector))]
public class VRTimelineCutsceneListener : MonoBehaviour
{
    private PlayableDirector director;
    private bool subscribed = false;

    void Awake()
    {
        director = GetComponent<PlayableDirector>();
    }

    void OnEnable()
    {
        if (director == null) return;

        if (!subscribed)
        {
            director.played += OnTimelineStarted;
            director.stopped += OnTimelineStopped;
            subscribed = true;
        }
    }

    void OnDisable()
    {
        if (subscribed && director != null)
        {
            director.played -= OnTimelineStarted;
            director.stopped -= OnTimelineStopped;
            subscribed = false;
        }
    }

    private void OnTimelineStarted(PlayableDirector dir)
    {
        if (VRCutsceneManager.Instance != null)
            VRCutsceneManager.Instance.BeginCutscene();
    }

    private void OnTimelineStopped(PlayableDirector dir)
    {
        if (VRCutsceneManager.Instance != null)
            VRCutsceneManager.Instance.EndCutscene();
    }
}
