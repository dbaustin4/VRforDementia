using UnityEngine;

[ExecuteAlways]
public class AfterimageHistory : MonoBehaviour
{
    public Material afterimageMat;     // Mat_Afterimage
    RenderTexture history;

    void OnEnable() { EnsureRT(); }
    void OnDisable() { if (history) { history.Release(); history = null; } }

    void EnsureRT()
    {
        if (history && (history.width == Screen.width && history.height == Screen.height)) return;
        if (history) history.Release();
        history = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.DefaultHDR) { name = "AfterimageHistory" };
        history.Create();
        if (afterimageMat) afterimageMat.SetTexture("_HistoryTex", history);
    }

    void LateUpdate()
    {
        if (!afterimageMat) return;
        EnsureRT();
        // Copy current frame (_BlitTexture result) AFTER the Full Screen Pass runs.
        // Easiest: use Camera.onPostRender-like copy via CommandBuffer is overkill here;
        // Instead, bind from a separate full-screen pass that calls this:
    }

    // Call this from a small renderer pass OR from a MonoBehaviour using OnRenderImage in Built-in.
    // For URP simplicity, we'll add a tiny component on the camera:
}
