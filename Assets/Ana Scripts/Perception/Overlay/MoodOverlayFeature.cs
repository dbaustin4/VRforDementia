// Assets/Perception/Overlay/MoodOverlayFeature.cs
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class MoodOverlayFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Tooltip("Material that uses the Perception/MoodOverlay_Combined shader.")]
        public Material overlayMaterial;

        [Tooltip("When to inject the pass. AfterRenderingPostProcessing is usually fine.")]
        public RenderPassEvent evt = RenderPassEvent.AfterRenderingPostProcessing;

        [Tooltip("Optional material pass index (keep 0 unless you know you need another).")]
        public int materialPass = 0;
    }

    class MoodOverlayPass : ScriptableRenderPass
    {
        private readonly Settings _settings;
        private RenderTargetIdentifier _source;
        private RTHandle _tempColor;
        private readonly ProfilingSampler _profiler = new ProfilingSampler("MoodOverlay Blit");
        private static readonly int _BlitTexId = Shader.PropertyToID("_BlitTexture");

        public MoodOverlayPass(Settings settings)
        {
            _settings = settings;
            renderPassEvent = settings.evt;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (_settings.overlayMaterial == null) return;

            // Get the current camera color target
#if UNITY_2023_1_OR_NEWER
            var colorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
#else
            var colorTarget = renderingData.cameraData.renderer.cameraColorTarget;
#endif

            _source = colorTarget;

            // Allocate a temporary RT for blitting
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref _tempColor, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_MoodOverlay_Temp");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_settings.overlayMaterial == null)
                return;

            var cmd = CommandBufferPool.Get("MoodOverlay Execute");
            using (new ProfilingScope(cmd, _profiler))
            {
                cmd.SetGlobalTexture(_BlitTexId, _source);

#if UNITY_2023_1_OR_NEWER
                Blitter.BlitCameraTexture(cmd, _source, _tempColor, _settings.overlayMaterial, _settings.materialPass);
                Blitter.BlitCameraTexture(cmd, _tempColor, _source);
#else
                // Older URP: use cmd.Blit
                cmd.Blit(_source, _tempColor, _settings.overlayMaterial, _settings.materialPass);
                cmd.Blit(_tempColor, _source);
#endif
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // nothing needed
        }

        public void Dispose()
        {
            _tempColor?.Release();
            _tempColor = null;
        }
    }

    public Settings settings = new Settings();
    private MoodOverlayPass _pass;

    public override void Create()
    {
        _pass = new MoodOverlayPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.overlayMaterial == null)
            return;

        _pass.renderPassEvent = settings.evt;
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _pass?.Dispose();
        _pass = null;
    }
}
