---
name: uloop-unity-search
description: "Unityプロジェクト内でアセットを検索する。シーン、プレハブ、スクリプト、マテリアル、その他のアセットを名前や型で検索する場合、またはユーザーからプロジェクトファイルの検索を求められた場合に使用する。アセットパスとメタデータを返す。"
---

# uloop unity-search

Unity Searchを使用してUnityプロジェクト内を検索する。

## 使用方法

```bash
uloop unity-search [options]
```

## パラメータ

| パラメータ | 型 | デフォルト | 説明 |
|-----------|------|---------|-------------|
| `--search-query` | string | - | 検索クエリ |
| `--providers` | array | - | 検索プロバイダー (例: `asset`, `scene`, `find`) |
| `--max-results` | integer | `50` | 最大結果件数 |
| `--save-to-file` | boolean | `false` | 結果をファイルに保存する |

## グローバルオプション

| オプション | 説明 |
|--------|-------------|
| `--project-path <path>` | 特定のUnityプロジェクトを対象とする（ `--port` とは排他的）。パスの解決は `cd` と同じルールに従う。絶対パスはそのまま使用され、相対パスは現在の作業ディレクトリから解決される。 |
| `-p, --port <port>` | UnityのTCPポートを直接指定する（ `--project-path` とは排他的）。 |

## 例

```bash
# アセットを検索
uloop unity-search --search-query "Player"

# 特定のプロバイダーを使用して検索
uloop unity-search --search-query "t:Prefab" --providers asset

# 結果件数を制限
uloop unity-search --search-query "*.cs" --max-results 20
```

## 出力

パスとメタデータを含む検索結果の JSON 配列を返す。

## 注意事項

利用可能な検索プロバイダーを確認するには `uloop get-provider-details` を使用すること。
