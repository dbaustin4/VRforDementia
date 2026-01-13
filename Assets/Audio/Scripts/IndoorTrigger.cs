using UnityEngine;

public class IndoorTrigger : MonoBehaviour
{
    public OutsideSoundManager outsideSoundManager;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        outsideSoundManager.SetIndoors(true); // or SetMuffled(true) if you rename it later
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        outsideSoundManager.SetIndoors(false);
    }
}
