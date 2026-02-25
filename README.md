# Unity GLB Vehicle Sync

動的に読み込まれたGLB (glTFバイナリ) の 3Dモデルをリアルタイムで同期することを実証するUnityプロジェクトです。
**Unity 6** と **Netcode for GameObjects** を使用して構築されています。

## 機能

- **ダイナミックGLBローディング**: 実行時にローカルディスクから `.glb` ファイルを読み込みます。
- **マルチプレイヤー同期**: 読み込まれたアバター/車両を接続されているすべてのクライアント間で自動的に同期します。
- **アセットサーバー統合**: ファイルを外部サーバーにアップロードし、URLをクライアントに配布します。
- **自動接続**:
  - **Dedicated Server**: バッチモードで自動的に起動します。
  - **Client/Host**: `ConnectionManager` スクリプトで設定可能です。
- **CI/CD**: WindowsおよびWindows Serverビルド用の自動化されたGitHub Actionsワークフロー。
- **Micro:bit コントロール**: WindowsのBluetooth機能を使用して、BBC Micro:bitでアバターを操作できます。
  - 詳細は [Microbit_Setup.md](Microbit_Setup.md) を参照してください。

## 要件

- **Unity**: 6000.3.9f1
- **アセットサーバー**: ファイルのアップロードと提供を処理するための互換性のあるHTTPサーバーが必要です。
  - デフォルトURL: `http://localhost:3000`
  - *[Simple-Rust-Asset-Server](https://github.com/malken21/Simple-Rust-Asset-Server) と互換性があります*

## 始め方

1. Unity 6000.3.9f1 でプロジェクトを開きます。
2. `Assets/Scenes/Client.unity`（またはメインシーン）を開きます。
3. アセットサーバーが `http://localhost:3000` で実行されていることを確認します。
4. Play Mode（再生モード）に入ります。
    - **Host/Client**: シーン上の `ConnectionManager` スクリプトによって制御されます。
5. 任意の場所をクリック（または実装されている場合はUIプロンプトに従って）して、`.glb` ファイルを選択します。
6. ファイルがアップロードされ、3Dモデルが接続されているすべてのクライアントに表示されます。

## 起動オプション (コマンドライン引数)

ビルドした実行ファイルは、コマンドライン引数を使用して起動モードや接続先を設定できます。

| 引数 | 値 | 説明 |
| :--- | :--- | :--- |
| `-mode` | `HOST` または `CLIENT` | 起動モードを指定します。 |
| `-port` | 数値 (例: `7777`) | 使用するポート番号。 |
| `-assetUrl` | URL文字列 | **Hostモード用**。アセットサーバーのベースURL。 |
| `-serverIp` | IPアドレス | **Clientモード用**。接続先ゲームサーバーのIPアドレス。 |
| `-summonAvatar` | `true` または `false` | `true`: アバター読み込み有効 (デフォルト)。`false`: アバター無効化＆上空視点モード (観戦者モード)。 |

### 使用例

**ホストとして起動 (PowerShell):**

```powershell
./Unity-GLB-Vehicle-Sync.exe -mode HOST -port 7777 -assetUrl "http://assets.example.com"
```

**クライアントとして起動 (PowerShell):**

```powershell
./Unity-GLB-Vehicle-Sync.exe -mode CLIENT -port 7777 -serverIp "192.168.1.10"
```

## ビルド

### ローカルビルド

Unityの **Build Profiles** ウィンドウ (`File > Build Profiles`) を使用して以下をビルドします：

- **Windows**: 標準のクライアント/ホストビルド。
- **Windows Server**: ヘッドレスサーバービルド。

### GitHub Actions

`v*.*.*` のタグ付けを行うと、検証済みのリリースビルドが自動的に生成されます。

- アーティファクト: `Windows.zip`, `WindowsServer.zip`
