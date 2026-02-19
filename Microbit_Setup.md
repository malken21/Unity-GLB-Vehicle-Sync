# Unity制御用 Micro:bit Bluetooth設定ガイド

micro:bitでアバターを操作するには、加速度センサやボタンのデータをBluetooth UARTサービス経由で送信する専用のプログラムをmicro:bitに書き込む必要があります。

## 1. Micro:bitのプログラミング (MakeCode)

1. [micro:bit用 MakeCode](https://makecode.microbit.org/) にアクセスします。
2. **新しいプロジェクト** を作成します。
3. **拡張機能** ギアアイコン（または「高度なブロック」→「拡張機能」）をクリックします。
4. `bluetooth` を検索し、**bluetooth** 拡張機能を追加します。
    * *注意: これにより無線（Radio）機能が無効になりますが、問題ありません。*
5. ギアアイコンから **プロジェクトの設定** を開きます。
6. **ペアリングなしでも接続可能 (No Pairing Required)** にチェックを入れます。これは、複雑なペアリングなしでWindows/Unityから簡単に接続するために不可欠です。

## 2. ブロックコードの例

ツールボックスから以下のロジックを作成します：

**最初だけ (On Start):**

* `Bluetooth UART サービスを開始する` (UARTサービスを開始)
* `アイコンを表示 (ハート)` (動作中であることを示す)

**ずっと (Forever):**

* `もし ボタンAが押されている なら`:
  * `Bluetooth UART 文字列を送信する "L"` (左回転)
* `または もし ボタンBが押されている なら`:
  * `Bluetooth UART 文字列を送信する "R"` (右回転)
* `そうでなければ`:
  * `Bluetooth UART 文字列を送信する "S"` (停止)
* `一時停止 (ミリ秒) 100` (データの過剰送信を防ぐため)

*加速度センサを使用する場合の代替例:*

* `もし 加速度 (ロール) < -20 なら`:
  * `Bluetooth UART 文字列を送信する "L"`
* `または もし 加速度 (ロール) > 20 なら`:
  * `Bluetooth UART 文字列を送信する "R"`
* `そうでなければ`:
  * `Bluetooth UART 文字列を送信する "S"`

## 3. JavaScript コードの例

**JavaScript** タブに切り替えて、以下のコードを貼り付けることもできます：

```javascript
bluetooth.startUartService()
basic.showIcon(IconNames.Heart)

basic.forever(function () {
    if (input.buttonIsPressed(Button.A)) {
        bluetooth.uartWriteString("L")
    } else if (input.buttonIsPressed(Button.B)) {
        bluetooth.uartWriteString("R")
    } else {
        bluetooth.uartWriteString("S")
    }
    basic.pause(100)
})
```

## 4. 書き込み (Flashing)

1. micro:bitをUSBで接続します。
2. **ダウンロード** をクリックします。
3. 生成された `.hex` ファイルを MICROBIT ドライブにコピーします。
4. 転送が完了するまで待ちます。

## 5. Windowsでのペアリング

1. Windows PCで、**設定 > Bluetooth とデバイス** を開きます。
2. **デバイスの追加 > Bluetooth** をクリックします。
3. micro:bitの電源を入れます。
4. リストに表示された **BBC micro:bit [xxxxx]** を選択します。
5. PIN（パスコード）を求められた場合、micro:bitのLEDに表示されるパターンを確認するか、「ペアリングなしを許可」に設定していればそのまま接続されます。
6. Windowsで接続/ペアリングが完了すると、Unityスクリプトがデバイスを検出できるようになります。
