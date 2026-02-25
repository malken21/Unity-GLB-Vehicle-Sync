# Selection 操作

`execute-dynamic-code` を使用した Selection 操作のコード例。

## 選択中の GameObject を取得

```csharp
using UnityEditor;
using System.Collections.Generic;

GameObject[] selected = Selection.gameObjects;
if (selected.Length == 0)
{
    return "GameObjectが選択されていません";
}

List<string> names = new List<string>();
foreach (GameObject obj in selected)
{
    names.Add(obj.name);
}
return $"選択中: {string.Join(", ", names)}";
```

## アクティブな (最後に選択した) GameObject を取得

```csharp
using UnityEditor;

GameObject active = Selection.activeGameObject;
if (active == null)
{
    return "アクティブな GameObject がありません";
}
return $"アクティブ: {active.name}";
```

## プログラムによる選択の設定

```csharp
using UnityEditor;

GameObject obj = GameObject.Find("Player");
if (obj == null)
{
    return "GameObject 'Player' が見つかりません";
}

Selection.activeGameObject = obj;
return $"{obj.name} を選択しました";
```

## 複数の GameObject を選択

```csharp
using UnityEditor;

GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
if (enemies.Length == 0)
{
    return "Enemy が見つかりません";
}

Selection.objects = enemies;
return $"{enemies.Length} 個の Enemy を選択しました";
```

## 最上位（Top-Level）の Transform のみを取得

```csharp
using UnityEditor;
using System.Collections.Generic;

Transform[] transforms = Selection.GetTransforms(SelectionMode.TopLevel);
if (transforms.Length == 0)
{
    return "Transform が選択されていません";
}

List<string> names = new List<string>();
foreach (Transform t in transforms)
{
    names.Add(t.name);
}
return $"最上位オブジェクト: {string.Join(", ", names)}";
```

## 深層の選択（子オブジェクトを含む）を取得

```csharp
using UnityEditor;

Transform[] transforms = Selection.GetTransforms(SelectionMode.Deep);
if (transforms.Length == 0)
{
    return "Transform が選択されていません";
}

return $"深層選択の合計件数: {transforms.Length}";
```

## 編集可能なオブジェクトのみを取得

```csharp
using UnityEditor;
using System.Collections.Generic;

Transform[] transforms = Selection.GetTransforms(SelectionMode.Editable);
if (transforms.Length == 0)
{
    return "編集可能な Transform が選択されていません";
}

List<string> names = new List<string>();
foreach (Transform t in transforms)
{
    names.Add(t.name);
}
return $"編集可能: {string.Join(", ", names)}";
```

## 選択中のアセットを取得

```csharp
using UnityEditor;
using System.Collections.Generic;

Object[] selectedAssets = Selection.GetFiltered<Object>(SelectionMode.Assets);
if (selectedAssets.Length == 0)
{
    return "アセットが選択されていません";
}

List<string> paths = new List<string>();
foreach (Object asset in selectedAssets)
{
    paths.Add(AssetDatabase.GetAssetPath(asset));
}
return $"アセット: {string.Join(", ", paths)}";
```

## 選択中のアセット GUID を取得

```csharp
using UnityEditor;
using System.Collections.Generic;

string[] guids = Selection.assetGUIDs;
if (guids.Length == 0)
{
    return "アセットが選択されていません";
}

List<string> paths = new List<string>();
foreach (string guid in guids)
{
    paths.Add(AssetDatabase.GUIDToAssetPath(guid));
}
return $"選択されたアセット: {string.Join(", ", paths)}";
```

## 選択したオブジェクトのすべての子を選択

```csharp
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

GameObject parent = Selection.activeGameObject;
if (parent == null)
{
    return "GameObjectが選択されていません";
}

List<GameObject> children = new List<GameObject>();
foreach (Transform child in parent.GetComponentsInChildren<Transform>())
{
    if (child != parent.transform)
    {
        children.Add(child.gameObject);
    }
}

if (children.Count == 0)
{
    return "子オブジェクトが見つかりません";
}

Selection.objects = children.ToArray();
return $"{children.Count} 個の子を選択しました";
```

## コンポーネントによる選択のフィルタリング

```csharp
using UnityEditor;
using System.Collections.Generic;

GameObject[] selected = Selection.gameObjects;
List<GameObject> withRigidbody = new List<GameObject>();

foreach (GameObject obj in selected)
{
    if (obj.GetComponent<Rigidbody>() != null)
    {
        withRigidbody.Add(obj);
    }
}

if (withRigidbody.Count == 0)
{
    return "選択範囲内に Rigidbody を持つオブジェクトはありません";
}

Selection.objects = withRigidbody.ToArray();
return $"Rigidbody を持つ {withRigidbody.Count} 個のオブジェクトに絞り込みました";
```

## オブジェクトが選択されているか確認

```csharp
using UnityEditor;

GameObject player = GameObject.Find("Player");
if (player == null)
{
    return "Player が見つかりません";
}

bool isSelected = Selection.Contains(player);
return $"Player は選択されてい{(isSelected ? "" : "ません")}";
```

## 選択の解除

```csharp
using UnityEditor;

Selection.activeObject = null;
return "選択を解除しました";
```

## レイヤーでオブジェクトを選択

```csharp
using UnityEditor;
using System.Collections.Generic;

int layer = LayerMask.NameToLayer("UI");
GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
List<GameObject> layerObjects = new List<GameObject>();

foreach (GameObject obj in allObjects)
{
    if (obj.layer == layer)
    {
        layerObjects.Add(obj);
    }
}

if (layerObjects.Count == 0)
{
    return "UI レイヤーにオブジェクトが見つかりません";
}

Selection.objects = layerObjects.ToArray();
return $"UI レイヤー上の {layerObjects.Count} 個のオブジェクトを選択しました";
```

## ヒエラルキー/プロジェクト内でオブジェクトをピン留め（注目）

```csharp
using UnityEditor;

GameObject obj = GameObject.Find("Player");
if (obj == null)
{
    return "Player が見つかりません";
}

EditorGUIUtility.PingObject(obj);
return $"ヒエラルキー内の {obj.name} をピン留めしました";
```

## Scene ビューで選択したオブジェクトにフォーカス

```csharp
using UnityEditor;

if (Selection.activeGameObject == null)
{
    return "GameObjectが選択されていません";
}

SceneView.FrameLastActiveSceneView();
return "選択したオブジェクトを画面中央に表示しました";
```

