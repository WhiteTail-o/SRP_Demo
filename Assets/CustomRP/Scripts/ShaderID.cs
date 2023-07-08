using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShaderID   // 用于存放较为通用的ShaderID
{
    // Common
    public static int _Color = Shader.PropertyToID("_Color");
    public static int _MainTex = Shader.PropertyToID("_MainTex");
    public static int _Cutoff = Shader.PropertyToID("_Cutoff"); //AlphaCutOff Threshold
    public static int _Metallic = Shader.PropertyToID("_Metallic");
    public static int _Roughness = Shader.PropertyToID("_Roughness");
    
    //阴影图集
    public static int _DirectionalShadowAtlas = Shader.PropertyToID("_DirectionalShadowAtlas");
    public static int _ShadowAtlasSize = Shader.PropertyToID("_ShadowAtlasSize");

    //各阴影的VP矩阵
    public static int _DirectionalShadowMatrices = Shader.PropertyToID("_DirectionalShadowMatrices");

    //级联阴影相关
    public static int _CascadeCount = Shader.PropertyToID("_CascadeCount");
    public static int _CascadeCullingSpheres = Shader.PropertyToID("_CascadeCullingSpheres");

    //级联数据，x：1/cullingSphereRadius^2; y: texelSize * 2^(1/2)
    public static int _CascadeData = Shader.PropertyToID("_CascadeData");

     //阴影衰减，
     //x: 1/maxShaodwDistance，y: 1/distanceFade, shadow = (1 - depth * x) * y;
     //z: 1/(1 - (1-f)^2)
    public static int _ShadowDistanceFade = Shader.PropertyToID("_ShadowDistanceFade");

    public static int _EmissionColor = Shader.PropertyToID("_EmissionColor");
}