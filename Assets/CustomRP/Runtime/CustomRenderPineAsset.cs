using UnityEngine;
using UnityEngine.Rendering;

//右键菜单
[CreateAssetMenu(menuName = "Rendering/CreateCustomRenderPipeline")]
public class CustomRenderPineAsset : RenderPipelineAsset
{
    [SerializeField]
    bool useDynamicBatching = true, useGPUInstanceing = true, useSRPBathcer = true;

    //阴影设置
    [SerializeField]
    ShadowSettings shadows = default;
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(useDynamicBatching, useGPUInstanceing, useSRPBathcer, shadows);
    }
}
