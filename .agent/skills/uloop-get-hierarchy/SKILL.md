---
name: uloop-get-hierarchy
description: "Unityのヒエラルキー構造を取得する。シーン構造の確認、GameObjectの探索、親子関係のチェック、またはユーザーからのヒエラルキー照会時に使用する。シーン内のコンポーネントを含むGameObjectツリーを返す。"
---

# uloop get-hierarchy

Unityのヒエラルキー構造を取得する。

## 使用方法

```bash
uloop get-hierarchy [options]
```

## パラメータ

| パラメータ | 型 | デフォルト | 説明 |
|-----------|------|---------|-------------|
| `--root-path` | string | - | 開始点となるルートGameObjectのパス |
| `--max-depth` | integer | `-1` | 最大深度 (-1 は無制限) |
| `--include-components` | boolean | `true` | コンポーネント情報を含める |
| `--include-inactive` | boolean | `true` | 非アクティブなGameObjectを含める |
| `--include-paths` | boolean | `false` | 完全なパス情報を含める |
| `--use-selection` | boolean | `false` | 選択されているGameObjectをルートとして使用する。trueの場合、 `--root-path` は無視される。 |

## グローバルオプション

| オプション | 説明 |
|--------|-------------|
| `--project-path <path>` | 特定のUnityプロジェクトを対象とする（ `--port` とは排他的）。パスの解決は `cd` と同じルールに従う。絶対パスはそのまま使用され、相対パスは現在の作業ディレクトリから解決される。 |
| `-p, --port <port>` | UnityのTCPポートを直接指定する（ `--project-path` とは排他的）。 |

## 例

```bash
# ヒエラルキー全体を取得
uloop get-hierarchy

# 指定したルートからヒエラルキーを取得
uloop get-hierarchy --root-path "Canvas/UI"

# 深度を制限
uloop get-hierarchy --max-depth 2

# コンポーネント情報なしで取得
uloop get-hierarchy --include-components false

# 現在選択されているGameObjectからヒエラルキーを取得
uloop get-hierarchy --use-selection
```

## 出力

GameObjectとそのコンポーネントの階層構造を含む JSON を返す。
