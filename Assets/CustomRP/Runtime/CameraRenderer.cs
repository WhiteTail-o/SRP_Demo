using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    ScriptableRenderContext context;
    Camera camera;

    const string bufferName = "Render Camera";
    CommandBuffer buffer = new CommandBuffer{
        name = bufferName
    };

    // 剔除结果
    CullingResults cullingResults;

    Lighting lighting = new Lighting();

    //LightMode Tags
    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");

    public void Render( ScriptableRenderContext context, Camera camera,
        bool useDynamicBatching, bool useGPUInstanceing,
        ShadowSettings shadowSettings ) {
        this.context = context;
        this.camera = camera;
        //设置buffer名字
        PrepareBuffer();

        PrepareForSceneWindow();
        if ( !Cull(shadowSettings.maxDistance) ) {
            return;
        }

        buffer.BeginSample(SampleName);
        ExecuteBuffer();
        //设置灯光、阴影（处于场景正式渲染前）
        lighting.Setup(context, cullingResults, shadowSettings);
        buffer.EndSample(SampleName);

        Setup();

        DrawVisibleGeometry(useDynamicBatching, useGPUInstanceing);
        DrawUnsupportedShaders();
        DrawGizmos();

        //清理如阴影Atlas等
        lighting.Cleanup();
        Submit();
    }

    void Setup() {
        //设置相机参数、变换矩阵
        context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;

        buffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth, flags == CameraClearFlags.Color, 
            flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear
            );
            
        //开始采样，用于Profiler和Debugger中显示
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
        
    }

    /// <summary>
    /// 绘制可见物
    /// </summary>
    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstanceing) {
        //Draw Opaque
        var sortingSettings = new SortingSettings(camera) {
            criteria = SortingCriteria.CommonOpaque
        };
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings) {
            enableInstancing = useGPUInstanceing,
            enableDynamicBatching = useDynamicBatching,
            perObjectData = PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume
        };

        //增加对Lit.shader的LightMode = CustomLit的支持，index表示DrawRenderer中的优先级
        drawingSettings.SetShaderPassName(1, litShaderTagId);
        
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
        );

        //Skybox
        context.DrawSkybox(camera);

        //Draw Transparent
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;

        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
        );
    }

    /// <summary>
    /// 提交缓冲区命令
    /// </summary>
    void Submit(){
        buffer.EndSample(SampleName);
        ExecuteBuffer();
        context.Submit();
    }

    /// <summary>
    /// 执行CB后，清空buffer
    /// </summary>
    void ExecuteBuffer() {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    bool Cull(float maxShadowDistance) {
        ScriptableCullingParameters p;
        if (camera.TryGetCullingParameters(out p)) {
            //最大阴影距离和远裁切面的最小值作为阴影距离
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            cullingResults = context.Cull(ref p);
            return true;
        }
        return false;
    }
}
