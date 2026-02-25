# クリーンアップ操作

`execute-dynamic-code` を使用したプロジェクトのクリーンアップ操作のコード例。

## GameObject 上の Missing Script を検出

```csharp
using UnityEditor;

GameObject selected = Selection.activeGameObject;
if (selected == null)
{
    return "GameObjectが選択されていません";
}

int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(selected);
return $"{selected.name} には {missingCount} 個の Missing Script があります";
```

## GameObject から Missing Script を削除

```csharp
using UnityEditor;

GameObject selected = Selection.activeGameObject;
if (selected == null)
{
    return "GameObjectが選択されていません";
}

int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(selected);
return $"{selected.name} から {removedCount} 個の Missing Script を削除しました";
```

## シーン内の Missing Script をスキャン

```csharp
using UnityEditor;

GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
List<string> objectsWithMissing = new List<string>();

foreach (GameObject obj in allObjects)
{
    int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(obj);
    if (count > 0)
    {
        objectsWithMissing.Add($"{obj.name} ({count}個)");
    }
}

if (objectsWithMissing.Count == 0)
{
    return "シーン内に Missing Script は見つかりませんでした";
}

return $"Missing Script を含むオブジェクト: {string.Join(", ", objectsWithMissing)}";
```

## シーンからすべての Missing Script を削除

```csharp
using UnityEditor;

GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
int totalRemoved = 0;

int undoGroup = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("すべての Missing Script を削除");

foreach (GameObject obj in allObjects)
{
    int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
    totalRemoved += removed;
}

Undo.CollapseUndoOperations(undoGroup);
return $"シーンから計 {totalRemoved} 個の Missing Script を削除しました";
```

## コンポーネント内の Missing Reference を検出

```csharp
using UnityEditor;

GameObject selected = Selection.activeGameObject;
if (selected == null)
{
    return "GameObjectが選択されていません";
}

List<string> missingRefs = new List<string>();

Component[] components = selected.GetComponents<Component>();
foreach (Component comp in components)
{
    if (comp == null) continue;

    SerializedObject so = new SerializedObject(comp);
    SerializedProperty prop = so.GetIterator();

    while (prop.NextVisible(true))
    {
        if (prop.propertyType == SerializedPropertyType.ObjectReference)
        {
            if (prop.objectReferenceValue == null && prop.objectReferenceInstanceIDValue != 0)
            {
                missingRefs.Add($"{comp.GetType().Name}.{prop.name}");
            }
        }
    }
}

if (missingRefs.Count == 0)
{
    return "Missing Reference は見つかりませんでした";
}

return $"Missing Reference: {string.Join(", ", missingRefs)}";
```

## シーン内の Missing Reference をスキャン

```csharp
using UnityEditor;

GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
List<string> results = new List<string>();

foreach (GameObject obj in allObjects)
{
    Component[] components = obj.GetComponents<Component>();
    foreach (Component comp in components)
    {
        if (comp == null) continue;

        SerializedObject so = new SerializedObject(comp);
        SerializedProperty prop = so.GetIterator();

        while (prop.NextVisible(true))
        {
            if (prop.propertyType == SerializedPropertyType.ObjectReference)
            {
                if (prop.objectReferenceValue == null && prop.objectReferenceInstanceIDValue != 0)
                {
                    results.Add($"{obj.name}/{comp.GetType().Name}.{prop.name}");
                }
            }
        }
    }
}

if (results.Count == 0)
{
    return "シーン内に Missing Reference は見つかりませんでした";
}

return $"Missing Reference ({results.Count}件): {string.Join(", ", results.Take(10))}...";
```

## プロジェクト内の不使用マテリアルを検索

```csharp
using UnityEditor;

string[] materialGuids = AssetDatabase.FindAssets("t:Material");
HashSet<string> usedMaterials = new HashSet<string>();

string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
foreach (string guid in prefabGuids)
{
    string path = AssetDatabase.GUIDToAssetPath(guid);
    string[] deps = AssetDatabase.GetDependencies(path, true);
    foreach (string dep in deps)
    {
        if (dep.EndsWith(".mat"))
        {
            usedMaterials.Add(dep);
        }
    }
}

List<string> unusedMaterials = new List<string>();
foreach (string guid in materialGuids)
{
    string path = AssetDatabase.GUIDToAssetPath(guid);
    if (!usedMaterials.Contains(path))
    {
        unusedMaterials.Add(path);
    }
}

return $"{unusedMaterials.Count} 個の不使用の可能性があるマテリアルが見つかりました";
```

## 空の GameObject を検索

```csharpsharp
using UnityEditor;

GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
List<string> emptyObjects = new List<string>();

foreach (GameObject obj in allObjects)
{
    Component[] components = obj.GetComponents<Component>();
    if (components.Length == 1 && obj.transform.childCount == 0)
    {
        emptyObjects.Add(obj.name);
    }
}

if (emptyObjects.Count == 0)
{
    return "空の GameObject は見つかりませんでした";
}

return $"空のオブジェクト ({emptyObjects.Count}件): {string.Join(", ", emptyObjects.Take(20))}";
```

## ヒエラルキー内の重複する名前を検索

```csharp
using UnityEditor;

GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
Dictionary<string, int> nameCounts = new Dictionary<string, int>();

foreach (GameObject obj in allObjects)
{
    if (nameCounts.ContainsKey(obj.name))
    {
        nameCounts[obj.name]++;
    }
    else
    {
        nameCounts[obj.name] = 1;
    }
}

List<string> duplicates = new List<string>();
foreach (KeyValuePair<string, int> kvp in nameCounts)
{
    if (kvp.Value > 1)
    {
        duplicates.Add($"{kvp.Key} ({kvp.Value}件)");
    }
}

if (duplicates.Count == 0)
{
    return "重複する名前は見つかりませんでした";
}

return $"重複名: {string.Join(", ", duplicates.Take(15))}";
```

## 壊れたプレハブインスタンスをチェック

```csharp
using UnityEditor;

GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
List<string> brokenPrefabs = new List<string>();

foreach (GameObject obj in allObjects)
{
    if (PrefabUtility.IsPartOfPrefabInstance(obj))
    {
        GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(obj);
        if (prefabAsset == null)
        {
            brokenPrefabs.Add(obj.name);
        }
    }
}

if (brokenPrefabs.Count == 0)
{
    return "壊れたプレハブインスタンスは見つかりませんでした";
}

return $"壊れたプレハブインスタンス: {string.Join(", ", brokenPrefabs)}";
```

## 負のスケールを持つオブジェクトを検索

```csharp
using UnityEditor;

GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
List<string> negativeScale = new List<string>();

foreach (GameObject obj in allObjects)
{
    Vector3 scale = obj.transform.localScale;
    if (scale.x < 0 || scale.y < 0 || scale.z < 0)
    {
        negativeScale.Add($"{obj.name} ({scale})");
    }
}

if (negativeScale.Count == 0)
{
    return "負のスケールを持つオブジェクトは見つかりませんでした";
}

return $"負のスケールのオブジェクト: {string.Join(", ", negativeScale.Take(10))}";
```

## 空の親 GameObject を削除

```csharp
using UnityEditor;

GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

int undoGroup = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("空の親を削除");

int removedCount = 0;
foreach (GameObject obj in allObjects)
{
    if (obj == null) continue;

    Component[] components = obj.GetComponents<Component>();
    if (components.Length == 1 && obj.transform.childCount == 0)
    {
        Undo.DestroyObjectImmediate(obj);
        removedCount++;
    }
}

Undo.CollapseUndoOperations(undoGroup);
return $"{removedCount} 個の空の GameObject を削除しました";
```

## 巨大なメッシュを検索

```csharp
using UnityEditor;

string[] meshGuids = AssetDatabase.FindAssets("t:Mesh");
List<string> largeMeshes = new List<string>();
int threshold = 10000;

foreach (string guid in meshGuids)
{
    string path = AssetDatabase.GUIDToAssetPath(guid);
    Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
    if (mesh != null && mesh.vertexCount > threshold)
    {
        largeMeshes.Add($"{path} ({mesh.vertexCount} 頂点)");
    }
}

if (largeMeshes.Count == 0)
{
    return $"{threshold} 頂点を超えるメッシュは見つかりませんでした";
}

return $"巨大なメッシュ: {string.Join(", ", largeMeshes.Take(10))}";
```

## アセット参照の検証

```csharp
using UnityEditor;

string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/Data" });
List<string> invalidRefs = new List<string>();

foreach (string guid in guids)
{
    string path = AssetDatabase.GUIDToAssetPath(guid);
    ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
    if (so == null) continue;

    SerializedObject serializedObj = new SerializedObject(so);
    SerializedProperty prop = serializedObj.GetIterator();

    while (prop.NextVisible(true))
    {
        if (prop.propertyType == SerializedPropertyType.ObjectReference)
        {
            if (prop.objectReferenceValue == null && prop.objectReferenceInstanceIDValue != 0)
            {
                invalidRefs.Add($"{path}: {prop.name}");
            }
        }
    }
}

if (invalidRefs.Count == 0)
{
    return "すべてのアセット参照は有効です";
}

return $"無効な参照 ({invalidRefs.Count}件): {string.Join(", ", invalidRefs.Take(10))}";
```

