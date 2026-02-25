---
name: uloop-compile
description: "Unityプロジェクトをコンパイルする。編集後のコードのコンパイル確認、コンパイルエラーのチェック、またはユーザーからのコンパイル要求時に使用する。エラーと警告の数を返す。"
---

# uloop compile

Unityプロジェクトのコンパイルを実行する。

## 使用方法

```bash
uloop compile [--force-recompile] [--wait-for-domain-reload]
```

## パラメータ

| パラメータ | 型 | 説明 |
|-----------|------|-------------|
| `--force-recompile` | boolean | 完全な再コンパイルを強制する (ドメインリロードをトリガーする) |
| `--wait-for-domain-reload` | boolean | ドメインリロードが完了するまで待機してから復帰する |

## グローバルオプション

| オプション | 説明 |
|--------|-------------|
| `--project-path <path>` | 特定のUnityプロジェクトを対象とする（ `--port` とは排他的）。パスの解決は `cd` と同じルールに従う。絶対パスはそのまま使用され、相対パスは現在の作業ディレクトリから解決される。 |
| `-p, --port <port>` | UnityのTCPポートを直接指定する（ `--project-path` とは排他的）。 |

## 例

```bash
# コンパイルチェック
uloop compile

# 完全な再コンパイルを強制
uloop compile --force-recompile

# 再コンパイルを強制し、ドメインリロードの完了を待機
uloop compile --force-recompile true --wait-for-domain-reload true

# 再コンパイルを強制せずにドメインリロードの完了のみ待機
uloop compile --force-recompile false --wait-for-domain-reload true
```

## 出力

以下の JSON を返す:
- `Success`: boolean (成功したかどうか)
- `ErrorCount`: number (エラー数)
- `WarningCount`: number (警告数)

## トラブルシューティング

コンパイル後に CLI がハングしたり、「Unity is busy」というエラーが表示されたりする場合、古いロックファイルが接続を妨げている可能性がある。以下のコマンドを実行してクリーンアップすること:

```bash
uloop fix
```

これにより、Unityプロジェクトの Temp ディレクトリから残っているロックファイル (`compiling.lock`, `domainreload.lock`, `serverstarting.lock`) が削除される。
