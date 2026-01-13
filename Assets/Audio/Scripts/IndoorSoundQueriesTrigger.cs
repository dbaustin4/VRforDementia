using UnityEngine;

public class IndoorSoundQueriesTrigger : MonoBehaviour
{
    public IndoorSoundsManager indoorSounds;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        indoorSounds.SetIndoorActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        indoorSounds.SetIndoorActive(false);
    }
}
