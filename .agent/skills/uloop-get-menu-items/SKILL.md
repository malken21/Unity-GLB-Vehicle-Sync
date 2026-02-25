---
name: uloop-get-menu-items
description: "uloop CLIを介してUnityのMenuItemを取得する。使用目的: (1) Unityエディタで利用可能なメニューコマンドを探索する、(2) 自動化のためのメニューパスを見つける、(3) プログラムによるメニュー実行の準備をする。"
---

# uloop get-menu-items

Unityの MenuItem を取得する。

## 使用方法

```bash
uloop get-menu-items [options]
```

## パラメータ

| パラメータ | 型 | デフォルト | 説明 |
|-----------|------|---------|-------------|
| `--filter-text` | string | - | フィルターテキスト |
| `--filter-type` | string | `contains` | フィルタータイプ: `contains` (含む), `exact` (一致), `startswith` (前方一致) |
| `--max-count` | integer | `200` | 最大取得件数 |
| `--include-validation` | boolean | `false` | バリデーション関数を含める |

## グローバルオプション

| オプション | 説明 |
|--------|-------------|
| `--project-path <path>` | 特定のUnityプロジェクトを対象とする（ `--port` とは排他的）。パスの解決は `cd` と同じルールに従う。絶対パスはそのまま使用され、相対パスは現在の作業ディレクトリから解決される。 |
| `-p, --port <port>` | UnityのTCPポートを直接指定する（ `--project-path` とは排他的）。 |

## 例

```bash
# 全てのメニュー項目をリストアップ
uloop get-menu-items

# テキストでフィルター
uloop get-menu-items --filter-text "GameObject"

# 完全一致
uloop get-menu-items --filter-text "File/Save" --filter-type exact
```

## 出力

パスとメタデータを含むメニュー項目の JSON 配列を返す。

## 注意事項

取得したメニューコマンドを実行するには `uloop execute-menu-item` を使用すること。
