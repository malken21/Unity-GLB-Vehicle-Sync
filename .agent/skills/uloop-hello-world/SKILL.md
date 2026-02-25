---
name: uloop-hello-world
description: "uloop CLIを介したサンプルツール。MCPツールシステムをテストしたり、カスタムツールの実装例を確認したりする場合に使用する。"
---

# uloop hello-world

多言語サポートを備えたパーソナライズされた Hello World ツール。

## 使用方法

```bash
uloop hello-world [options]
```

## パラメータ

| パラメータ | 型 | デフォルト | 説明 |
|-----------|------|---------|-------------|
| `--name` | string | `World` | 挨拶する名前 |
| `--language` | string | `english` | 挨拶の言語: `english`, `japanese`, `spanish`, `french` |
| `--include-timestamp` | boolean | `true` | レスポンスにタイムスタンプを含めるかどうか |

## 例

```bash
# デフォルトの挨拶
uloop hello-world

# 名前を指定して挨拶
uloop hello-world --name "Alice"

# 日本語での挨拶
uloop hello-world --name "太郎" --language japanese

# タイムスタンプなしでスペイン語の挨拶
uloop hello-world --name "Carlos" --language spanish --include-timestamp false
```

## 出力

以下の内容を含む JSON を返す:
- `Message`: 挨拶メッセージ
- `Language`: 使用された言語
- `Timestamp`: 現在のタイムスタンプ (有効な場合)

## 注意事項

これは、以下を実演するカスタムツールのサンプルである:
- Schema を使用した型安全なパラメータ処理
- 言語選択のための列挙型 (Enum) パラメータ
- ブールフラグパラメータ
- 多言語サポート
