//定义与光照相关的物体表面属性
#ifndef CUSTOM_SURFACE_INCLUDED
#define CUSTOM_SURFACE_INCLUDED

struct Surface {
    float3 viewDirectionWS;
    //此处不标明空间，为了规范默认使用世界空间，声明结构体需要标明空间
    float3 position;
    float3 normal;
    float metallic;
    float roughness;
    float3 color;
    float alpha; 
    float depthVS;
    float dither;
};

#endif