# 一括操作

`execute-dynamic-code` を使用した一括処理のコード例。

## 選択したオブジェクトの一括変更

```csharp
using UnityEditor;

GameObject[] selected = Selection.gameObjects;
if (selected.Length == 0)
{
    return "GameObjectが選択されていません";
}

int undoGroup = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("一括変更");

foreach (GameObject obj in selected)
{
    Undo.RecordObject(obj.transform, "");
    obj.transform.localScale = Vector3.one * 2;
}

Undo.CollapseUndoOperations(undoGroup);
return $"{selected.Length} 個のオブジェクトをスケーリングしました (Undoは1ステップ)";
```

## SerializedObject を使用した複数オブジェクトの編集

```csharp
using UnityEditor;

GameObject[] selected = Selection.gameObjects;
if (selected.Length == 0)
{
    return "GameObjectが選択されていません";
}

List<Transform> transforms = new List<Transform>();
foreach (GameObject obj in selected)
{
    transforms.Add(obj.transform);
}

SerializedObject serializedObj = new SerializedObject(transforms.ToArray());
SerializedProperty positionProp = serializedObj.FindProperty("m_LocalPosition");
positionProp.vector3Value = Vector3.zero;
serializedObj.ApplyModifiedProperties();

return $"{selected.Length} 個のオブジェクトの座標をリセットしました";
```

## コンポーネントの一括追加

```csharp
using UnityEditor;

GameObject[] selected = Selection.gameObjects;
if (selected.Length == 0)
{
    return "GameObjectが選択されていません";
}

int undoGroup = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("Rigidbodyの一括追加");

int addedCount = 0;
foreach (GameObject obj in selected)
{
    if (obj.GetComponent<Rigidbody>() == null)
    {
        Undo.AddComponent<Rigidbody>(obj);
        addedCount++;
    }
}

Undo.CollapseUndoOperations(undoGroup);
return $"{addedCount} 個のオブジェクトに Rigidbody を追加しました";
```

## StartAssetEditing を使用したアセットの一括処理

```csharp
using UnityEditor;

string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials" });
if (guids.Length == 0)
{
    return "マテリアルが見つかりません";
}

AssetDatabase.StartAssetEditing();

int modified = 0;
foreach (string guid in guids)
{
    string path = AssetDatabase.GUIDToAssetPath(guid);
    Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
    if (mat != null)
    {
        mat.color = Color.white;
        EditorUtility.SetDirty(mat);
        modified++;
    }
}

AssetDatabase.StopAssetEditing();
AssetDatabase.SaveAssets();

return $"{modified} 個のマテリアルの色をリセットしました";
```

## GameObject の一括名前変更

```csharp
using UnityEditor;

GameObject[] selected = Selection.gameObjects;
if (selected.Length == 0)
{
    return "GameObjectが選択されていません";
}

int undoGroup = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("一括名前変更");

for (int i = 0; i < selected.Length; i++)
{
    Undo.RecordObject(selected[i], "");
    selected[i].name = $"Item_{i:D3}";
}

Undo.CollapseUndoOperations(undoGroup);
return $"{selected.Length} 個のオブジェクトの名前を変更しました";
```

## レイヤーの一括設定

```csharp
using UnityEditor;

GameObject[] selected = Selection.gameObjects;
if (selected.Length == 0)
{
    return "GameObjectが選択されていません";
}

int layer = LayerMask.NameToLayer("Default");

int undoGroup = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("一括レイヤー設定");

foreach (GameObject obj in selected)
{
    Undo.RecordObject(obj, "");
    obj.layer = layer;
}

Undo.CollapseUndoOperations(undoGroup);
return $"{selected.Length} 個のオブジェクトのレイヤーを Default に設定しました";
```

## タグの一括設定

```csharp
using UnityEditor;

GameObject[] selected = Selection.gameObjects;
if (selected.Length == 0)
{
    return "GameObjectが選択されていません";
}

int undoGroup = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("一括タグ設定");

foreach (GameObject obj in selected)
{
    Undo.RecordObject(obj, "");
    obj.tag = "Enemy";
}

Undo.CollapseUndoOperations(undoGroup);
return $"{selected.Length} 個のオブジェクトを Enemy としてタグ付けしました";
```

## ScriptableObject の一括変更

```csharp
using UnityEditor;

string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/Data" });
if (guids.Length == 0)
{
    return "ScriptableObjectが見つかりません";
}

int modified = 0;
foreach (string guid in guids)
{
    string path = AssetDatabase.GUIDToAssetPath(guid);
    ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
    if (so == null) continue;

    SerializedObject serializedObj = new SerializedObject(so);
    SerializedProperty prop = serializedObj.FindProperty("isEnabled");
    if (prop != null)
    {
        prop.boolValue = true;
        serializedObj.ApplyModifiedProperties();
        EditorUtility.SetDirty(so);
        modified++;
    }
}

AssetDatabase.SaveAssets();
return $"{modified} 個の ScriptableObject を有効にしました";
```

## コンポーネントの一括削除

```csharp
using UnityEditor;

GameObject[] selected = Selection.gameObjects;
if (selected.Length == 0)
{
    return "GameObjectが選択されていません";
}

int undoGroup = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("Rigidbodyの一括削除");

int removedCount = 0;
foreach (GameObject obj in selected)
{
    Rigidbody rb = obj.GetComponent<Rigidbody>();
    if (rb != null)
    {
        Undo.DestroyObjectImmediate(rb);
        removedCount++;
    }
}

Undo.CollapseUndoOperations(undoGroup);
return $"{removedCount} 個のオブジェクトから Rigidbody を削除しました";
```

## Staticフラグの一括設定

```csharp
using UnityEditor;

GameObject[] selected = Selection.gameObjects;
if (selected.Length == 0)
{
    return "GameObjectが選択されていません";
}

int undoGroup = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("一括Static設定");

foreach (GameObject obj in selected)
{
    Undo.RecordObject(obj, "");
    GameObjectUtility.SetStaticEditorFlags(obj, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
}

Undo.CollapseUndoOperations(undoGroup);
return $"{selected.Length} 個のオブジェクトにStaticフラグを設定しました";
```

## プログレスバーを使用した一括処理

```csharp
using UnityEditor;

string[] guids = AssetDatabase.FindAssets("t:Texture2D");
if (guids.Length == 0)
{
    return "テクスチャが見つかりません";
}

int processed = 0;
foreach (string guid in guids)
{
    string path = AssetDatabase.GUIDToAssetPath(guid);
    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
    if (importer != null && importer.maxTextureSize > 1024)
    {
        importer.maxTextureSize = 1024;
        importer.SaveAndReimport();
        processed++;
    }

    if (processed % 10 == 0)
    {
        EditorUtility.DisplayProgressBar("テクスチャ処理中", path, (float)processed / guids.Length);
    }
}

EditorUtility.ClearProgressBar();
return $"{processed} 個のテクスチャを最大1024にリサイズしました";
```

## オブジェクトの一括整列

```csharp
using UnityEditor;

GameObject[] selected = Selection.gameObjects;
if (selected.Length < 2)
{
    return "2つ以上のオブジェクトを選択してください";
}

int undoGroup = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("オブジェクトの整列");

float startX = selected[0].transform.position.x;
float spacing = 2f;

for (int i = 0; i < selected.Length; i++)
{
    Undo.RecordObject(selected[i].transform, "");
    Vector3 pos = selected[i].transform.position;
    pos.x = startX + (i * spacing);
    selected[i].transform.position = pos;
}

Undo.CollapseUndoOperations(undoGroup);
return $"{selected.Length} 個のオブジェクトを {spacing}m 間隔で整列しました";
```

## アセットの一括名前変更 (Undo対応)

```csharp
using UnityEditor;

// ObjectNames.SetNameSmart() は Undo をサポートしています (AssetDatabase.RenameAsset() はサポートしていません)
Object[] selected = Selection.objects;
if (selected.Length == 0)
{
    return "アセットが選択されていません";
}

for (int i = 0; i < selected.Length; i++)
{
    string newName = $"{i:D3}_{selected[i].name}";
    ObjectNames.SetNameSmart(selected[i], newName);
}

AssetDatabase.SaveAssets();
return $"{selected.Length} 個のアセットの名前を変更しました";
```

## マテリアルの一括置換

```csharp
using UnityEditor;

GameObject[] selected = Selection.gameObjects;
if (selected.Length == 0)
{
    return "GameObjectが選択されていません";
}

string materialPath = "Assets/Materials/NewMaterial.mat";
Material newMat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
if (newMat == null)
{
    return $"{materialPath} にマテリアルが見つかりません";
}

int undoGroup = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("マテリアルの一括置換");

int replaced = 0;
foreach (GameObject obj in selected)
{
    MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
    if (renderer != null)
    {
        Undo.RecordObject(renderer, "");
        renderer.sharedMaterial = newMat;
        replaced++;
    }
}

Undo.CollapseUndoOperations(undoGroup);
return $"{replaced} 個のオブジェクトのマテリアルを置換しました";
```

