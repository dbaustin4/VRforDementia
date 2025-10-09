// PerceptionDebug.cs
using UnityEngine;

public class PerceptionDebug : MonoBehaviour
{
    void OnEnable()
    {
        Shader.SetGlobalFloat("_VignetteStrength", 0.7f);  // strong vignette
        Shader.SetGlobalFloat("_Contrast", -0.35f);        // lower contrast
        Shader.SetGlobalFloat("_Desaturation", 0.4f);      // desaturate
    }
}
