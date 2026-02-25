---
name: uloop-execute-dynamic-code
description: "uloop CLIを介してUnityエディタ内でC#コードを動的に実行する。エディタの自動化に使用する: (1) プレハブやマテリアルの接続、AddComponent操作 (2) SerializedObjectを使用した参照の接続 (3) シーンや階層の編集、バッチ処理。ファイルI/Oやスクリプトの作成には使用しない。"
---

# uloop execute-dynamic-code

Unityエディタ内でC#コードを動的に実行する。

## 使用方法

```bash
uloop execute-dynamic-code --code '<c# code>'
```

## パラメータ

| パラメータ | 型 | 説明 |
|-----------|------|-------------|
| `--code` | string | 実行するC#コード (クラスラップなしの直接的なステートメント) |
| `--compile-only` | boolean | 実行せずにコンパイルのみ行う |
| `--auto-qualify-unity-types-once` | boolean | Unityの型を自動的に修飾する |

## コード形式

直接的なステートメントのみを記述する（クラス/名前空間/メソッドなし）。戻り値 (return) は任意である。

```csharp
// 冒頭の using ディレクティブは巻上げられる
using UnityEngine;
var x = Mathf.PI;
return x;
```

## 文字列リテラル (シェル別)

| シェル | 記述方法 |
|-------|--------|
| bash/zsh/MINGW64/Git Bash | `'Debug.Log("Hello!");'` |
| PowerShell | `'Debug.Log(""Hello!"");'` |

## 許可される操作

- プレハブ/マテリアルの接続 (PrefabUtility)
- AddComponent および参照の接続 (SerializedObject)
- シーン/階層の編集
- インスペクターの修正

## 禁止されている操作

- System.IO.* (File/Directory/Path)
- AssetDatabase.CreateFolder / ファイル書き込み
- .cs/.asmdef ファイルの作成または編集

## グローバルオプション

| オプション | 説明 |
|--------|-------------|
| `--project-path <path>` | 特定のUnityプロジェクトを対象とする（ `--port` とは排他的）。パスの解決は `cd` と同じルールに従う。絶対パスはそのまま使用され、相対パスは現在の作業ディレクトリから解決される。 |
| `-p, --port <port>` | UnityのTCPポートを直接指定する（ `--project-path` とは排他的）。 |

## 例

### bash / zsh / MINGW64 / Git Bash

```bash
uloop execute-dynamic-code --code 'return Selection.activeGameObject?.name;'
uloop execute-dynamic-code --code 'new GameObject("MyObject");'
uloop execute-dynamic-code --code 'UnityEngine.Debug.Log("Hello from CLI!");'
```

### PowerShell

```powershell
uloop execute-dynamic-code --code 'return Selection.activeGameObject?.name;'
uloop execute-dynamic-code --code 'new GameObject(""MyObject"");'
uloop execute-dynamic-code --code 'UnityEngine.Debug.Log(""Hello from CLI!"");'
```

## 出力

実行結果またはコンパイルエラーを含む JSON を返す。

## 注意事項

ファイルやディレクトリの操作には、代わりに端末コマンドを使用すること。

## カテゴリ別コード例

詳細なコード例については、以下のファイルを参照すること:

- **プレハブ操作**: [references/prefab-operations.md](references/prefab-operations.md) を参照
  - プレハブの作成、インスタンス化、コンポーネントの追加、プロパティの修正
- **マテリアル操作**: [references/material-operations.md](references/material-operations.md) を参照
  - マテリアルの作成、シェーダー/テクスチャの設定、プロパティの修正
- **アセット操作**: [references/asset-operations.md](references/asset-operations.md) を参照
  - アセットの検索、複製、移動、名前変更、ロード
- **ScriptableObject**: [references/scriptableobject.md](references/scriptableobject.md) を参照
  - ScriptableObject の作成、SerializedObject による修正
- **シーン操作**: [references/scene-operations.md](references/scene-operations.md) を参照
  - GameObject の作成/修正、親の設定、参照の接続、シーンのロード
