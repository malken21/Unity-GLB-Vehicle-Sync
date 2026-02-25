---
paths: Packages/src/Editor/Api/**
---

# MCP ツール開発ガイド

このドキュメントでは、uLoopMCP 用の新しい MCP ツールを作成する方法を説明する。

## ディレクトリ構造

```text
McpTools/
├── Core/                    # 基本クラスとインフラストラクチャ
│   ├── AbstractUnityTool.cs # 全てのツールの基底クラス
│   ├── BaseToolSchema.cs    # パラメータスキーマの基底クラス
│   ├── BaseToolResponse.cs  # レスポンスの基底クラス
│   └── McpToolAttribute.cs  # ツール登録用のアトリビュート
├── YourNewTool/             # 新しいツールのフォルダ
│   ├── YourNewToolSchema.cs
│   ├── YourNewToolResponse.cs
│   ├── YourNewToolTool.cs
│   └── SKILL.md             # スキルドキュメント (任意)
└── ...
```

## ステップバイステップ: 新しい MCP ツールの作成

### ステップ 1: ツールフォルダの作成

`McpTools/` 配下に、ツール名 (PascalCase) と同じ名前の新しいフォルダを作成する。

```bash
mkdir McpTools/YourNewTool
```

### ステップ 2: スキーマクラスの作成

スキーマ (Schema) は、ツールの入力パラメータを定義する。

`YourNewToolSchema.cs`:

```csharp
using System.ComponentModel;

namespace io.github.hatayama.uLoopMCP
{
    public class YourNewToolSchema : BaseToolSchema
    {
        [Description("MCPツールのスキーマに表示される説明")]
        public string SomeParameter { get; set; } = "default value";

        [Description("列挙型を使用した別のパラメータ")]
        public SomeEnum Mode { get; set; } = SomeEnum.Default;

        [Description("数値パラメータ")]
        public float Scale { get; set; } = 1.0f;
    }

    public enum SomeEnum
    {
        Default = 0,
        Option1 = 1,
        Option2 = 2
    }
}
```

**重要事項:**
- `BaseToolSchema` を継承すること
- パラメータのドキュメント化には `[Description]` アトリビュートを使用すること
- 任意のパラメータにはデフォルト値を設定すること
- 列挙型 (Enum) は、MCP スキーマ内で自動的に文字列の選択肢に変換される

### ステップ 3: レスポンスクラスの作成

レスポンス (Response) は、ツールが返す内容を定義する。

`YourNewToolResponse.cs`:

```csharp
#nullable enable

namespace io.github.hatayama.uLoopMCP
{
    public class YourNewToolResponse : BaseToolResponse
    {
        public string? ResultPath { get; set; }
        public int? Count { get; set; }
        public bool Success { get; set; }

        public YourNewToolResponse(string resultPath, int count)
        {
            ResultPath = resultPath;
            Count = count;
            Success = true;
        }

        public YourNewToolResponse(bool failure)
        {
            ResultPath = null;
            Count = null;
            Success = false;
        }

        public YourNewToolResponse()
        {
        }
    }
}
```

**重要事項:**
- `BaseToolResponse` を継承すること
- ヌル安全 (Null Safety) のために `#nullable enable` を使用すること
- 成功時と失敗時のためのコンストラクタを提供すること
- JSON デシリアライズのためにデフォルトコンストラクタを含めること

### ステップ 4: ツールクラスの作成

ツール (Tool) クラスにメインロジックを記述する。

`YourNewToolTool.cs`:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

namespace io.github.hatayama.uLoopMCP
{
    [McpTool(Description = "このツールの動作に関する簡潔な説明")]
    public class YourNewToolTool : AbstractUnityTool<YourNewToolSchema, YourNewToolResponse>
    {
        public override string ToolName => "your-new-tool";  // kebab-case

        protected override async Task<YourNewToolResponse> ExecuteAsync(
            YourNewToolSchema parameters,
            CancellationToken ct)
        {
            string correlationId = McpConstants.GenerateCorrelationId();

            VibeLogger.LogInfo(
                "your_new_tool_start",
                "ツールの実行を開始しました",
                new { Mode = parameters.Mode.ToString() },
                correlationId: correlationId
            );

            // パラメータのバリデーション
            ValidateParameters(parameters);

            // ここにツールのロジックを記述
            // 注: 既にメインスレッド上で実行されているため、MainThreadSwitcher を呼び出す必要はない

            VibeLogger.LogInfo(
                "your_new_tool_success",
                "ツールが正常に完了しました",
                new { ResultPath = "some/path" },
                correlationId: correlationId
            );

            return new YourNewToolResponse("some/path", 42);
        }

        private void ValidateParameters(YourNewToolSchema parameters)
        {
            if (parameters.Scale < 0.1f || parameters.Scale > 2.0f)
            {
                throw new ArgumentException(
                    $"Scaleは0.1から2.0の間である必要があります。入力値: {parameters.Scale}");
            }
        }
    }
}
```

**重要事項:**
- `[McpTool(Description = "...")]` アトリビュートを追加すること
- `AbstractUnityTool<TSchema, TResponse>` を継承すること
- `ToolName` を kebab-case の文字列として設定すること
- `CancellationToken ct` というパラメータ名を使用すること
- ロギングには `VibeLogger` を使用すること
- try-catch は不要（プロジェクト方針に従う）

### ステップ 5: コンパイルとテスト

1. Unity でコンパイル:
   ```
   mcp_uLoopMCP_compile
   ```

2. MCP 経由でテスト:
   ```
   mcp_uLoopMCP_your-new-tool
   ```

### ステップ 6: SKILL.md の作成 (任意)

CLI スキルをサポートするために、ツールと同じフォルダに `SKILL.md` を作成する。

`McpTools/YourNewTool/SKILL.md`:

```markdown
---
name: uloop-your-new-tool
description: AIコンテキスト用の簡潔な説明。以下の目的で使用する: (1) 第一のユースケース, (2) 第二のユースケース。
---

# uloop your-new-tool

このツールが何を行うかを一行で説明。

## 使用方法

\`\`\`bash
uloop your-new-tool [--some-parameter <value>] [--mode <mode>]
\`\`\`

## パラメータ

| パラメータ | 型 | デフォルト | 説明 |
|-----------|------|---------|-------------|
| `--some-parameter` | string | `""` | 説明 |
| `--mode` | enum | `Default` | 選択肢: `Default`, `Option1`, `Option2` |
| `--scale` | number | `1.0` | スケール係数 (0.1 から 2.0) |

## 例

\`\`\`bash
# 基本的な使用方法
uloop your-new-tool

# パラメータを指定
uloop your-new-tool --mode Option1 --scale 0.5
\`\`\`

## 出力

以下の内容を含む JSON を返す:
- `ResultPath`: 結果へのパス
- `Count`: 処理されたアイテムの数
- `Success`: 操作が成功したかどうか
```

**注:** frontmatter に `internal: true` を追加すると、バンドルされたスキルから除外される。

### ステップ 7: bundled-skills.ts の生成

`bundled-skills.ts` ファイルは、SKILL.md ファイルから**自動生成**される。

**仕組み:**
- `scripts/generate-bundled-skills.ts` スクリプトが以下をスキャンする:
  - `Editor/Api/McpTools/<ToolFolder>/SKILL.md`
  - `skill-definitions/cli-only/<SkillFolder>/SKILL.md`
- frontmatter に `internal: true` があるスキルは除外される

**生成コマンド:**
```bash
cd Packages/src/Cli~
npx tsx scripts/generate-bundled-skills.ts
```

**注:** これは `npm run build` 時にも自動的に実行される。

### ステップ 8: default-tools.json の更新 (手動)

`Packages/src/Cli~/src/default-tools.json` にツールのスキーマを追加する。

```json
{
  "name": "your-new-tool",
  "description": "簡潔な説明",
  "inputSchema": {
    "type": "object",
    "properties": {
      "SomeParameter": {
        "type": "string",
        "description": "説明"
      },
      "Mode": {
        "type": "string",
        "enum": ["Default", "Option1", "Option2"],
        "default": "Default"
      }
    }
  }
}
```

### ステップ 9: Lint とビルドの実行

```bash
cd Packages/src/Cli~
npm run lint && npm run build
```

これにより以下が実行される:
1. TypeScript ファイルに対して ESLint を実行
2. SKILL.md ファイルから `bundled-skills.ts` を再生成
3. esbuild で CLI をバンドル

### ステップ 10: CLI と Unity の同期

CLI はツール定義にキャッシュファイル (`.uloop/tools.json`) を使用する。新しいツールを追加した後は、Unity と同期する必要がある。

```bash
uloop sync
```

**ツールの読み込みの仕組み:**
1. CLI が `.uloop/tools.json` (キャッシュファイル) を確認
2. キャッシュが存在すれば、キャッシュされたツール定義を使用
3. キャッシュが存在しなければ、`default-tools.json` (npm パッケージに同梱) を使用

**同期すべきタイミング:**
- Unity で MCP ツールを追加または変更した後
- uLoopMCP のバージョンを更新した後
- CLI コマンドが Unity のツールと一致しない場合

## 命名規則

| アイテム | 規則 | 例 |
|------|------------|---------|
| フォルダ | PascalCase | `YourNewTool/` |
| スキーマクラス | PascalCase + Schema | `YourNewToolSchema` |
| レスポンスクラス | PascalCase + Response | `YourNewToolResponse` |
| ツールクラス | PascalCase + Tool | `YourNewToolTool` |
| ToolName プロパティ | kebab-case | `"your-new-tool"` |
| SKILL.md name フィールド | uloop- プレフィックス + kebab-case | `uloop-your-new-tool` |

## ヒント

- **EditorWindow での先行テスト**: 複雑なツールの場合は、MCP ツールの実装前に `Assets/Editor/` にテスト用の EditorWindow を作成することを検討する。
- **async/await の適切な使用**: 遅延には `Task.Delay()` ではなく `TimerDelay.Wait()` を使用すること。
- **Unity エディタの状態への対応**: 再生中と非再生中の両方の状態を考慮すること。
- **リソースのクリーンアップ**: テクスチャ、レンダーテクスチャ、一時オブジェクトなどは必ずクリーンアップすること。

## 参考実装

- 単純なツール: `ClearConsole/`
- 列挙型パラメータを持つツール: `ControlPlayMode/`
- 非同期操作を伴う複雑なツール: `CaptureUnityWindow/`
- ファイル出力を行うツール: `UnitySearch/`
