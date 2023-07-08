#ifndef CUSTOM_UNLIT_INPUT_INCLUDED
#define CUSTOM_UNLIT_INPUT_INCLUDED

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

TEXTURE2D(_EmissionMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
    UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

float2 TransformUV(float2 baseUV) {
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST);
    return baseUV * baseST.xy + baseST.zw;
}

float4 GetBase(float2 baseUV) {
    float4 map = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, baseUV);
    float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Color);
    return map * color;
}

float3 GetEmission(float2 baseUV) {
    return GetBase(baseUV).rgb;
}

#endif