using UnityEngine;

[CreateAssetMenu(fileName = "LightPreset", menuName = "VR/Light Preset", order = 0)]
public class LightPresetSO : ScriptableObject
{
    [Header("Main Light Settings")]
    public Color color = Color.white;
    [Range(0f, 5f)] public float intensity = 1f;
    public Vector3 directionEuler = new Vector3(50f, -30f, 0f);

    [Header("Extra (only used for Spot/Point)")]
    public bool applyExtraSettings = false;
    [Range(1f, 179f)] public float spotAngle = 40f;
    [Range(0f, 25f)] public float range = 10f;

    [Header("Fog Settings (optional)")]
    public bool enableFog = false;
    public Color fogColor = new Color(0.7f, 0.8f, 0.9f);
    [Range(0f, 0.1f)] public float fogDensity = 0.01f;
    public FogMode fogMode = FogMode.Exponential;
}
