using UnityEngine;

public class IndoorSoundQueriesTrigger : MonoBehaviour
{
    [Header("Managers to control")]
    public IndoorSoundsManager indoorSounds;
    public OutsideSoundManager outsideSounds;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (indoorSounds != null)
            indoorSounds.SetIndoorActive(true);

        if (outsideSounds != null)
            outsideSounds.SetIndoors(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (indoorSounds != null)
            indoorSounds.SetIndoorActive(false);

        if (outsideSounds != null)
            outsideSounds.SetIndoors(false);
    }
}
