using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

// 用于将场景中的光源信息通过CPU传递到GPU
public class Lighting
{
    private const string bufferName = "Lighting";

    //最大方向光源数量
    private const int maxDirLightCount = 4;

    //CBUFFER中对应数据名称的Id
    // private static int dirLightColorId = Shader.PropertyToID("_DirectionalLightColor");
    // private static int dirLightDirectionId = Shader.PropertyToID("_DirectionalLightDirection");

    private static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    private static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
    private static int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");

    //x:shadowStrength, y:atlas offset, z:normalBias
    private static int dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");
    static Vector4[] dirLightShadowData = new Vector4[maxDirLightCount];

    //存储可见光颜色和方向
    private static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
    private static Vector4[] dirLightDirections = new Vector4[maxDirLightCount];

    private CommandBuffer buffer = new CommandBuffer() {
        name = bufferName
    };

    //储存影响可见范围的灯光信息
    private CullingResults cullingResults;

    private Shadows shadows = new Shadows();

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings) {
        this.cullingResults = cullingResults;
        buffer.BeginSample(bufferName);

        //设置阴影
        shadows.Setup(context, cullingResults, shadowSettings);
        // SetupDirectionalLight();
        SetupLights();

        shadows.Render();
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    //配置Vector4数组中的单个属性
    //ref传递Visiblelight，防止copy（该结构体较大）
    void SetupDirectionalLight(int index, ref VisibleLight visibleLight) {
        // //通过RenderSettings.sun获取场景中默认的最主要的一个方向光，我们可以在Window/Rendering/Lighting Settings中显式配置它
        // Light light = RenderSettings.sun;

        // //使用CommandBuffer.SetGlobalVector将光源信息传递给GPU
        // buffer.SetGlobalVector(dirLightColorId, light.color.linear * light.intensity);
        // buffer.SetGlobalVector(dirLightDirectionId, -light.transform.forward);
        //-----------------------------------//
        dirLightColors[index] = visibleLight.finalColor;
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);

        //存储可见光的阴影数据
        dirLightShadowData[index] = shadows.ReserveDirectionalShadows(visibleLight.light, index);
    }

    void SetupLights() {
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        int dirLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++) {
            VisibleLight visibleLight = visibleLights[i];

            //配置方向光源
            if (visibleLight.lightType == LightType.Directional) {
                SetupDirectionalLight(dirLightCount++, ref visibleLight);

                //方向光源数量小于maxDirLightCount
                if (dirLightCount >= maxDirLightCount) {
                    break;
                }
            }
        }

        buffer.SetGlobalInt(dirLightCountId, visibleLights.Length);
        buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
        buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
        buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);

    }

    public void Cleanup() {
        shadows.Cleanup();
    }
}
