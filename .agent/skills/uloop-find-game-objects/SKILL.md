---
name: uloop-find-game-objects
description: "シーン内の特定のGameObjectを検索する。名前による検索、特定のコンポーネントを持つオブジェクトの検索、タグやレイヤーによる検索、Unityエディタで現在選択されているGameObjectの取得、またはユーザーからのGameObject検索要求時に使用する。一致するGameObjectをパスとコンポーネント情報と共に返す。"
---

# uloop find-game-objects

検索条件に一致するGameObjectを検索する、または現在選択されているオブジェクトを取得する。

## 使用方法

```bash
uloop find-game-objects [options]
```

## パラメータ

| パラメータ | 型 | デフォルト | 説明 |
|-----------|------|---------|-------------|
| `--name-pattern` | string | - | 検索する名前のパターン |
| `--search-mode` | string | `Contains` | 検索モード: `Exact` (完全一致), `Path` (パス), `Regex` (正規表現), `Contains` (部分一致), `Selected` (選択中) |
| `--required-components` | array | - | 必須コンポーネント |
| `--tag` | string | - | タグフィルター |
| `--layer` | string | - | レイヤーフィルター |
| `--max-results` | integer | `20` | 最大結果件数 |
| `--include-inactive` | boolean | `false` | 非アクティブなGameObjectも含める |

## 検索モード

| モード | 説明 |
|------|-------------|
| `Exact` | 名前の完全一致 |
| `Path` | ヒエラルキーパスによる検索 (例: `Canvas/Button`) |
| `Regex` | 正規表現パターン |
| `Contains` | 名前の部分一致 (デフォルト) |
| `Selected` | Unityエディタで現在選択されているGameObjectを取得 |

## グローバルオプション

| オプション | 説明 |
|--------|-------------|
| `--project-path <path>` | 特定のUnityプロジェクトを対象とする（ `--port` とは排他的）。パスの解決は `cd` と同じルールに従う。絶対パスはそのまま使用され、相対パスは現在の作業ディレクトリから解決される。 |
| `-p, --port <port>` | UnityのTCPポートを直接指定する（ `--project-path` とは排他的）。 |

## 例

```bash
# 名前で検索
uloop find-game-objects --name-pattern "Player"

# コンポーネントで検索
uloop find-game-objects --required-components Rigidbody

# タグで検索
uloop find-game-objects --tag "Enemy"

# 正規表現で検索
uloop find-game-objects --name-pattern "UI_.*" --search-mode Regex

# 選択されているGameObjectを取得
uloop find-game-objects --search-mode Selected

# 非アクティブなものも含めて選択中のオブジェクトを取得
uloop find-game-objects --search-mode Selected --include-inactive
```

## 出力

一致するGameObjectを JSON で返す。

`Selected` モードで複数のオブジェクトを選択している場合、結果は以下のファイルに書き出される:
- 単一選択の場合: JSON レスポンスを直接返す
- 複数選択の場合: `.uloop/outputs/FindGameObjectsResults/` 配下のファイル
- 選択なしの場合: メッセージと共に空の結果を返す
