# ScriptableObject 操作

`execute-dynamic-code` を使用した ScriptableObject 操作のコード例。

## ScriptableObject インスタンスの作成

```csharp
using UnityEditor;

ScriptableObject so = ScriptableObject.CreateInstance<ScriptableObject>();
string path = "Assets/Data/MyData.asset";
AssetDatabase.CreateAsset(so, path);
AssetDatabase.SaveAssets();
return $"ScriptableObject を {path} に作成しました";
```

## カスタム ScriptableObject の作成

```csharp
using UnityEditor;

ScriptableObject so = ScriptableObject.CreateInstance("MyCustomSO");
if (so == null)
{
    return "型 'MyCustomSO' が見つかりません。クラスが存在することを確認してください。";
}

string path = "Assets/Data/MyCustomData.asset";
AssetDatabase.CreateAsset(so, path);
AssetDatabase.SaveAssets();
return $"{so.GetType().Name} を {path} に作成しました";
```

## SerializedObject を使用した ScriptableObject の変更

```csharp
using UnityEditor;

string path = "Assets/Data/MyData.asset";
ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

if (so == null)
{
    return $"{path} にアセットが見つかりません";
}

SerializedObject serializedObj = new SerializedObject(so);
SerializedProperty prop = serializedObj.FindProperty("myField");

if (prop != null)
{
    prop.stringValue = "New Value";
    serializedObj.ApplyModifiedProperties();
    EditorUtility.SetDirty(so);
    AssetDatabase.SaveAssets();
    return "プロパティを更新しました";
}
return "プロパティ 'myField' が見つかりません";
```

## Int/Float/Bool プロパティの設定

```csharp
using UnityEditor;

string path = "Assets/Data/GameSettings.asset";
ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
if (so == null)
{
    return $"{path} にアセットが見つかりません";
}

SerializedObject serializedObj = new SerializedObject(so);

SerializedProperty intProp = serializedObj.FindProperty("maxHealth");
if (intProp != null) intProp.intValue = 100;

SerializedProperty floatProp = serializedObj.FindProperty("moveSpeed");
if (floatProp != null) floatProp.floatValue = 5.5f;

SerializedProperty boolProp = serializedObj.FindProperty("isEnabled");
if (boolProp != null) boolProp.boolValue = true;

serializedObj.ApplyModifiedProperties();
EditorUtility.SetDirty(so);
AssetDatabase.SaveAssets();
return "プロパティを更新しました";
```

## 参照プロパティの設定

```csharp
using UnityEditor;

string soPath = "Assets/Data/CharacterData.asset";
string prefabPath = "Assets/Prefabs/Player.prefab";

ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(soPath);
GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

if (so == null)
{
    return $"{soPath} に ScriptableObject が見つかりません";
}
if (prefab == null)
{
    return $"{prefabPath} にプレハブが見つかりません";
}

SerializedObject serializedObj = new SerializedObject(so);
SerializedProperty prop = serializedObj.FindProperty("playerPrefab");

if (prop != null)
{
    prop.objectReferenceValue = prefab;
    serializedObj.ApplyModifiedProperties();
    EditorUtility.SetDirty(so);
    AssetDatabase.SaveAssets();
    return "参照を正常に設定しました";
}
return "プロパティが見つかりません";
```

## 配列/リストプロパティの設定

```csharp
using UnityEditor;

string path = "Assets/Data/ItemDatabase.asset";
ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

if (so == null)
{
    return $"{path} にアセットが見つかりません";
}

SerializedObject serializedObj = new SerializedObject(so);
SerializedProperty arrayProp = serializedObj.FindProperty("items");

if (arrayProp != null && arrayProp.isArray)
{
    arrayProp.ClearArray();
    arrayProp.InsertArrayElementAtIndex(0);
    arrayProp.GetArrayElementAtIndex(0).stringValue = "Sword";
    arrayProp.InsertArrayElementAtIndex(1);
    arrayProp.GetArrayElementAtIndex(1).stringValue = "Shield";

    serializedObj.ApplyModifiedProperties();
    EditorUtility.SetDirty(so);
    AssetDatabase.SaveAssets();
    return "配列を 2 つのアイテムで更新しました";
}
return "配列プロパティが見つかりません";
```

## 特定の型の ScriptableObject をすべて検索

```csharp
using UnityEditor;
using System.Collections.Generic;

string typeName = "GameSettings";
string[] guids = AssetDatabase.FindAssets($"t:{typeName}");
List<string> paths = new List<string>();

foreach (string guid in guids)
{
    paths.Add(AssetDatabase.GUIDToAssetPath(guid));
}
return $"{typeName} アセットが {paths.Count} 個見つかりました";
```

## ScriptableObject の複製

```csharp
using UnityEditor;

string sourcePath = "Assets/Data/Template.asset";
string destPath = "Assets/Data/NewInstance.asset";

ScriptableObject source = AssetDatabase.LoadAssetAtPath<ScriptableObject>(sourcePath);
if (source == null)
{
    return $"{sourcePath} にソースアセットが見つかりません";
}

ScriptableObject copy = Object.Instantiate(source);
AssetDatabase.CreateAsset(copy, destPath);
AssetDatabase.SaveAssets();
return $"{destPath} に複製しました";
```

## ScriptableObject の全プロパティをリスト表示

```csharp
using UnityEditor;
using System.Collections.Generic;

string path = "Assets/Data/MyData.asset";
ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

if (so == null)
{
    return $"{path} にアセットが見つかりません";
}

SerializedObject serializedObj = new SerializedObject(so);
SerializedProperty prop = serializedObj.GetIterator();

List<string> properties = new List<string>();
while (prop.NextVisible(true))
{
    properties.Add($"{prop.name} ({prop.propertyType})");
}
return string.Join(", ", properties);
```
