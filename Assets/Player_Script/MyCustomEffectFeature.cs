using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MyCustomEffectFeature : ScriptableRendererFeature
{
    class Pass : ScriptableRenderPass
    {
        static readonly string kTag = "MyCustomEffect";
        readonly Material mat;
        RTHandle tempRT;
        RTHandle source;

        public Pass(Material mat, RenderPassEvent evt)
        {
            this.mat = mat;
            renderPassEvent = evt;
        }

        public void Setup(RTHandle src) => source = src;

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref tempRT, desc, name: "_MyCustomEffectTemp");
        }

        public override void Execute(ScriptableRenderContext ctx, ref RenderingData renderingData)
        {
            var stack = VolumeManager.instance.stack;
            var settings = stack.GetComponent<MyCustomEffect>();
            if (settings == null || !settings.IsActive()) return;

            var cmd = CommandBufferPool.Get(kTag);
            cmd.SetGlobalFloat("_Intensity", settings.intensity.value);

            Blitter.BlitCameraTexture(cmd, source, tempRT, mat, 0);
            Blitter.BlitCameraTexture(cmd, tempRT, source);

            ctx.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose() => tempRT?.Release();
    }

    [SerializeField] Shader shader;
    [SerializeField] RenderPassEvent evt = RenderPassEvent.AfterRenderingPostProcessing;

    Material mat;
    Pass pass;

    public override void Create()
    {
        if (shader != null)
            mat = CoreUtils.CreateEngineMaterial(shader);
        pass = new Pass(mat, evt);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        pass.Setup(renderer.cameraColorTargetHandle);
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(mat);
        pass?.Dispose();
    }
}
