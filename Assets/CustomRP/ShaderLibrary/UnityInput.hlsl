#ifndef CUSTOM_UNITY_INPUT_INCLUDED
#define CUSTOM_UNITY_INPUT_INCLUDED

CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    //在定义（UnityPerDraw）CBuffer时，因为Unity对一组相关数据都归到一个Feature中，即使我们没用到unity_LODFade，我们也需要放到这个CBuffer中来构造一个完整的Feature
    //如果不加这个unity_LODFade，不能支持SRP Batcher
    float4 unity_LODFade;
    real4 unity_WorldTransformParams;

    //添加lightmap转换属性和动态光照贴图，防止兼容问题导致SRP中断
    float4 unity_LightmapST;
    float4 unity_DynamicLightmapST;

    //SH
    float4 unity_SHAr;
    float4 unity_SHAg;
    float4 unity_SHAb;
    float4 unity_SHBr;
    float4 unity_SHBg;
    float4 unity_SHBb;
    float4 unity_SHC;

    //LPPV相关
    float4 unity_ProbeVolumeParams;
    float4x4 unity_ProbeVolumeWorldToObject;
    float4 unity_ProbeVolumeSizeInv;
    float4 unity_ProbeVolumeMin;
CBUFFER_END

float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;

//上一帧矩阵数据
float4x4 unity_MatrixPreviousM;
float4x4 unity_MatrixPreviousMI;

//相机位置
float3 _WorldSpaceCameraPos;

#endif