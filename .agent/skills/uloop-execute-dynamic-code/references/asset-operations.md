# アセット操作

`execute-dynamic-code` を使用した AssetDatabase 操作のコード例。

## 型によるアセット検索

```csharp
using UnityEditor;
using System.Collections.Generic;

string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
List<string> paths = new List<string>();

foreach (string guid in prefabGuids)
{
    paths.Add(AssetDatabase.GUIDToAssetPath(guid));
}
return $"{paths.Count} 個のプレハブが見つかりました";
```

## 名前によるアセット検索

```csharp
using UnityEditor;
using System.Collections.Generic;

string searchName = "Player";
string[] guids = AssetDatabase.FindAssets(searchName);
List<string> paths = new List<string>();

foreach (string guid in guids)
{
    paths.Add(AssetDatabase.GUIDToAssetPath(guid));
}
return $"'{searchName}' に一致するアセットが {paths.Count} 個見つかりました";
```

## フォルダ内のアセット検索

```csharp
using UnityEditor;
using System.Collections.Generic;

string folder = "Assets/Prefabs";
string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
List<string> paths = new List<string>();

foreach (string guid in guids)
{
    paths.Add(AssetDatabase.GUIDToAssetPath(guid));
}
return $"{folder} 内に {paths.Count} 個のプレハブが見つかりました";
```

## アセットの複製

```csharp
using UnityEditor;

string sourcePath = "Assets/Materials/MyMaterial.mat";
string destPath = "Assets/Materials/MyMaterial_Backup.mat";

bool success = AssetDatabase.CopyAsset(sourcePath, destPath);
return success ? $"{destPath} にコピーしました" : "コピーに失敗しました";
```

## アセットの移動

```csharp
using UnityEditor;

string sourcePath = "Assets/OldFolder/MyAsset.asset";
string destPath = "Assets/NewFolder/MyAsset.asset";

string error = AssetDatabase.MoveAsset(sourcePath, destPath);
return string.IsNullOrEmpty(error) ? $"{destPath} に移動しました" : $"エラー: {error}";
```

## アセット名の変更

```csharp
using UnityEditor;

string assetPath = "Assets/Materials/OldName.mat";
string newName = "NewName";

string error = AssetDatabase.RenameAsset(assetPath, newName);
return string.IsNullOrEmpty(error) ? $"{newName} に名前を変更しました" : $"エラー: {error}";
```

## アセット名の変更 (Undo対応)

```csharp
using UnityEditor;

// ObjectNames.SetNameSmart() は Undo をサポートしています (AssetDatabase.RenameAsset() はサポートしていません)
Object selected = Selection.activeObject;
if (selected == null)
{
    return "アセットが選択されていません";
}

string oldName = selected.name;
ObjectNames.SetNameSmart(selected, "NewName");
AssetDatabase.SaveAssets();
return $"{oldName} から {selected.name} に名前を変更しました";
```

## オブジェクトからアセットパスを取得

```csharp
using UnityEditor;

GameObject selected = Selection.activeGameObject;
if (selected == null)
{
    return "オブジェクトが選択されていません";
}

string path = AssetDatabase.GetAssetPath(selected);
if (string.IsNullOrEmpty(path))
{
    return "選択されたオブジェクトはアセットではありません (シーンオブジェクトです)";
}
return $"アセットパス: {path}";
```

## 指定パスのアセットをロード

```csharp
using UnityEditor;

string path = "Assets/Prefabs/Player.prefab";
GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

if (asset == null)
{
    return $"{path} にアセットが見つかりません";
}
return $"ロード完了: {asset.name}";
```

## 特定の型の全アセットを取得

```csharp
using UnityEditor;

string[] scriptGuids = AssetDatabase.FindAssets("t:MonoScript");
int count = 0;

foreach (string guid in scriptGuids)
{
    string path = AssetDatabase.GUIDToAssetPath(guid);
    if (path.StartsWith("Assets/"))
    {
        count++;
    }
}
return $"Assets フォルダ内に {count} 個のスクリプトが見つかりました";
```

## アセットの存在確認

```csharp
using UnityEditor;

string path = "Assets/Prefabs/Player.prefab";
string guid = AssetDatabase.AssetPathToGUID(path);

bool exists = !string.IsNullOrEmpty(guid);
return exists ? $"アセットが存在します: {path}" : $"アセットが見つかりません: {path}";
```

## アセットの依存関係を取得

```csharp
using UnityEditor;

string assetPath = "Assets/Prefabs/Player.prefab";
string[] dependencies = AssetDatabase.GetDependencies(assetPath, true);

return $"アセットには {dependencies.Length} 個の依存関係があります";
```

## AssetDatabase のリフレッシュ

```csharp
using UnityEditor;

AssetDatabase.Refresh();
return "AssetDatabase をリフレッシュしました";
```
