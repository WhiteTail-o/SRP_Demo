using UnityEngine;

//不允许同一个物体挂载多个该组件
[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
    [SerializeField] Color mBaseColor = Color.white;

    [SerializeField, Range(0.0f, 1.0f)] 
    float mCutOff = 0.5f; 

    [SerializeField, Range(0.0f, 1.0f)]
    float mMetallic = 0.5f;

    [SerializeField, Range(0.0f, 1.0f)]
    float mRoughness = 0.5f;

    [SerializeField, ColorUsage(false, true)]
    Color mEmissionColor = Color.black;

    private static MaterialPropertyBlock block;

    private void OnValidate() {
        if (block == null) {
            block = new MaterialPropertyBlock();
        }

        block.SetColor(ShaderID._Color, mBaseColor);
        block.SetFloat(ShaderID._Cutoff, mCutOff);
        block.SetFloat(ShaderID._Metallic, mMetallic);
        block.SetFloat(ShaderID._Roughness, mRoughness);
        block.SetColor(ShaderID._EmissionColor, mEmissionColor);
        //设置相同材质下，每实例的数据
        GetComponent<Renderer>().SetPropertyBlock(block);
    }

    private void Awake() {
        OnValidate();
    }
}
