#ifndef CUSTOM_LIT_PASS_INCLUDED
#define CUSTOM_LIT_PASS_INCLUDED

// #include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadows.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"
#include "../ShaderLibrary/GI.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"

// CBUFFER_START(UnityPerMaterial)
//  float4 _Color;
// CBUFFER_END


struct Attributes {
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 baseUV : TEXCOORD0;
    GI_ATTRIBUTE_DATA
    //通过该宏定义每个实例的ID，告诉GPU绘制的是那个Object
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings {
    float4 positionCS : SV_POSITION;
    float3 positionWS : VAR_POSITION;
    float3 normalWS : VAR_NORMAL;
    float2 baseUV : VAR_BASE_UV;
    GI_VARYINGS_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings LitPassVertex( Attributes input )
{
    Varyings output;
    //提取input中Object的索引ID，并将其存储到其他实例宏所依赖的全局静态变量中
    UNITY_SETUP_INSTANCE_ID(input);
    //将对象索引ID传递给output
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    TRANSFER_GI_DATA(input, output);

    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(output.positionWS);

    output.normalWS = TransformObjectToWorldNormal(input.normalOS);

    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST);
    output.baseUV = TransformUV(input.baseUV);
    return output;
}

float4 LitPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);

    float4 baseMap = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.baseUV);
    //通过UNITY_ACCESS_INSTANCED_PROP()获得每实例数据
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Color);
    baseColor *= baseMap;

    #if defined(_SHADOWS_CLIP)
        clip(baseColor.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
    #endif

    Surface surfaceWS;
    surfaceWS.position = input.positionWS;
    surfaceWS.normal = normalize(input.normalWS);
    surfaceWS.viewDirectionWS = normalize(_WorldSpaceCameraPos - input.positionWS);
    surfaceWS.depthVS = -TransformWorldToView(input.positionWS).z;
    surfaceWS.color = baseColor.rgb * baseMap.rgb;
    surfaceWS.alpha = baseColor.a;
    surfaceWS.metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
    surfaceWS.roughness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Roughness);
    //计算抖动值
    surfaceWS.dither = InterleavedGradientNoise(input.positionCS.xy, 0);

#if defined(_PREMULTIPLY_ALPHA)
    BRDF brdf = GetBRDF(surfaceWS, true);
#else
    BRDF brdf = GetBRDF(surfaceWS);
#endif
    GI gi = GetGI(GI_FRAGMENT_DATA(input), surfaceWS);
    float3 LitColor = GetLighting(surfaceWS, brdf, gi);
    LitColor += GetEmission(input.baseUV);
    return float4(LitColor, surfaceWS.alpha);
}

#endif