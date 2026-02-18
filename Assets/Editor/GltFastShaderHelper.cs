using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;
using System.Collections.Generic;

public class GltFastShaderHelper
{
    [MenuItem("Tools/glTFast/Update Graphics Settings")]
    public static void UpdateGraphicsSettings()
    {
        var ensureShaders = new List<string>
        {
            "glTF/PbrMetallicRoughness",
            "glTF/Unlit",
            "Shader Graphs/glTF-pbrMetallicRoughness-Opaque",
            "Shader Graphs/glTF-pbrMetallicRoughness-Transparent",
            "Shader Graphs/glTF-unlit-Opaque",
            "Shader Graphs/glTF-unlit-Transparent"
        };

        var graphicsSettings = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
        var serializedObject = new SerializedObject(graphicsSettings);
        var arrayProp = serializedObject.FindProperty("m_AlwaysIncludedShaders");

        bool changed = false;

        foreach (var shaderName in ensureShaders)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[GltFastShaderHelper] Shader not found: {shaderName}");
                continue;
            }

            bool present = false;
            for (int i = 0; i < arrayProp.arraySize; ++i)
            {
                var element = arrayProp.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == shader)
                {
                    present = true;
                    break;
                }
            }

            if (!present)
            {
                int index = arrayProp.arraySize;
                arrayProp.InsertArrayElementAtIndex(index);
                arrayProp.GetArrayElementAtIndex(index).objectReferenceValue = shader;
                changed = true;
                Debug.Log($"[GltFastShaderHelper] Added shader to Always Included: {shaderName}");
            }
        }

        if (changed)
        {
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log("[GltFastShaderHelper] Graphics Settings updated successfully.");
        }
        else
        {
            Debug.Log("[GltFastShaderHelper] No changes needed.");
        }
    }
}
