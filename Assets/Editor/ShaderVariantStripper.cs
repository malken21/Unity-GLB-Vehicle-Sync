using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

public class ShaderVariantStripper : IPreprocessShaders
{
    public int callbackOrder => 0;

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        // Target specifically the URP Lit shader family which is causing the explosion
        if (shader.name != "Universal Render Pipeline/Lit" && 
            shader.name != "Universal Render Pipeline/Simple Lit" &&
            shader.name != "Universal Render Pipeline/Complex Lit")
            return;

        for (int i = data.Count - 1; i >= 0; --i)
        {
            var shaderKeywordSet = data[i].shaderKeywordSet;

            // Strip Debug Display variants (not needed for release builds usually)
            // This drastically reduces variants as it multiplies with everything
            if (shaderKeywordSet.IsEnabled(new ShaderKeyword("DEBUG_DISPLAY")))
            {
                data.RemoveAt(i);
                continue;
            }

            // Strip Additional Light Shadows (Point/Spot shadows)
            // This is often the main cause of combinatorial explosion (main light shadows + additional light shadows)
            // If you need point light shadows, remove this block, but expect build times to increase.
            if (shaderKeywordSet.IsEnabled(new ShaderKeyword("_ADDITIONAL_LIGHT_SHADOWS")))
            {
                data.RemoveAt(i);
                continue;
            }
        }
    }
}
