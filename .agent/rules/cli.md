---
paths: Packages/src/Cli~/**
---

# uloop CLI

Unity Editorと通信するためのCLIツール。MCPサーバー(TypeScriptServer~)からは完全に独立している。

## アーキテクチャ

- **TypeScriptServer~ への依存性ゼロ**
- `direct-unity-client.ts` 内でTCP接続を使用してUnityと直接通信
- MCPサーバーを経由せずにUnityのTCPサーバーと直接やり取りを行う

## ディレクトリ構造

```text
src/
├── cli.ts                 # エントリポイント (commander.js)
├── version.ts             # バージョン管理 (release-pleaseによって自動更新)
├── execute-tool.ts        # ツール実行ロジック
├── direct-unity-client.ts # Unityへの直接TCP通信
├── simple-framer.ts       # TCPフレーミング
├── port-resolver.ts       # ポート検出
├── tool-cache.ts          # ツールキャッシュ (.uloop/tools.json)
├── arg-parser.ts          # 引数解析
├── default-tools.json     # デフォルトのツール定義
├── skills/                # Claude Code スキル機能
│   ├── skills-command.ts
│   ├── skills-manager.ts  # バンドルされたスキルとプロジェクトスキルの収集
│   ├── bundled-skills.ts  # SKILL.md ファイルから自動生成
│   └── skill-definitions/
│       └── cli-only/      # CLI専用の内部スキル
└── __tests__/
    └── cli-e2e.test.ts    # E2Eテスト
```

## グローバルオプション

Unityと通信する全てのコマンドは、以下のグローバルオプションをサポートしている。

| オプション | 説明 |
|--------|-------------|
| `-p, --port <port>` | UnityのTCPポートを直接指定 |
| `--project-path <path>` | ポートを自動解決するためのUnityプロジェクトパスを指定 |

`--port` と `--project-path` は排他的である。

### --project-path

指定されたプロジェクトディレクトリから `UserSettings/UnityMcpSettings.json` を読み込むことで、対象のUnityインスタンスを解決する。パスの解決は `cd` と同じルールに従う。絶対パス（ `/` で始まる）はそのまま使用され、相対パスは現在の作業ディレクトリから解決される。

```bash
# 絶対パス
uloop compile --project-path /Users/foo/moorestech_server

# 相対パス (現在の作業ディレクトリから解決)
uloop compile --project-path ./moorestech_server
uloop compile --project-path ../other/project
```

## ビルド

```bash
npm run build    # dist/cli.bundle.cjs を生成
npm run lint     # ESLint を実行
```

## E2Eテスト

E2Eテストは実際のUnity Editorと通信するため、以下の準備が必要である。

1. Unity Editorが起動していること
2. uLoopMCPパッケージがインストールされていること
3. CLIがビルドされていること (`npm run build`)

```bash
npm run test:cli # E2Eテストを実行 (Unityが起動している必要がある)
```

### ドメインリロードと接続断

**重要**: `compile` コマンドの実行後、Unityはドメインリロード(Domain Reload)をトリガーし、これによりC#のTCPサーバーが強制的に切断される。その結果、Unityへの接続が失敗する数秒間の利用不能時間が生じる。この挙動は回避不能である。

E2Eテストを作成する際の注意点:
- `compile` の後に実行されるコマンドには、 `runCli()` の代わりに `runCliWithRetry()` を使用すること
- 他のテストへの影響を最小限にするため、 `compile --force-recompile` テストはテストスイートの最後に配置すること
- コンパイル関連のテストの直後のテストは、接続が不安定になる可能性があることに留意すること

## npm パブリッシュ

このディレクトリは `uloop-cli` パッケージとしてnpmに公開される。
バージョンは `Packages/src/package.json` と同期される（release-pleaseによって管理）。

## スキルシステム

スキルは以下の2つのソースから収集される。

1. **バンドルされたスキル** (ビルド時): 以下の場所にある `SKILL.md` ファイルから自動生成される。
   - `Editor/Api/McpTools/<ToolFolder>/SKILL.md`
   - `skill-definitions/cli-only/<SkillFolder>/SKILL.md`

2. **プロジェクトスキル** (実行時): Unityプロジェクトの `Editor/` フォルダ内をスキャンして収集される。
   - `Assets/**/Editor/`
   - `Packages/**/Editor/`
   - `Library/PackageCache/**/Editor/`

`npx tsx scripts/generate-bundled-skills.ts` を実行して `bundled-skills.ts` を再生成する。

frontmatterに `internal: true` が指定されているスキルは、バンドルされたスキルから除外される。

現在の内部スキル:
- `uloop-get-project-info`
- `uloop-get-version`

READMEのドキュメントでバンドルされたスキルの数を更新する際は、内部スキルをカウントから除外することを忘れないこと。

## 注意事項

- `version.ts` は TypeScriptServer~ とは別のファイルである（コピーではない）
- ビルド成果物の `dist/cli.bundle.cjs` は `.gitignore` によって除外されている
- `node_modules/` も `.gitignore` によって除外されている
