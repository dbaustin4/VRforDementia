using UnityEngine;

/// <summary>
/// Simple helper that optionally follows a target Transform
/// and destroys the GameObject when the AudioSource finishes playing.
/// </summary>
public class SpatialAudioFollow : MonoBehaviour
{
    public Transform target;
    public AudioSource audioSource;

    private void Update()
    {
        if (target != null)
        {
            transform.position = target.position;
        }

        if (audioSource == null)
        {
            Destroy(gameObject);
            return;
        }

        if (!audioSource.isPlaying)
        {
            Destroy(gameObject);
        }
    }
}
