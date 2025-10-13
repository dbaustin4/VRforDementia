using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Simple capture of camera target into the history RT after rendering
[RequireComponent(typeof(Camera))]
public class AfterimageCapture : MonoBehaviour
{
    public AfterimageHistory history;
    ScriptableRenderPassInput inputs = ScriptableRenderPassInput.Color;

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (history != null && history.enabled)
            Graphics.Blit(src, history.GetType()
                .GetField("history", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(history) as RenderTexture);
        Graphics.Blit(src, dst);
    }
}
