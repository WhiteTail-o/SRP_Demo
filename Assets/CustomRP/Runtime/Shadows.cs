using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    private const string bufferName = "Shadows";

    private CommandBuffer buffer = new CommandBuffer() {
        name = bufferName
    };

    private ScriptableRenderContext context;

    private CullingResults cullingResults;

    private ShadowSettings shadowSettings;

    //可投影方向光的数量
    const int maxShadowedDirectionalLightCount = 4;
    //最大级联数量
    const int maxCascades = 4;

    struct ShadowedDirectionalLight {
        public int visibleLightIndex;

        //斜度比例偏差值
        public float slopeScaleBias;
        public float nearPlaneOffset;
    }

    //存储可投影的可见光源的索引
    ShadowedDirectionalLight[] shadowedDirectionalLights = new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

    //已配置的光源数量
    private int ShadowedDirectionalLightCount;

    //阴影VP矩阵
    static Matrix4x4[] dirShadowMatrics = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades];

    static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];

    //级联数据
    static Vector4[] cascadeData = new Vector4[maxCascades];

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings) {
        this.context = context;
        this.cullingResults = cullingResults;
        this.shadowSettings = shadowSettings;
        ShadowedDirectionalLightCount = 0;
    }

    void ExecuteBuffer() {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    //存储可见光的阴影数据，目的是在阴影图集中为该光源阴影贴图保留空间，并存储渲染他们所需的信息
    // 判断该光源是否开启阴影投影，且强度是否大于0，
    // 通过cullingResults.GetShadowCasterBounds判断在最大投影范围内是否存在受该光源影响的物体
    public Vector3 ReserveDirectionalShadows(Light light, int visibleLightIndex) {
        if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount 
        && light.shadows != LightShadows.None 
        && light.shadowStrength > 0f
        && cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b)) {
            shadowedDirectionalLights[ShadowedDirectionalLightCount] = new ShadowedDirectionalLight {
                visibleLightIndex = visibleLightIndex,
                slopeScaleBias = light.shadowBias,
                nearPlaneOffset = light.shadowNearPlane
            };

            //返回阴影强度和Atlas offset
            return new Vector3(light.shadowStrength, shadowSettings.directioinal.cascadeCount * ShadowedDirectionalLightCount++, light.shadowNormalBias);
        }
        return Vector3.zero;
    }

    public void Render() {
        if (ShadowedDirectionalLightCount > 0) {
            RenderDirectionalShadows();
        }
        else {
            //如果因为某种原因不需要渲染阴影，我们也需要生成一张1x1大小的ShadowAtlas
            //因为WebGL 2.0下如果某个材质包含ShadowMap但在加载时丢失了ShadowMap会报错
            buffer.GetTemporaryRT(ShaderID._DirectionalShadowAtlas, 1, 1, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        }
    }

    //渲染定向光源
    void RenderDirectionalShadows() {
        int atlasSize = (int)shadowSettings.directioinal.atlasSize;
        buffer.GetTemporaryRT(ShaderID._DirectionalShadowAtlas, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);

        //RenderBufferLoadAction.DontCare意味着在将其设置为RenderTarget之后，我们不关心它的初始状态，不对其进行任何预处理
        //RenderBufferStoreAction.Store意味着完成这张RT上的所有渲染指令之后（要切换为下一个RenderTarget时），我们会将其存储到显存中为后续采样使用
        buffer.SetRenderTarget(ShaderID._DirectionalShadowAtlas, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.ClearRenderTarget(true, false, Color.clear);

        buffer.BeginSample(bufferName);
        ExecuteBuffer();

        //要分割的Tile大小和数量
        int tiles = ShadowedDirectionalLightCount * shadowSettings.directioinal.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;

        //遍历所有方向光渲染阴影
        for (int i = 0; i < ShadowedDirectionalLightCount; i++) {
            RenderDirectionalShadows(i, split, tileSize);
        }
        
        //将级联阴影数量和包围球数据发送到GPU
        buffer.SetGlobalInt(ShaderID._CascadeCount, shadowSettings.directioinal.cascadeCount);
        buffer.SetGlobalVectorArray(ShaderID._CascadeCullingSpheres, cascadeCullingSpheres);
        buffer.SetGlobalMatrixArray(ShaderID._DirectionalShadowMatrices, dirShadowMatrics);
        buffer.SetGlobalVectorArray(ShaderID._CascadeData, cascadeData);

        float f = 1f - shadowSettings.directioinal.cascadeFade;
        buffer.SetGlobalVector(ShaderID._ShadowDistanceFade, new Vector4(1f / shadowSettings.maxDistance, 1f / shadowSettings.distanceFade, 1f / (1f - f * f)));

        SetKeywords(ShaderKeywords._CASCADE_BLEND, (int)shadowSettings.directioinal.cascadeBlend);
        SetKeywords(ShaderKeywords._DIRECTIONAL_PCF, (int)shadowSettings.directioinal.filter);
        buffer.SetGlobalVector(ShaderID._ShadowAtlasSize, new Vector4(atlasSize, 1f / atlasSize));

        buffer.EndSample(bufferName);
        ExecuteBuffer();

    }

    /// <summary>
    /// 渲染单个光源的Shadow Map到Shadow Atlas上
    /// </summary>
    /// <param name="index">光源的索引</param>
    /// <param name="split">Tile一个方向上的总数</param>
    /// <param name="tileSize">在Atlas上分配的Tile大小</param>
    void RenderDirectionalShadows(int index, int split, int tileSize) {
        ShadowedDirectionalLight light = shadowedDirectionalLights[index];
        //根据cullingResults和当前光源的索引来构造一个ShadowDrawingSettings
        var shadowDrawingSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
        

        //级联数据
        int cascadeCount = shadowSettings.directioinal.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = shadowSettings.directioinal.CacadeRatios;
        float cullingFactor = Mathf.Max(0f, 0.8f - shadowSettings.directioinal.cascadeFade);

        for (int i = 0; i < cascadeCount; i++) {
            //计算方向光源VP矩阵，即阴影分割数据
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, 
                i, cascadeCount, ratios,
                tileSize, light.nearPlaneOffset,
                out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData);
            
            //所有光源都使用相同的级联，获取第一个光源的包围球数据
            if (index == 0) {
                // cascadeCullingSpheres[i] = shadowSplitData.cullingSphere;
                SetCascadeData(i, shadowSplitData.cullingSphere, tileSize);
            }

            shadowSplitData.shadowCascadeBlendCullingFactor = cullingFactor;
            shadowDrawingSettings.splitData = shadowSplitData;
            int tileIndex = tileOffset + i;
            //设置渲染Viewport
            Vector2 offset = SetTileViewport(tileIndex, split, tileSize);

            // dirShadowMatrics[index] = projMatrix * viewMatrix;
            dirShadowMatrics[tileIndex] = ConvertToAtlasMatrix(projMatrix * viewMatrix, offset, split);
            buffer.SetViewProjectionMatrices(viewMatrix, projMatrix);

            //设置斜率比例偏差值
            buffer.SetGlobalDepthBias(0, light.slopeScaleBias);
            ExecuteBuffer();
            //渲染Shader中带有ShadowCaster Pass的物体
            context.DrawShadows(ref shadowDrawingSettings);
            buffer.SetGlobalDepthBias(0f, 0f);
        }
    }

    /// <summary>
    /// 调整Viewport渲染单个Tile
    /// </summary>
    /// <param name="index">光源的索引</param>
    /// <param name="split">Tile一个方向上的总数</param>
    /// <param name="tileSize">在Atlas上分配的Tile大小</param>
    /// <returns>偏移值</returns>
    Vector2 SetTileViewport(int index, int split, int tileSize) {
        Vector2 offset = new Vector2(index % split, index / split);
        buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        return offset;
    }

    /// <summary>
    /// 计算世界空间到Shadow Atlas对应位置的VP矩阵
    /// </summary>
    /// <param name="offset">偏移值</param>
    /// <param name="split">Tile一个方向上的总数</param>
    /// <returns>世界空间到Shadow Atlas对应位置的VP矩阵</returns>
    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split) {
        //使用了Reverse Z
        if (SystemInfo.usesReversedZBuffer) {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }

        //光源裁剪空间坐标范围为[-1,1]，而纹理坐标和深度都是[0,1]，因此，我们将裁剪空间坐标转化到[0,1]内
        //然后将[0,1]下的x,y偏移到光源对应的Tile上
        float scale = 1f / split;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);

        return m;
    }

    //设置级联数据
    void SetCascadeData(int index, Vector4 cullingSphere, float tileSize) {
        float texelSize = 2.0f * cullingSphere.w / tileSize;
        float filterSize = texelSize * ((float)shadowSettings.directioinal.filter + 1f);
        
        cullingSphere.w -= filterSize;
        cascadeCullingSpheres[index] = cullingSphere;

        cascadeData[index] = new Vector4(1f/(cullingSphere.w * cullingSphere.w), filterSize * 1.4142136f);
    }

    //设置开启那种PCF
    void SetKeywords(string[] keywords, int enabledIndex) {
        // int enabledIndex = (int)shadowSettings.directioinal.filter;
        for (int i = 0; i < keywords.Length; i++) {
            if (i == enabledIndex) {
                buffer.EnableShaderKeyword(keywords[i]);
            }
            else{
                buffer.DisableShaderKeyword(keywords[i]);
            }
        }
    }
    
    //释放临时RT
    public void Cleanup() {
        buffer.ReleaseTemporaryRT(ShaderID._DirectionalShadowAtlas);
        ExecuteBuffer();
    }
}
