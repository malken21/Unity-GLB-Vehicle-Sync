---
name: uloop-screenshot
description: "Unityエディタウィンドウのスクリーンショットを撮影し、PNG画像として保存する。使用目的: (1) Game、Scene、Console、Inspectorなどのウィンドウの撮影、(2) デバッグやドキュメント作成のための現在の視覚的状態のキャプチャ、(3) エディタの外観を画像ファイルとして保存。"
---

# uloop capture-window

Unityのエディタウィンドウを名前で指定してキャプチャし、PNGとして保存する。

## 使用方法

```bash
uloop capture-window [--window-name <name>] [--resolution-scale <scale>] [--match-mode <mode>]
```

## パラメータ

| パラメータ | 型 | デフォルト | 説明 |
|-----------|------|---------|-------------|
| `--window-name` | string | `Game` | キャプチャするウィンドウ名 (例: "Game", "Scene", "Console", "Inspector", "Project", "Hierarchy", その他 EditorWindow のタイトル) |
| `--resolution-scale` | number | `1.0` | 解像度のスケール (0.1 から 1.0) |
| `--match-mode` | enum | `exact` | ウィンドウ名の照合モード: `exact` (完全一致), `prefix` (前方一致), `contains` (含む)。全てのモードで大文字小文字は区別されない。 |

## 照合モード

| モード | 説明 | 例 |
|------|-------------|---------|
| `exact` | ウィンドウ名が完全に一致する必要がある | "Project" は "Project" のみに一致 |
| `prefix` | ウィンドウ名が入力値で始まる必要がある | "Project" は "Project" と "Project Settings" に一致 |
| `contains` | ウィンドウ名に入力値が含まれている必要がある | "set" は "Project Settings" に一致 |

## ウィンドウ名

ウィンドウ名は、ウィンドウのタイトルバー（タブ）に表示されているテキストである。一般的なウィンドウ名には以下のものがある:

- **Game**: Gameビューウィンドウ
- **Scene**: Sceneビューウィンドウ
- **Console**: コンソールウィンドウ
- **Inspector**: インスペクターウィンドウ
- **Project**: プロジェクトブラウザウィンドウ
- **Hierarchy**: ヒエラルキーウィンドウ
- **Animation**: アニメーションウィンドウ
- **Animator**: アニメーターウィンドウ
- **Profiler**: プロファイラーウィンドウ
- **Audio Mixer**: オーディオミキサーウィンドウ

カスタムの EditorWindow のタイトル（例: "EditorWindow Capture Test"）を指定することも可能である。

## グローバルオプション

| オプション | 説明 |
|--------|-------------|
| `--project-path <path>` | 特定のUnityプロジェクトを対象とする（ `--port` とは排他的）。パスの解決は `cd` と同じルールに従う。絶対パスはそのまま使用され、相対パスは現在の作業ディレクトリから解決される。 |
| `-p, --port <port>` | UnityのTCPポートを直接指定する（ `--project-path` とは排他的）。 |

## 例

```bash
# Gameビューをフル解像度でキャプチャ
uloop capture-window

# Gameビューを半分の解像度でキャプチャ
uloop capture-window --window-name Game --resolution-scale 0.5

# Sceneビューをキャプチャ
uloop capture-window --window-name Scene

# コンソールウィンドウをキャプチャ
uloop capture-window --window-name Console

# インスペクターウィンドウをキャプチャ
uloop capture-window --window-name Inspector

# プロジェクトブラウザをキャプチャ (完全一致 - "Project Settings" には一致しない)
uloop capture-window --window-name Project

# "Project" で始まるすべてのウィンドウをキャプチャ (前方一致)
uloop capture-window --window-name Project --match-mode prefix

# タイトルを指定してカスタム EditorWindow をキャプチャ
uloop capture-window --window-name "My Custom Window"
```

## 出力

以下の内容を含む JSON を返す:
- `CapturedCount`: キャプチャされたウィンドウ数
- `CapturedWindows`: キャプチャされたウィンドウ情報の配列。それぞれ以下を含む:
  - `ImagePath`: 保存された PNG ファイルへの絶対パス
  - `FileSizeBytes`: 保存されたファイルのサイズ (バイト)
  - `Width`: キャプチャされた画像の幅 (ピクセル)
  - `Height`: キャプチャされた画像の高さ (ピクセル)

複数のウィンドウが一致する場合（例: 複数の Inspector ウィンドウがある、または `contains` モードを使用している場合）、すべての一致するウィンドウが連番付きのファイル名（例: `Inspector_1_*.png`, `Inspector_2_*.png`）でキャプチャされる。

## 注意事項

- 必要に応じて、事前に `uloop focus-window` を実行すること
- 対象ウィンドウが Unity エディタで開かれている必要がある
- ウィンドウ名の照合では大文字小文字は区別されない
