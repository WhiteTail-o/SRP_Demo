//阴影采样
#ifndef CUSTOM_SHADOWS_INCLUDED
#define CUSTOM_SHADOWS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"
#if defined(_DIRECTIONAL_PCF3)
    //需要四个过滤样本，因为每个样本使用双线性2x2的滤波模式
    #define DIRECTIONAL_FILTER_SAMPLES 4
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_DIRECTIONAL_PCF5)
    #define DIRECTIONAL_FILTER_SAMPLES 9
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_DIRECTIONAL_PCF7)
    #define DIRECTIONAL_FILTER_SAMPLES 16
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4

#include "./Surface.hlsl"

//阴影图集
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);

//sampler_linear_clamp_compare这个取名十分有讲究，Unity会将这个名字翻译成使用Linear过滤、Clamp包裹的用于深度比较的采样器
//https://docs.unity3d.com/cn/current/Manual/SL-SamplerStates.html
#define SHADOW_SAMPLER sampler_linear_clamp_compare

/*
SAMPLER_CMP是一个特殊的采样器，
我们可以通过与其匹配的SampleCmp函数来采样深度图。
该函数将采样一块纹素区域（不是仅仅一个纹素），
对于每个纹素，将其采样出来的深度值与给定比较值进行比较，返回0或者1。
最后将这些纹素的每个0或1结果通过纹理过滤模式混合在一起（不是平均值这么简单的混合），最后将一个[0,1]的float类型混合结果返回给着色器。
这个深度比较结果，相比只采样一个纹素效果更好。
*/
SAMPLER_CMP(SHADOW_SAMPLER);

#define MAX_CASCADE_COUNT 4
CBUFFER_START(_CustomShadows)
    int _CascadeCount;
    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
    float4 _CascadeData[MAX_CASCADE_COUNT];
    //阴影矩阵:世界空间到对应Shadow Atlas
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
    float4 _ShadowDistanceFade;
    float4 _ShadowAtlasSize;
CBUFFER_END

struct DirectionalShadowData {
    float strength;
    int tileIndex;
    float normalBias;
};

struct ShadowData {
    int cascadeIndex;
    float strength;
    float cascadeBlend;
};

//公式计算阴影过渡强度（maxDistance左右）
float FadedShadowStrength(float distance, float scale, float fade) {
    return saturate( (1.0 - distance * scale)*fade ); 
}

ShadowData GetShadowData(Surface surfaceWS) {
    ShadowData data;
    data.cascadeBlend = 1.0;
    data.strength = FadedShadowStrength(surfaceWS.depthVS, _ShadowDistanceFade.x, _ShadowDistanceFade.y);
    int i;
    for (i = 0; i < _CascadeCount; i++) {
        float4 sphere = _CascadeCullingSpheres[i];
        float distanceSqr = DistanceSquared(surfaceWS.position, sphere.xyz);
        float w2 = sphere.w * sphere.w;
        if (distanceSqr < w2) {
            
            float fade = FadedShadowStrength(distanceSqr, _CascadeData[i].x, _ShadowDistanceFade.z);
            //对于最后一个级联应用过渡
            if (i == _CascadeCount - 1) {
                data.strength *= fade;
            }
            else {
                data.cascadeBlend = fade;
            }
            break;
        }
    }

    if (i == _CascadeCount) { //超出最后一个级联范围，标识符设置为0，不进行阴影采样
        data.strength = 0.0;
    }

#if defined(_CASCADE_BLEND_DITHER)
    //混合值小于抖动值时，跳到下一个级联
    else if (data.cascadeBlend < surfaceWS.dither) {
        i += 1;
    }
#endif

#if !defined(_CASCADE_BLEND_SOFT)
    data.cascadeBlend = 1.0;
#endif

    data.cascadeIndex = i;
    return data;
}

//positionSTS(Shadow Tile Space, 即在阴影Atlas下的坐标)
float SampleDirectionalShadowAtlas(float3 positionSTS) {
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

//定义了DIRECTIONAL_FILTER_SETUP时需要多次采样
float FilterDirectionalShadow (float3 positionSTS) {
    #if defined(DIRECTIONAL_FILTER_SETUP) 
        float weights[DIRECTIONAL_FILTER_SAMPLES];
        float2 positions[DIRECTIONAL_FILTER_SAMPLES];
        float4 size = _ShadowAtlasSize.yyxx;
        DIRECTIONAL_FILTER_SETUP(size, positionSTS.xy, weights, positions);
        float shadow = 0;
        for (int i = 0; i < DIRECTIONAL_FILTER_SAMPLES; i++) {
            shadow += weights[i] * SampleDirectionalShadowAtlas(float3(positions[i].xy, positionSTS.z));
        };
        return shadow;
    #else
        return SampleDirectionalShadowAtlas(positionSTS);
    #endif
}

//计算阴影衰减
float GetDirectionalShadowAttenuation(DirectionalShadowData data, ShadowData shadowData, Surface surfaceWS) {
    if (data.strength <= 0.0) {
        return 1.0;
    }

    //计算normal bias
    float3 normalBias = surfaceWS.normal * (data.normalBias * _CascadeData[shadowData.cascadeIndex].y);

    float3 positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex], float4(surfaceWS.position + normalBias, 1.0)).xyz;
    float shadow = FilterDirectionalShadow(positionSTS);

    //级联混合小于1需要在该level和下一level之间进行混合
    if (shadowData.cascadeBlend < 1.0) {
        normalBias = surfaceWS.normal * (data.normalBias * _CascadeData[shadowData.cascadeIndex + 1].y);
        positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex + 1], float4(surfaceWS.position + normalBias, 1.0)).xyz;
        shadow = lerp(FilterDirectionalShadow(positionSTS), shadow, shadowData.cascadeBlend);
        // shadow = 0;
    }
    
#if !defined(_RECEIVE_SHADOWS)
    return 1.0;
#endif

    return lerp(1.0, shadow, data.strength);
}

#endif