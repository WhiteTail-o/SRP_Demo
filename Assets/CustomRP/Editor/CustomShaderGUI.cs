using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomShaderGUI : ShaderGUI
{
    //用于显示、编辑材质属性
    MaterialEditor editor;

    //编制材质的引用对象
    Object[] materials;

    //材质编辑属性
    MaterialProperty[] properties;

    //是否折叠
    private bool showPresets;

    enum ShadowMode {
        On, Clip, Dither, Off
    }

    void SetShadowCasterPass() {
        MaterialProperty shadows = FindProperty("_Shadows", properties, false);
        if (shadows == null || shadows.hasMixedValue) {
            return;
        }
        bool enabled = shadows.floatValue < (float)ShadowMode.Off;
        foreach (Material m in editor.targets) {
            m.SetShaderPassEnabled("ShadowCaster", enabled);
        }
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties) {
        EditorGUI.BeginChangeCheck();
        //首先绘制材质Inspector下原本所有的GUI，例如材质的Properties等
        base.OnGUI(materialEditor, properties);
        editor = materialEditor;
        materials = materialEditor.targets;
        this.properties = properties;
        BakedEmission();

        //空行
        EditorGUILayout.Space();
        showPresets = EditorGUILayout.Foldout(showPresets, "Presets", true);
        if (showPresets) {
            OpaquePreset();
            ClipPreset();
            TransparentPreset();
            PremulTransparent();
        }

        if (EditorGUI.EndChangeCheck()) {
            SetShadowCasterPass();
        }
    }

    void BakedEmission() {
        EditorGUI.BeginChangeCheck();
        editor.LightmapEmissionProperty();
        if (EditorGUI.EndChangeCheck()) {
            foreach (Material m in materials) {
                m.globalIlluminationFlags &=~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
        }
    }

    /// <summary>
    /// 设置材质属性
    /// </summary>
    /// <param name="name">Property名字</param>
    /// <param name="value">属性值</param>
    bool SetProperty(string name, float value) {
        MaterialProperty property = FindProperty(name, properties, false);
        if (property != null) {
            property.floatValue = value;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 同时设置属性、关键字，用于改变shader中的Toggle等属性的属性值和对应的关键词
    /// </summary>
    /// <param name="name">关键词对应的属性名</param>
    /// <param name="keyword">关键词</param>
    /// <param name="value">值</param>
    void SetProperty(string name, string keyword, bool value) {
        if ( SetProperty(name, value ? 1f : 0f) ) {
            SetKeyword(keyword, value);
        }
    }

    /// <summary>
    /// 设置关键词状态
    /// </summary>
    /// <param name="keyword">关键词</param>
    /// <param name="enabled">是否启用</param>
    void SetKeyword(string keyword, bool enabled) {
        if (enabled) {
            foreach (Material m in materials) {
                m.EnableKeyword(keyword);
            }
        }
        else {
            foreach (Material m in materials) {
                m.DisableKeyword(keyword);
            }
        }
    }

    // private bool Clipping {
    //     set => SetProperty("_Clipping", "_CLIPPING", value);
    // }

    private bool PremultiplyAlpha {
        set => SetProperty("_PremulAlpha", "_PREMULTIPLY_ALPHA", value);
    }

    private BlendMode SrcBlend {
        set => SetProperty("_SrcBlend", (float)value);
    }

    private BlendMode DstBlend {
        set => SetProperty("_DstBlend", (float)value);
    }

    private bool ZWrite {
        set => SetProperty("_ZWrite", value ? 1f : 0f);
    }

    private ShadowMode Shadows {
        set {
            if (SetProperty("_Shadows", (float)value)) {
                SetKeyword("_SHADOWS_CLIP", value == ShadowMode.Clip);
                SetKeyword("_SHADOWS_DITHER", value == ShadowMode.Dither);
            }
        }
    }

    RenderQueue RenderQueue {
        set {
            foreach (Material m in materials) {
                m.renderQueue = (int)value;
            }
        }
    }

    /// <summary>
    /// 支持编辑器下的undo操作（撤回）
    /// </summary>
    /// <param name="name">Button name</param>
    /// <returns></returns>
    bool PresetButton(string name) {
        if (GUILayout.Button(name)) {
            editor.RegisterPropertyChangeUndo(name);
            return true;
        }
        return false;
    }

    void OpaquePreset() {
        if (PresetButton("Opaque")) {
            // Clipping = false;
            Shadows = ShadowMode.On;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.Geometry;
        }
    }

    void ClipPreset() {
        if (PresetButton("Clip")) {
            // Clipping = true;
            Shadows = ShadowMode.Clip;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.AlphaTest;
        }
    }

    void TransparentPreset() {
        if (PresetButton("Transparent")) {
            // Clipping = false;
            Shadows = ShadowMode.Dither;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.SrcAlpha;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }

    bool HasProperty(string name) => FindProperty(name, properties, false) != null;
    bool HasPremultiplyAlpha => HasProperty("_PremulAlpha");

    void PremulTransparent() {
        if (HasPremultiplyAlpha && PresetButton("PremulAlpha Transparent")) {
            // Clipping = false;
            Shadows = ShadowMode.Dither;
            PremultiplyAlpha = true;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }
}
