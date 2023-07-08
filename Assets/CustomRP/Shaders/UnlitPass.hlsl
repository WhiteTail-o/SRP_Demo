#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"

// CBUFFER_START(UnityPerMaterial)
//  float4 _Color;
// CBUFFER_END
// TEXTURE2D(_MainTex);
// SAMPLER(sampler_MainTex);

// UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
//     UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
//     UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
//     UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
// UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)


struct Attributes {
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    //通过该宏定义每个实例的ID，告诉GPU绘制的是那个Object
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings {
    float4 positioncS : SV_POSITION;
    float2 baseUV : VAR_BASE_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings UnlitPassVertex( Attributes input )
{
    Varyings output;
    //提取input中Object的索引ID，并将其存储到其他实例宏所依赖的全局静态变量中
    UNITY_SETUP_INSTANCE_ID(input);
    //将对象索引ID传递给output
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positioncS = TransformWorldToHClip(positionWS);

    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST);
    output.baseUV = input.baseUV * baseST.xy + baseST.zw;
    return output;
}

float4 UnlitPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);

    float4 baseMap = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.baseUV);
    //通过UNITY_ACCESS_INSTANCED_PROP()获得每实例数据
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Color);
    baseColor *= baseMap;

    #if defined(_SHADOWS_CLIP)
        clip(baseColor.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
    #endif

    return baseColor;
}

#endif