//光照计算相关
#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

#include "./GI.hlsl"

//计算Shading Point接收的光能量（radiance）
float3 IncomingLight(Surface surface, Light light) {
    return saturate(dot(surface.normal, light.directionWS)) * light.color * light.attenuation;
}

//得到单个光源的光照结果
float3 GetLighting(Surface surface, BRDF brdf, Light light) {
    return IncomingLight(surface, light) * DirectBRDF(surface, brdf, light);
}

//得到最终光照结果
float3 GetLighting(Surface surfaceWS, BRDF brdf, GI gi) {
    ShadowData shadowData = GetShadowData(surfaceWS);

    // float3 finalColor = 0.0;
    float3 finalColor = gi.diffuse * brdf.diffuse;
    for (int i = 0; i < GetDirectionalLightCount(); i++) {
        Light light = GetDirectionalLight(i, surfaceWS, shadowData);
        finalColor += GetLighting(surfaceWS, brdf, light);
    }
    return finalColor;
}

#endif