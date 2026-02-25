# Undo 操作

`execute-dynamic-code` を使用した Undo（元に戻す）をサポートする操作のコード例。

## プロパティ変更の記録 (Undo.RecordObject)

```csharp
using UnityEditor;

GameObject selected = Selection.activeGameObject;
if (selected == null)
{
    return "GameObjectが選択されていません";
}

Undo.RecordObject(selected.transform, "オブジェクトの移動");
selected.transform.position = new Vector3(0, 5, 0);
return $"{selected.name} を移動しました (Undo可能)";
```

## 複数オブジェクトの記録

```csharp
using UnityEditor;

GameObject[] selectedObjects = Selection.gameObjects;
if (selectedObjects.Length == 0)
{
    return "GameObjectが選択されていません";
}

Object[] transforms = new Object[selectedObjects.Length];
for (int i = 0; i < selectedObjects.Length; i++)
{
    transforms[i] = selectedObjects[i].transform;
}

Undo.RecordObjects(transforms, "複数オブジェクトの移動");
foreach (GameObject obj in selectedObjects)
{
    obj.transform.position += Vector3.up * 2;
}
return $"{selectedObjects.Length} 個のオブジェクトを移動しました (Undo可能)";
```

## オブジェクトの完全な Undo（複雑な変更用）

```csharp
using UnityEditor;

GameObject selected = Selection.activeGameObject;
if (selected == null)
{
    return "GameObjectが選択されていません";
}

Undo.RegisterCompleteObjectUndo(selected, "オブジェクトの完全変更");
selected.name = "RenamedObject";
selected.layer = LayerMask.NameToLayer("Default");
selected.tag = "Untagged";
return $"オブジェクトを完全に変更しました (Undo可能)";
```

## Undo 付きのコンポーネント追加

```csharp
using UnityEditor;

GameObject selected = Selection.activeGameObject;
if (selected == null)
{
    return "GameObjectが選択されていません";
}

Rigidbody rb = Undo.AddComponent<Rigidbody>(selected);
rb.mass = 2f;
rb.useGravity = true;
return $"{selected.name} に Rigidbody を追加しました (Undo可能)";
```

## Undo 付きの親設定

```csharp
using UnityEditor;

GameObject child = GameObject.Find("Child");
GameObject parent = GameObject.Find("Parent");

if (child == null || parent == null)
{
    return "Child または Parent が見つかりません";
}

Undo.SetTransformParent(child.transform, parent.transform, "親の設定");
return $"{child.name} の親を {parent.name} に設定しました (Undo可能)";
```

## Undo 付きの GameObject 作成

```csharp
using UnityEditor;

GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
cube.name = "UndoableCube";
cube.transform.position = new Vector3(0, 1, 0);
Undo.RegisterCreatedObjectUndo(cube, "キューブの作成");
return $"{cube.name} を作成しました (Undo可能)";
```

## Undo 付きの GameObject 破棄

```csharp
using UnityEditor;

GameObject obj = GameObject.Find("ObjectToDelete");
if (obj == null)
{
    return "GameObjectが見つかりません";
}

Undo.DestroyObjectImmediate(obj);
return "GameObject を破棄しました (Undo可能)";
```

## 名前付き Undo グループ

```csharp
using UnityEditor;

GameObject selected = Selection.activeGameObject;
if (selected == null)
{
    return "GameObjectが選択されていません";
}

Undo.SetCurrentGroupName("複雑な Transform 操作");

Undo.RecordObject(selected.transform, "");
selected.transform.position = Vector3.zero;
selected.transform.rotation = Quaternion.identity;
selected.transform.localScale = Vector3.one;

return "Transform をリセットしました (Undoは1ステップ)";
```

## 複数の操作を 1 つの Undo にまとめる

```csharp
using UnityEditor;

int undoGroup = Undo.GetCurrentGroup();
Undo.SetCurrentGroupName("一括操作");

GameObject cube1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
cube1.name = "Cube1";
Undo.RegisterCreatedObjectUndo(cube1, "");

GameObject cube2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
cube2.name = "Cube2";
cube2.transform.position = Vector3.right * 2;
Undo.RegisterCreatedObjectUndo(cube2, "");

GameObject cube3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
cube3.name = "Cube3";
cube3.transform.position = Vector3.right * 4;
Undo.RegisterCreatedObjectUndo(cube3, "");

Undo.CollapseUndoOperations(undoGroup);
return "3 つのキューブを作成しました (Undoは1ステップ)";
```

## Undo 付きの ScriptableObject の変更

```csharp
using UnityEditor;

string path = "Assets/Data/GameSettings.asset";
ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
if (so == null)
{
    return $"{path} にアセットが見つかりません";
}

Undo.RecordObject(so, "設定の変更");
SerializedObject serializedObj = new SerializedObject(so);
SerializedProperty prop = serializedObj.FindProperty("maxHealth");
if (prop != null)
{
    prop.intValue = 200;
    serializedObj.ApplyModifiedProperties();
}
return "ScriptableObject を変更しました (Undo可能)";
```

## Undo 付きのマテリアルの変更

```csharp
using UnityEditor;

string path = "Assets/Materials/MyMaterial.mat";
Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
if (mat == null)
{
    return $"{path} にマテリアルが見つかりません";
}

Undo.RecordObject(mat, "マテリアル色の変更");
mat.color = Color.red;
return "マテリアルの色を変更しました (Undo可能)";
```

