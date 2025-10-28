using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(PlayableDirector))]
public class AutoPlayTimelineOnStart : MonoBehaviour
{
    private PlayableDirector director;

    void Awake()
    {
        director = GetComponent<PlayableDirector>();
    }

    void Start()
    {
        // Wait a frame to make sure everything’s initialized
        StartCoroutine(PlayAfterOneFrame());
    }

    System.Collections.IEnumerator PlayAfterOneFrame()
    {
        yield return null;
        director.Play();
    }
}
