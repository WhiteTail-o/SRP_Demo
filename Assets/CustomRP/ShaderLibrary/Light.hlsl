//光源属性
#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

//注意与CS中匹配
#define MAX_DIRECTIONAL_LIGHT_COUNT 4

CBUFFER_START(_CustomLight)
    int _DirectionalLightCount;
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];

    //阴影数据,x: shadow strength, y: Light Count(Offset)
    float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

struct Light {
    // Light Color
    float3 color;

    // Light Direction (to the outside)
    float3 directionWS;

    //灯光衰减
    float attenuation;
};

int GetDirectionalLightCount() {
    return _DirectionalLightCount;
}

//获得阴影数据
DirectionalShadowData GetDirectionalShadowData(int lightIndex, ShadowData shadowData) {
    DirectionalShadowData data;
    data.strength = _DirectionalLightShadowData[lightIndex].x * shadowData.strength;
    data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
    data.normalBias = _DirectionalLightShadowData[lightIndex].z;
    return data;
}

// Get a direction light, defalut color is white, light direction is (0, 1, 0)
Light GetDirectionalLight(int index, Surface surfaceWS, ShadowData shadowData) {
    Light light;
    light.color = _DirectionalLightColors[index].rgb;
    light.directionWS = _DirectionalLightDirections[index].xyz;

    //阴影数据
    DirectionalShadowData dirShadowData = GetDirectionalShadowData(index, shadowData);
    light.attenuation = GetDirectionalShadowAttenuation(dirShadowData, shadowData, surfaceWS);
    // light.attenuation = shadowData.cascadeIndex / 4.0;
    return light;
}

#endif