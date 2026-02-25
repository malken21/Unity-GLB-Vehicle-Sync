---
name: uloop-control-play-mode
description: "Unityエディタの再生モードを制御する。再生、停止、一時停止の操作、ゲーム挙動のテスト、またはユーザーからの再生/停止要求時に使用する。"
---

# uloop control-play-mode

Unityエディタの再生モード (再生/停止/一時停止) を制御する。

## 使用方法

```bash
uloop control-play-mode [options]
```

## パラメータ

| パラメータ | 型 | デフォルト | 説明 |
|-----------|------|---------|-------------|
| `--action` | string | `Play` | 実行するアクション: `Play` (再生), `Stop` (停止), `Pause` (一時停止) |

## グローバルオプション

| オプション | 説明 |
|--------|-------------|
| `--project-path <path>` | 特定のUnityプロジェクトを対象とする（ `--port` とは排他的）。パスの解決は `cd` と同じルールに従う。絶対パスはそのまま使用され、相対パスは現在の作業ディレクトリから解決される。 |
| `-p, --port <port>` | UnityのTCPポートを直接指定する（ `--project-path` とは排他的）。 |

## 例

```bash
# 再生モードを開始
uloop control-play-mode --action Play

# 再生モードを停止
uloop control-play-mode --action Stop

# 再生モードを一時停止
uloop control-play-mode --action Pause
```

## 出力

現在の再生モードの状態を含む JSON を返す:
- `IsPlaying`: Unityが現在再生モードかどうか
- `IsPaused`: 再生モードが一時停止中かどうか
- `Message`: 実行されたアクションの説明

## 注意事項

- Play アクションは、Unityエディタでゲームを開始する（一時停止からの再開も含む）
- Stop アクションは、再生モードを終了してエディタモードに戻る
- Pause アクションは、再生モードを維持したままゲームを一時停止する
- 自動テストのワークフローに有用である
