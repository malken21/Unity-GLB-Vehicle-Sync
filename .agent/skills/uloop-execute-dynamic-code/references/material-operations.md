# マテリアル操作

`execute-dynamic-code` を使用したマテリアル操作のコード例。

## 新規マテリアルの作成

```csharp
using UnityEditor;

Shader shader = Shader.Find("Standard");
Material mat = new Material(shader);
mat.name = "MyMaterial";
string path = "Assets/Materials/MyMaterial.mat";
AssetDatabase.CreateAsset(mat, path);
AssetDatabase.SaveAssets();
return $"マテリアルを {path} に作成しました";
```

## マテリアルの色を設定

```csharp
using UnityEditor;

string matPath = "Assets/Materials/MyMaterial.mat";
Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
mat.SetColor("_Color", new Color(1f, 0.5f, 0f, 1f));
EditorUtility.SetDirty(mat);
AssetDatabase.SaveAssets();
return "マテリアルの色をオレンジに設定しました";
```

## マテリアルのプロパティ設定 (Float, Vector)

```csharp
using UnityEditor;

string matPath = "Assets/Materials/MyMaterial.mat";
Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

mat.SetFloat("_Metallic", 0.8f);
mat.SetFloat("_Glossiness", 0.6f);
mat.SetVector("_EmissionColor", new Vector4(1, 1, 0, 1));

EditorUtility.SetDirty(mat);
AssetDatabase.SaveAssets();
return "マテリアルのプロパティを更新しました";
```

## マテリアルにテクスチャを割り当て

```csharp
using UnityEditor;

string matPath = "Assets/Materials/MyMaterial.mat";
string texPath = "Assets/Textures/MyTexture.png";

Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

mat.SetTexture("_MainTex", tex);
EditorUtility.SetDirty(mat);
AssetDatabase.SaveAssets();
return $"マテリアルに {tex.name} を割り当てました";
```

## GameObject にマテリアルを割り当て

```csharp
using UnityEditor;

string matPath = "Assets/Materials/MyMaterial.mat";
Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

GameObject selected = Selection.activeGameObject;
if (selected == null)
{
    return "GameObjectが選択されていません";
}

Renderer renderer = selected.GetComponent<Renderer>();
if (renderer == null)
{
    return "選択されたオブジェクトに Renderer がありません";
}

renderer.sharedMaterial = mat;
EditorUtility.SetDirty(selected);
return $"{selected.name} に {mat.name} を割り当てました";
```

## マテリアルキーワードの有効化/無効化

```csharp
using UnityEditor;

string matPath = "Assets/Materials/MyMaterial.mat";
Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

mat.EnableKeyword("_EMISSION");
mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

EditorUtility.SetDirty(mat);
AssetDatabase.SaveAssets();
return "マテリアルの Emission を有効にしました";
```

## 特定のシェーダーを使用している全マテリアルを検索

```csharp
using UnityEditor;

string shaderName = "Standard";
string[] guids = AssetDatabase.FindAssets("t:Material");
List<string> matchingMaterials = new List<string>();

foreach (string guid in guids)
{
    string path = AssetDatabase.GUIDToAssetPath(guid);
    Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
    if (mat != null && mat.shader != null && mat.shader.name == shaderName)
    {
        matchingMaterials.Add(path);
    }
}
return $"{shaderName} を使用しているマテリアルが {matchingMaterials.Count} 個見つかりました";
```

## マテリアルの複製

```csharp
using UnityEditor;

string sourcePath = "Assets/Materials/MyMaterial.mat";
string destPath = "Assets/Materials/MyMaterial_Copy.mat";

Material source = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
Material copy = new Material(source);
AssetDatabase.CreateAsset(copy, destPath);
AssetDatabase.SaveAssets();
return $"マテリアルを {destPath} に複製しました";
```
