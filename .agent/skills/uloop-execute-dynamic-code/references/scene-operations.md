# シーン操作

`execute-dynamic-code` を使用したシーンおよびヒエラルキー操作のコード例。

## GameObject の作成

```csharp
GameObject obj = new GameObject("MyObject");
obj.transform.position = new Vector3(0, 1, 0);
return $"{obj.name} を作成しました";
```

## UI GameObject の作成 (Canvas配下)

```csharp
using UnityEngine.UI;

// UIオブジェクトには RectTransform が必要です（Canvas の子にすると自動追加されます）
GameObject canvas = GameObject.Find("Canvas");
if (canvas == null)
{
    return "シーン内に Canvas が見つかりません";
}

GameObject uiObj = new GameObject("MyUIElement");
uiObj.transform.SetParent(canvas.transform, false);
uiObj.AddComponent<RectTransform>();
uiObj.AddComponent<Image>();
return $"UI要素を作成しました: {uiObj.name}";
```

## プリミティブの作成

```csharp
GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
cube.name = "MyCube";
cube.transform.position = new Vector3(2, 0, 0);
return $"{cube.name} を作成しました";
```

## GameObject にコンポーネントを追加

```csharp
GameObject selected = Selection.activeGameObject;
if (selected == null)
{
    return "GameObjectが選択されていません";
}

Rigidbody rb = selected.AddComponent<Rigidbody>();
rb.mass = 2f;
rb.useGravity = true;
return $"{selected.name} に Rigidbody を追加しました";
```

## 名前で GameObject を検索

```csharp
GameObject obj = GameObject.Find("Player");
if (obj == null)
{
    return "GameObject 'Player' が見つかりません";
}
return $"発見: {obj.name} (座標: {obj.transform.position})";
```

## タグで GameObject を検索

```csharp
GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
return $"タグ 'Enemy' を持つ GameObject が {enemies.Length} 個見つかりました";
```

## 親の設定

```csharp
GameObject child = GameObject.Find("Child");
GameObject parent = GameObject.Find("Parent");

if (child == null || parent == null)
{
    return "Child または Parent が見つかりません";
}

child.transform.SetParent(parent.transform);
return $"{child.name} の親を {parent.name} に設定しました";
```

## すべての子を取得

```csharp
GameObject parent = Selection.activeGameObject;
if (parent == null)
{
    return "GameObjectが選択されていません";
}

List<string> children = new List<string>();
foreach (Transform child in parent.transform)
{
    children.Add(child.name);
}
return $"子オブジェクト: {string.Join(", ", children)}";
```

## コンポーネント参照の接続

```csharp
using UnityEditor;

GameObject player = GameObject.Find("Player");
GameObject target = GameObject.Find("Target");

if (player == null || target == null)
{
    return "Player または Target が見つかりません";
}

MonoBehaviour script = player.GetComponent("PlayerController") as MonoBehaviour;
if (script == null)
{
    return "Player に PlayerController が見つかりません";
}

SerializedObject serializedScript = new SerializedObject(script);
SerializedProperty targetProp = serializedScript.FindProperty("target");

if (targetProp != null)
{
    targetProp.objectReferenceValue = target.transform;
    serializedScript.ApplyModifiedProperties();
    return "Target 参照を接続しました";
}
return "プロパティ 'target' が見つかりません";
```

## シーンのロード (Editor)

```csharp
using UnityEditor.SceneManagement;

string scenePath = "Assets/Scenes/MainMenu.unity";
EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
return $"シーンをロードしました: {scenePath}";
```

## 現在のシーンを保存

```csharp
using UnityEditor.SceneManagement;

UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
EditorSceneManager.SaveScene(scene);
return $"シーンを保存しました: {scene.name}";
```

## 新規シーンの作成

```csharp
using UnityEditor.SceneManagement;

UnityEngine.SceneManagement.Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
return $"新規シーンを作成しました: {newScene.name}";
```

## シーン内のルート GameObject をすべて取得

```csharp
UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
GameObject[] roots = scene.GetRootGameObjects();

List<string> names = new List<string>();
foreach (GameObject root in roots)
{
    names.Add(root.name);
}
return $"ルートオブジェクト: {string.Join(", ", names)}";
```

## GameObject の破棄

```csharp
GameObject obj = GameObject.Find("OldObject");
if (obj == null)
{
    return "GameObjectが見つかりません";
}

Object.DestroyImmediate(obj);
return "GameObject を破棄しました";
```

## GameObject の複製

```csharp
GameObject selected = Selection.activeGameObject;
if (selected == null)
{
    return "GameObjectが選択されていません";
}

GameObject copy = Object.Instantiate(selected);
copy.name = selected.name + "_Copy";
copy.transform.position = selected.transform.position + Vector3.right * 2;
return $"複製を作成しました: {copy.name}";
```

## アクティブ/非アクティブの設定

```csharp
GameObject obj = GameObject.Find("MyObject");
if (obj == null)
{
    return "GameObjectが見つかりません";
}

obj.SetActive(!obj.activeSelf);
return $"{obj.name} を {(obj.activeSelf ? "アクティブ" : "非アクティブ")} にしました";
```

## Transform の変更

```csharp
GameObject selected = Selection.activeGameObject;
if (selected == null)
{
    return "GameObjectが選択されていません";
}

selected.transform.position = new Vector3(0, 5, 0);
selected.transform.rotation = Quaternion.Euler(0, 45, 0);
selected.transform.localScale = new Vector3(2, 2, 2);
return "Transform を変更しました";
```
