using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline
{
    CameraRenderer renderer = new CameraRenderer();

    bool useDynamicBatching, useGPUInstanceing;

    ShadowSettings shadowSettings;
    public CustomRenderPipeline(bool useDynamicBatching, bool useGPUInstanceing, bool useSRPBathcer, ShadowSettings shadowSettings) {
        this.shadowSettings = shadowSettings;

        this.useDynamicBatching= useDynamicBatching;
        this.useGPUInstanceing = useGPUInstanceing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBathcer;

        //灯光使用线性强度
        GraphicsSettings.lightsUseLinearIntensity = true;
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)   //SRP渲染入口
    {
        foreach (Camera camera in cameras) {
            renderer.Render(context, camera, useDynamicBatching, useGPUInstanceing, shadowSettings);
        }
    }
}
