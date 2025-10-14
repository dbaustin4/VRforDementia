using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LightTrigger : MonoBehaviour
{
    public LightManagerVR manager;
    public LightPresetSO preset;
    [Range(0f, 10f)] public float blendTime = 1.5f;
    public bool oneShot = true;
    public string playerTag = "Player";

    bool triggered;

    void OnTriggerEnter(Collider other)
    {
        if (triggered && oneShot) return;
        if (other.CompareTag(playerTag))
        {
            if (manager != null && preset != null)
            {
                manager.ApplyPreset(System.Array.IndexOf(manager.presets, preset), blendTime);
                triggered = true;
            }
        }
    }
}
