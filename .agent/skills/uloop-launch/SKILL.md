---
name: uloop-launch
description: "uloop CLIを介して、一致するEditorバージョンでUnityプロジェクトを起動する。使用目的: (1) 正しいEditorバージョンでUnityプロジェクトを開く、(2) 変更を適用するためにUnityを再起動する、(3) 起動時にビルドターゲットを切り替える。"
---

# uloop launch

プロジェクトに適したバージョンでUnityエディタを起動する。

## 使用方法

```bash
uloop launch [project-path] [options]
```

## パラメータ

| パラメータ | 型 | 説明 |
|-----------|------|-------------|
| `project-path` | string | Unityプロジェクトへのパス (任意。省略した場合は現在のディレクトリを検索する) |
| `-r, --restart` | boolean | 実行中のUnityを終了して再起動する |
| `-p, --platform <P>` | string | ビルドターゲット (例: StandaloneOSX, Android, iOS) |
| `--max-depth <N>` | number | project-path 省略時の検索深度 (デフォルト: 3, -1 は無制限) |
| `-a, --add-unity-hub` | boolean | Unity Hubへの追加のみ行う (起動はしない) |
| `-f, --favorite` | boolean | お気に入りとしてUnity Hubに追加する (起動はしない) |

## 例

```bash
# 現在のディレクトリでUnityプロジェクトを検索して起動
uloop launch

# 特定のプロジェクトを起動
uloop launch /path/to/project

# Unityを再起動 (既存のプロセスを終了して再起動)
uloop launch -r

# ビルドターゲットを指定して起動
uloop launch -p Android

# 起動せずにプロジェクトをUnity Hubに追加
uloop launch -a
```

## 出力

- 検出されたUnityバージョンを表示
- プロジェクトパスを表示
- Unityが既に実行されている場合、既存のウィンドウをフォーカスする
- 起動する場合、バックグラウンドでUnityを開く
