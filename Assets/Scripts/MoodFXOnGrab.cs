using UnityEngine;
using Oculus.Interaction; // part of Meta Interaction SDK

public class MoodFXOnGrab : MonoBehaviour
{
    [Header("References")]
    public Grabbable grabbable;                // The grabbable on this object
    public MoodFXDirector moodFXDirector;      // Reference to your MoodFXDirector prefab
    public MoodFXDirector.MoodId moodToTrigger = MoodFXDirector.MoodId.Happiness;
    public float blendDuration = 1f;

    private void Start()
    {
        if (grabbable == null)
            grabbable = GetComponent<Grabbable>();

        // Subscribe to grab events
        if (grabbable != null)
            grabbable.WhenPointerEventRaised += HandlePointerEvent;
    }

    private void OnDestroy()
    {
        if (grabbable != null)
            grabbable.WhenPointerEventRaised -= HandlePointerEvent;
    }

    private void HandlePointerEvent(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Select) // when grab starts
        {
            if (moodFXDirector != null)
            {
                // Smoothly transition to this mood
                moodFXDirector.CrossfadeTo(moodToTrigger, blendDuration);
            }
        }
        else if (evt.Type == PointerEventType.Unselect)
        {
            moodFXDirector.CrossfadeTo(MoodFXDirector.MoodId.Neutral, 1f);
        }

    }
}
