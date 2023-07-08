#ifndef CUSTOM_BRDF_INCLUDED
#define CUSTOM_BRDF_INCLUDED

#define DEFAULT_DIELECTRIC_F0 0.04
#define MIN_ROUGHNESS 0.01

struct BRDF {
    float3 diffuse;
    float3 specular;
    float roughness;
};

float OneMinusReflectivity(float metallic) {
    float range = 1.0 - DEFAULT_DIELECTRIC_F0;
    return range - metallic * range;
}

//计算Diffuse和Specular的BRDF及其比例
BRDF GetBRDF(Surface surface, bool applyAlphaToDiffuse = false) {
    BRDF brdf;

    brdf.diffuse = surface.color * OneMinusReflectivity(surface.metallic);

    if (applyAlphaToDiffuse) {
        //Premultiplied Alpha
        brdf.diffuse *= surface.alpha;
    }
    
    brdf.specular = lerp(DEFAULT_DIELECTRIC_F0, surface.color, surface.metallic);
    brdf.roughness = lerp(MIN_ROUGHNESS, 1.0, PerceptualRoughnessToRoughness(surface.roughness));
    return brdf;
}

//非PBR，Cook-Torrance的变体
float SpecularStrength (Surface surface, BRDF brdf, Light light) {
    float3 h = SafeNormalize(light.directionWS + surface.viewDirectionWS);
    float NdotH_2 = saturate(dot(surface.normal, h));
    NdotH_2 *= NdotH_2;

    float LdotH_2 = saturate( dot(light.directionWS, h) );
    LdotH_2 *= LdotH_2;

    float r2 = brdf.roughness * brdf.roughness;
    float d2 = NdotH_2 * (r2 - 1.0) + 1.0001;
    d2 *= d2;

    //归一化项
    float normalization = brdf.roughness * 4.0 + 2.0;
    return r2/(d2 * max(0.1, LdotH_2) * normalization);
}

//直接光照BRDF
float3 DirectBRDF(Surface surface, BRDF brdf, Light light) {
    return SpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

#endif