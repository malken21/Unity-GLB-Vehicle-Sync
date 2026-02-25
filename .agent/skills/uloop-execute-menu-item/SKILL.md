---
name: uloop-execute-menu-item
description: "uloop CLIを介してUnityのMenuItemを実行する。メニューコマンドをプログラムで実行する、エディタ操作を自動化する（保存、ビルド、更新）、またはスクリプトで定義されたカスタムメニュー項目を実行する場合に使用する。"
---

# uloop execute-menu-item

Unityの MenuItem を実行する。

## 使用方法

```bash
uloop execute-menu-item --menu-item-path "<path>"
```

## パラメータ

| パラメータ | 型 | デフォルト | 説明 |
|-----------|------|---------|-------------|
| `--menu-item-path` | string | - | メニュー項目のパス (例: "GameObject/Create Empty") |
| `--use-reflection-fallback` | boolean | `true` | リフレクションによるフォールバックを使用する |

## グローバルオプション

| オプション | 説明 |
|--------|-------------|
| `--project-path <path>` | 特定のUnityプロジェクトを対象とする（ `--port` とは排他的）。パスの解決は `cd` と同じルールに従う。絶対パスはそのまま使用され、相対パスは現在の作業ディレクトリから解決される。 |
| `-p, --port <port>` | UnityのTCPポートを直接指定する（ `--project-path` とは排他的）。 |

## 例

```bash
# 空の GameObject を作成
uloop execute-menu-item --menu-item-path "GameObject/Create Empty"

# シーンを保存
uloop execute-menu-item --menu-item-path "File/Save"

# プロジェクト設定を開く
uloop execute-menu-item --menu-item-path "Edit/Project Settings..."
```

## 出力

実行結果を含む JSON を返す。

## 注意事項

- 利用可能なメニューパスを確認するには `uloop get-menu-items` を使用すること
- 一部のメニュー項目は、特定のコンテキストや選択状態が必要な場合がある
