using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MeshBall : MonoBehaviour
{
    [SerializeField] private Mesh mesh = default;
    [SerializeField] private Material material = default;
    [SerializeField] private LightProbeProxyVolume lightProbeProxyVolume = null;
    [SerializeField, Range(0f, 1f)] private float cutoff = 0.5f;

    private Matrix4x4[] matrices = new Matrix4x4[1023];
    private Vector4[] baseColors = new Vector4[1023];
    private float[] metallic = new float[1023];
    private float[] roughness = new float[1023];

    private MaterialPropertyBlock block;

    private void Awake() {
        for (int i = 0; i < matrices.Length; i++) {
            matrices[i] = Matrix4x4.TRS(
                Random.insideUnitSphere * 10.0f,
                Quaternion.identity,
                Vector3.one
            );

            baseColors[i] = new Vector4(Random.value, Random.value, Random.value, Random.Range(0.1f, 0.3f));
            metallic[i] = Random.value < 0.25f ? 1.0f : 0.0f;
            roughness[i] = Random.Range(0.05f, 0.95f);
        }
    }

    private void Update() {
        if (block == null) {
            block = new MaterialPropertyBlock();
            block.SetVectorArray(ShaderID._Color, baseColors);
            block.SetFloatArray(ShaderID._Metallic, metallic);
            block.SetFloatArray(ShaderID._Roughness, roughness);
            block.SetFloat(ShaderID._Cutoff, cutoff);
            if (!lightProbeProxyVolume) {
                var positions = new Vector3[1023];
                for (int i = 0; i < matrices.Length; i++) {
                    positions[i] = matrices[i].GetColumn(3);
                }
                var lightProbes = new SphericalHarmonicsL2[1023];
                LightProbes.CalculateInterpolatedLightAndOcclusionProbes(positions, lightProbes, null);
                block.CopySHCoefficientArraysFrom(lightProbes);
            }
        }

        Graphics.DrawMeshInstanced(mesh, 0, material, matrices, 1023, block, ShadowCastingMode.On, true, 0, null, LightProbeUsage.CustomProvided);
    }
}
