---
name: uloop-clear-console
description: "Unityのコンソールログを消去する。テスト前のコンソールクリア、新しいデバッグセッションの開始、またはユーザーからのログ消去要求時に使用する。Unityコンソールの全てのログエントリを削除する。"
---

# uloop clear-console

Unityのコンソールログを消去する。

## 使用方法

```bash
uloop clear-console [--add-confirmation-message]
```

## パラメータ

| パラメータ | 型 | デフォルト | 説明 |
|-----------|------|---------|-------------|
| `--add-confirmation-message` | boolean | `false` | 消去後に確認メッセージを追加する |

## グローバルオプション

| オプション | 説明 |
|--------|-------------|
| `--project-path <path>` | 特定のUnityプロジェクトを対象とする（ `--port` とは排他的）。パスの解決は `cd` と同じルールに従う。絶対パスはそのまま使用され、相対パスは現在の作業ディレクトリから解決される。 |
| `-p, --port <port>` | UnityのTCPポートを直接指定する（ `--project-path` とは排他的）。 |

## 例

```bash
# コンソールを消去
uloop clear-console

# 確認メッセージ付きで消去
uloop clear-console --add-confirmation-message
```

## 出力

コンソールが消去されたことを確認する JSON を返す。
