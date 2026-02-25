---
name: uloop-run-tests
description: "Unity Test Runnerを実行し、詳細な結果を取得する。使用目的: (1) ユニットテスト (EditMode/PlayMode) の実行、(2) コード変更の検証、(3) テスト失敗の診断 — テスト失敗時にはエラーメッセージとスタックトレースを含む NUnit XML が自動保存される。"
---

# uloop run-tests

Unity Test Runnerを実行する。テストが失敗した場合、エラーメッセージとスタックトレースを含む NUnit XML 結果が自動的に保存される。詳細な失敗診断については、 `XmlPath` にある XML ファイルを参照すること。

## 使用方法

```bash
uloop run-tests [options]
```

## パラメータ

| パラメータ | 型 | デフォルト | 説明 |
|-----------|------|---------|-------------|
| `--test-mode` | string | `EditMode` | テストモード: `EditMode`, `PlayMode` |
| `--filter-type` | string | `all` | フィルタータイプ: `all`, `exact` (一致), `regex` (正規表現), `assembly` (アセンブリ) |
| `--filter-value` | string | - | フィルター値 (テスト名、パターン、またはアセンブリ名) |

## グローバルオプション

| オプション | 説明 |
|--------|-------------|
| `--project-path <path>` | 特定のUnityプロジェクトを対象とする（ `--port` とは排他的）。パスの解決は `cd` と同じルールに従う。絶対パスはそのまま使用され、相対パスは現在の作業ディレクトリから解決される。 |
| `-p, --port <port>` | UnityのTCPポートを直接指定する（ `--project-path` とは排他的）。 |

## 例

```bash
# 全ての EditMode テストを実行
uloop run-tests

# PlayMode テストを実行
uloop run-tests --test-mode PlayMode

# 特定のテストを実行
uloop run-tests --filter-type exact --filter-value "MyTest.TestMethod"

# パターンに一致するテストを実行
uloop run-tests --filter-type regex --filter-value ".*Integration.*"
```

## 出力

以下の内容を含む JSON を返す:
- `Success` (boolean): 全てのテストに合格したかどうか
- `Message` (string): 概要メッセージ
- `TestCount` (number): 実行された全テスト数
- `PassedCount` (number): 合格したテスト数
- `FailedCount` (number): 失敗したテスト数
- `SkippedCount` (number): スキップされたテスト数
- `XmlPath` (string): NUnit XML 結果ファイルへのパス (テスト失敗時に自動保存)

### XML 結果ファイル

テストが失敗すると、NUnit XML 結果が `{project_root}/.uloop/outputs/TestResults/<timestamp>.xml` に自動的に保存される。この XML には、以下を含むテストケースごとの結果が記述されている:
- テスト名およびフルネーム
- 合否/スキップの状態、および実行時間
- 失敗したテストの場合: `<message>` (アサーションエラー) および `<stack-trace>`
