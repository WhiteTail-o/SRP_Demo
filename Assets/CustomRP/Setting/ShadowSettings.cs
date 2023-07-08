using UnityEngine;

[System.Serializable]
public class ShadowSettings
{
    //渲染阴影贴图的距离
    [Min(0f)] public float maxDistance = 100f;
    [Range(0.001f, 1f)]
    public float distanceFade = 0.1f;

    //阴影贴图尺寸
    public enum TextureSize {
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096,
        _8192 = 8192
    }

    public enum FilterMode {
        PCF2x2, PCF3x3, PCF5x5, PCF7x7
    }

    //定义方向光源阴影贴图配置，使用单个纹理包含多个阴影贴图
    [System.Serializable]
    public struct Directioinal {
        public TextureSize atlasSize;

        public FilterMode filter;
        //级联数量
        [Range(1, 4)]
        public int cascadeCount;

        [Range(0f, 1f)]
        public float cascadeRatiol1, cascadeRatiol2, cascadeRatiol3;

        public Vector3 CacadeRatios => new Vector3(cascadeRatiol1, cascadeRatiol2, cascadeRatiol3);

        [Range(0.001f, 1f)]
        public float cascadeFade;

        public enum CascadeBlendMode {
            Hard, Soft, Dither
        }

        public CascadeBlendMode cascadeBlend;
    }

    //默认尺寸1024
    public Directioinal directioinal = new Directioinal {
        atlasSize = TextureSize._1024,
        filter = FilterMode.PCF5x5,
        cascadeCount = 4,
        cascadeRatiol1 = 0.1f,
        cascadeRatiol2 = 0.25f,
        cascadeRatiol3 = 0.5f,
        cascadeFade = 0.1f,
        cascadeBlend = Directioinal.CascadeBlendMode.Hard
    };

}
