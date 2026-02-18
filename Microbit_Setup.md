# Micro:bit Bluetooth Setup for Unity Control

To control your avatar with a micro:bit, you need to flash a specific program onto the micro:bit that sends accelerometer or button data via Bluetooth UART service.

## 1. Programming the Micro:bit (MakeCode)

1. Go to [MakeCode for micro:bit](https://makecode.microbit.org/).
2. Start a **New Project**.
3. Click on the **Extensions** gear icon (or "Advanced" -> "Extensions").
4. Search for `bluetooth` and add the **bluetooth** extension.
    * *Note: This will likely disable Radio functionality, which is fine.*
5. Click on **Project Settings** (gear icon) -> **Project Settings**.
6. Ensure **No Pairing Required: Anyone can connect via Bluetooth** is selected. This is crucial for easy connection from Windows/Unity without complex pairing.

## 2. Block Code Example

Create the following logic in the Block Editor:

**On Start:**

* `bluetooth uart service` (starts the UART service)
* `show icon (Heart)` (to indicate it's running)

**Forever:**

* `if button A is pressed`:
  * `bluetooth uart write string "L"` (Left)
* `else if button B is pressed`:
  * `bluetooth uart write string "R"` (Right)
* `else`:
  * `bluetooth uart write string "S"` (Stop)
* `pause (ms) 100` (to avoid flooding)

*Alternatively, using Accelerometer:*

* `if input rotation (roll) < -20`:
  * `bluetooth uart write string "L"`
* `else if input rotation (roll) > 20`:
  * `bluetooth uart write string "R"`
* `else`:
  * `bluetooth uart write string "S"`

## 3. JavaScript Code Example

You can switch to the **JavaScript** tab and paste this code:

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

## 4. Flashing

1. Connect your micro:bit via USB.
2. Click **Download**.
3. Copy the `.hex` file to the MICROBIT drive.
4. Wait for the compilation to finish.

## 5. Pairing with Windows

1. On your Windows PC, go to **Settings > Bluetooth & devices**.
2. Click **Add device** > **Bluetooth**.
3. Power on your micro:bit.
4. Select **BBC micro:bit [xxxxx]** when it appears.
5. If asked for a PIN, it might display a pattern on the micro:bit LEDs, or just connect if you selected "No Pairing Required".
6. Once connected/paired in Windows, the Unity script can discover it.
