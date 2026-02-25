---
name: uloop-get-logs
description: "Unityコンソールのログを確認する。ログのチェック、エラーのデバッグ、失敗の調査、またはユーザーからのコンソール出力照会時に使用する。主なオプション: --log-type (Error/Warning/Log/All), --max-count, --search-text。エラー、警告、Debug.Logメッセージを取得する。"
---

# uloop get-logs

Unityコンソールからログを取得する。

## 使用方法

```bash
uloop get-logs [options]
```

## パラメータ

| パラメータ | 型 | デフォルト | 説明 |
|-----------|------|---------|-------------|
| `--log-type` | string | `All` | ログタイプのフィルター: `Error`, `Warning`, `Log`, `All` |
| `--max-count` | integer | `100` | 取得するログの最大数 |
| `--search-text` | string | - | ログ内で検索するテキスト |
| `--include-stack-trace` | boolean | `false` | 出力にスタックトレースを含める |
| `--use-regex` | boolean | `false` | 検索に正規表現を使用する |
| `--search-in-stack-trace` | boolean | `false` | スタックトレース内を検索する |

## グローバルオプション

| オプション | 説明 |
|--------|-------------|
| `--project-path <path>` | 特定のUnityプロジェクトを対象とする（ `--port` とは排他的）。パスの解決は `cd` と同じルールに従う。絶対パスはそのまま使用され、相対パスは現在の作業ディレクトリから解決される。 |
| `-p, --port <port>` | UnityのTCPポートを直接指定する（ `--project-path` とは排他的）。 |

## 例

```bash
# 全てのログを取得
uloop get-logs

# エラーのみを取得
uloop get-logs --log-type Error

# 特定のテキストを検索
uloop get-logs --search-text "NullReference"

# 正規表現で検索
uloop get-logs --search-text "Missing.*Component" --use-regex
```

## 出力

メッセージ、タイプ、および任意でスタックトレースを含むログエントリの JSON 配列を返す。
