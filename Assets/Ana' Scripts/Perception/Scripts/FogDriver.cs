using UnityEngine;

public class FogDriver : MonoBehaviour
{
    public Material fogMat;            // Mat_HallucinationFog
    [Range(0, 1)] public float intensity = 0f; // hook this to your symptom slider

    void Update()
    {
        if (!fogMat) return;
        var tint = fogMat.GetColor("_Tint");
        tint.a = Mathf.Lerp(0.0f, 0.35f, intensity); // cap alpha for comfort
        fogMat.SetColor("_Tint", tint);
    }
}
