using UnityEngine;

public class SimpleObjective : MonoBehaviour
{
    public NarrativeDirector story;

    // Example: call this from a UI Button, XR event, or OnTriggerEnter, etc.
    public void Complete()
    {
        if (story != null)
        {
            story.CompleteObjectiveForCurrentChapter();
        }
        else
        {
            Debug.LogWarning("SimpleObjective: StoryGameManager not assigned.");
        }
    }

    // Example trigger usage (optional)
    private void OnTriggerEnter(Collider other)
    {
        // You can filter by tag/layer if needed
        Complete();
        // Optionally disable to avoid double firing
        enabled = false;
    }
}
