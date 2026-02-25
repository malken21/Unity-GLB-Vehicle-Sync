---
name: uloop-get-provider-details
description: "uloop CLIを介してUnity Searchプロバイダーの詳細情報を取得する。使用目的: (1) 利用可能な検索プロバイダーを探索する、(2) 検索機能やフィルターを理解する、(3) 特定のプロバイダーオプションを使用して検索を設定する。"
---

# uloop get-provider-details

Unity Searchプロバイダーに関する詳細情報を取得する。

## 使用方法

```bash
uloop get-provider-details [options]
```

## パラメータ

| パラメータ | 型 | デフォルト | 説明 |
|-----------|------|---------|-------------|
| `--provider-id` | string | - | 照会する特定のプロバイダーID |
| `--active-only` | boolean | `false` | アクティブなプロバイダーのみを表示する |
| `--include-descriptions` | boolean | `true` | 説明を含める |
| `--sort-by-priority` | boolean | `true` | 優先順位でソートする |

## グローバルオプション

| オプション | 説明 |
|--------|-------------|
| `--project-path <path>` | 特定のUnityプロジェクトを対象とする（ `--port` とは排他的）。パスの解決は `cd` と同じルールに従う。絶対パスはそのまま使用され、相対パスは現在の作業ディレクトリから解決される。 |
| `-p, --port <port>` | UnityのTCPポートを直接指定する（ `--project-path` とは排他的）。 |

## 例

```bash
# 全てのプロバイダーをリストアップ
uloop get-provider-details

# 特定のプロバイダーを取得
uloop get-provider-details --provider-id asset

# アクティブなプロバイダーのみ
uloop get-provider-details --active-only
```

## 出力

以下の JSON を返す:
- `Providers`: プロバイダー情報の配列 (ID, 名前, 説明, 優先順位)

## 注意事項

プロバイダーIDは `uloop unity-search --providers` オプションで使用する。
