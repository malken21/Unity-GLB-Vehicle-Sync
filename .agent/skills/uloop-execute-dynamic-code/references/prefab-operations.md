# プレハブ操作

`execute-dynamic-code` を使用したプレハブ操作のコード例。

## GameObject からプレハブを作成

```csharp
using UnityEditor;

GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
cube.name = "MyCube";
string path = "Assets/Prefabs/MyCube.prefab";
PrefabUtility.SaveAsPrefabAsset(cube, path);
Object.DestroyImmediate(cube);
return $"プレハブを {path} に作成しました";
```

## プレハブのインスタンス化

```csharp
using UnityEditor;

string prefabPath = "Assets/Prefabs/MyCube.prefab";
GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
if (prefab == null)
{
    return $"{prefabPath} にプレハブが見つかりません";
}

GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
instance.transform.position = new Vector3(0, 1, 0);
return $"{instance.name} をインスタンス化しました";
```

## プレハブにコンポーネントを追加

```csharp
using UnityEditor;

string prefabPath = "Assets/Prefabs/MyCube.prefab";
GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
if (prefab == null)
{
    return $"{prefabPath} にプレハブが見つかりません";
}
string assetPath = AssetDatabase.GetAssetPath(prefab);

using (PrefabUtility.EditPrefabContentsScope scope = new PrefabUtility.EditPrefabContentsScope(assetPath))
{
    GameObject root = scope.prefabContentsRoot;
    if (root.GetComponent<Rigidbody>() == null)
    {
        root.AddComponent<Rigidbody>();
    }
}
return "プレハブに Rigidbody を追加しました";
```

## プレハブのプロパティを変更

```csharp
using UnityEditor;

string prefabPath = "Assets/Prefabs/MyCube.prefab";
GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
if (prefab == null)
{
    return $"{prefabPath} にプレハブが見つかりません";
}

using (PrefabUtility.EditPrefabContentsScope scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
{
    GameObject root = scope.prefabContentsRoot;
    root.transform.localScale = new Vector3(2, 2, 2);

    MeshRenderer renderer = root.GetComponent<MeshRenderer>();
    if (renderer != null)
    {
        renderer.sharedMaterial.color = Color.red;
    }
}
return "プレハブのプロパティを変更しました";
```

## シーン内の全プレハブインスタンスを検索

```csharp
using UnityEditor;
using System.Collections.Generic;

string prefabPath = "Assets/Prefabs/MyCube.prefab";
GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
if (prefab == null)
{
    return $"{prefabPath} にプレハブが見つかりません";
}

List<GameObject> instances = new List<GameObject>();

foreach (GameObject obj in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
{
    if (PrefabUtility.GetCorrespondingObjectFromSource(obj) == prefab)
    {
        instances.Add(obj);
    }
}
return $"{prefab.name} のインスタンスが {instances.Count} 個見つかりました";
```

## プレハブのオーバーライドを適用

```csharp
using UnityEditor;

GameObject selected = Selection.activeGameObject;
if (selected == null)
{
    return "GameObjectが選択されていません";
}

if (!PrefabUtility.IsPartOfPrefabInstance(selected))
{
    return "選択されたオブジェクトはプレハブインスタンスではありません";
}

PrefabUtility.ApplyPrefabInstance(selected, InteractionMode.UserAction);
return $"{selected.name} のオーバーライドをプレハブに適用しました";
```
