using UnityEngine;
using Unity.Netcode;

/// <summary>
/// キーボード入力を受け取り、AvatarColorControllerに色変更を指示するクラス。
/// </summary>
[RequireComponent(typeof(AvatarColorController))]
public class AvatarColorKeyboardInput : NetworkBehaviour
{
    private AvatarColorController colorController;

    private void Awake()
    {
        colorController = GetComponent<AvatarColorController>();
    }

    private void Update()
    {
        // ネットワーク上の所有者（Owner）でない場合は処理しない
        if (!IsOwner) return;

        HandleKeyboardColorInput();
    }

    /// <summary>
    /// キーボードの 1-0 キー入力を監視し、Hue（0-1）を変更します。
    /// </summary>
    private void HandleKeyboardColorInput()
    {
        if (UnityEngine.InputSystem.Keyboard.current == null) return;

        int keyPressedIndex = -1; // 0-9
        var kb = UnityEngine.InputSystem.Keyboard.current;

        if (kb.digit1Key.wasPressedThisFrame) keyPressedIndex = 0;
        else if (kb.digit2Key.wasPressedThisFrame) keyPressedIndex = 1;
        else if (kb.digit3Key.wasPressedThisFrame) keyPressedIndex = 2;
        else if (kb.digit4Key.wasPressedThisFrame) keyPressedIndex = 3;
        else if (kb.digit5Key.wasPressedThisFrame) keyPressedIndex = 4;
        else if (kb.digit6Key.wasPressedThisFrame) keyPressedIndex = 5;
        else if (kb.digit7Key.wasPressedThisFrame) keyPressedIndex = 6;
        else if (kb.digit8Key.wasPressedThisFrame) keyPressedIndex = 7;
        else if (kb.digit9Key.wasPressedThisFrame) keyPressedIndex = 8;
        else if (kb.digit0Key.wasPressedThisFrame) keyPressedIndex = 9;

        if (keyPressedIndex != -1)
        {
            float t = keyPressedIndex / 9f;
            float hue = Mathf.Lerp(0.8f, 0.0f, t);
            
            colorController.SetHue(hue);
            Debug.Log($"[AvatarColorKeyboardInput] Keyboard {((keyPressedIndex + 1) % 10)} pressed. Changing Hue to {hue}");
        }
    }
}
