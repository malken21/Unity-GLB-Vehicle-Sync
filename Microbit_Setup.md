# Unity制御用 Micro:bit Bluetooth設定ガイド

micro:bitでアバターを操作するには、加速度センサやボタンのデータをBluetooth UARTサービス経由で送信する専用のプログラムをmicro:bitに書き込み、さらに通信を中継する専用WebSocketサーバー **MicroBridge** を実行する必要があります。

## 1. Micro:bitのプログラミング (MakeCode)

1. [micro:bit用 MakeCode](https://makecode.microbit.org/) にアクセスします。
2. **新しいプロジェクト** を作成します。
3. **拡張機能** ギアアイコン（または「高度なブロック」→「拡張機能」）をクリックします。
4. `bluetooth` を検索し、**bluetooth** 拡張機能を追加します。
    * *注意: これにより無線（Radio）機能が無効になりますが、問題ありません。*
5. ギアアイコンから **プロジェクトの設定** を開きます。
6. **ペアリングなしでも接続可能 (No Pairing Required)** にチェックを入れます。これは、複雑なペアリングなしでMicroBridgeから簡単に接続するために不可欠です。

## 2. データ送信仕様とJavaScriptコード

micro:bitから送信するUART通信のデータフォーマットは以下の形式とする。
`A(0 or 1),B(0 or 1),J(0 or 1),r(-180から180)`

「ジャンプ動き」が発生した場合にJを1として送信する。

**JavaScript** タブに切り替えて、以下のコードを貼り付ける：

```javascript
bluetooth.startUartService()
basic.showIcon(IconNames.Heart)

let jump = 0

// ジャンプ動きでジャンプ
input.onGesture(Gesture.Shake, function () {
    jump = 1
    basic.pause(100)
    jump = 0
})

basic.forever(function () {
    let a = input.buttonIsPressed(Button.A) ? 1 : 0
    let b = input.buttonIsPressed(Button.B) ? 1 : 0
    let r = input.rotation(Rotation.Roll)
    
    let str = "" + a + "," + b + "," + jump + "," + r + "\n"
    bluetooth.uartWriteString(str)
    
    basic.pause(100)
})
```

## 3. 書き込み (Flashing)

1. micro:bitをUSBで接続します。
2. **ダウンロード** をクリックします。
3. 生成された `.hex` ファイルを MICROBIT ドライブにコピーします。
4. 転送が完了するまで待ちます。

## 4. MicroBridgeの実行とUnityとの連携

Unityは現在、ネイティブのBluetooth接続ではなく、WebSocketを経由してmicro:bitと通信します。この中継を担うのが **MicroBridge** です。

1. Windows PCで、**設定 > Bluetooth とデバイス** からmicro:bitをペアリングします（「ペアリングなし」に設定した場合は不要なこともありますが、Windows環境ではOSレベルでの認識を安定させるためペアリングを推奨します）。
2. バックグラウンドで `microbridge` 実行可能ファイルを起動します（デフォルトでポート4000のWebSocketサーバーとして待機し、micro:bitへ自動接続します）。
3. Unityプロジェクト（またはビルド済みアプリ）を起動します。
4. Unity側で自動的に `ws://127.0.0.1:4000` へ接続し、micro:bitの操作を受信してアバターの動作等に反映します。
