using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using GLTFast;
using GLTFast.Materials;
using GLTFast.Schema;
using GLTFast.Logging;

public class CustomMaterialGenerator : IMaterialGenerator
{
    private readonly IMaterialGenerator _internalGenerator;
    private readonly Shader _fallbackShader;
    private ICodeLogger _logger;

    public CustomMaterialGenerator(Shader fallbackShader)
    {
        _fallbackShader = fallbackShader;
        
        var urpAsset = (UniversalRenderPipelineAsset)(QualitySettings.renderPipeline ? QualitySettings.renderPipeline : GraphicsSettings.defaultRenderPipeline);
        _internalGenerator = new UniversalRPMaterialGenerator(urpAsset);
    }

    public UnityEngine.Material GetDefaultMaterial(bool pointsSupport = false)
    {
        return _internalGenerator.GetDefaultMaterial(pointsSupport);
    }

    public UnityEngine.Material GenerateMaterial(MaterialBase gltfMaterial, IGltfReadable gltf, bool pointsSupport = false)
    {
        UnityEngine.Material mat = _internalGenerator.GenerateMaterial(gltfMaterial, gltf, pointsSupport);

        if (mat != null && _fallbackShader != null)
        {
            string sName = mat.shader != null ? mat.shader.name : "null";
            bool isBroken = mat.shader == null || 
                            sName == "Hidden/InternalErrorShader" || 
                            sName == "" || 
                            sName == "unlit" || 
                            sName.Contains("Error");

            if (isBroken)
            {
                _logger?.Warning(LogCode.ShaderMissing, sName + " (Fallback to " + _fallbackShader.name + ")");
                mat.shader = _fallbackShader;
            }
        }

        return mat;
    }

    public void SetLogger(ICodeLogger logger)
    {
        _logger = logger;
        _internalGenerator.SetLogger(logger);
    }
}
