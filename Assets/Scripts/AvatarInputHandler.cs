using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

/// <summary>
/// 矢印キーなどによるアバターの手動変形（スケール、Y軸回転）を処理するクラス。
/// Avatar.cs にあった入力処理ロジックを分離。
/// </summary>
public class AvatarInputHandler : NetworkBehaviour
{
    private Avatar _avatar;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _avatar = GetComponent<Avatar>();
    }

    private void Update()
    {
        if (!IsOwner || _avatar == null) return;

        // Vibilityトグル (Hキー)
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            Avatar.s_hideOtherPlayers = !Avatar.s_hideOtherPlayers;
            Debug.Log($"[AvatarInputHandler] Toggle other players visibility: {!Avatar.s_hideOtherPlayers}");
            
            // シーン上のすべてのアバターに反映
            foreach (var avatar in FindObjectsByType<Avatar>(FindObjectsSortMode.None))
            {
                avatar.UpdateLocalVisibility();
            }
        }

        // 矢印キーでのスケールや回転の操作
        HandleManualAdjustments();
    }

    private void HandleManualAdjustments()
    {
        if (Keyboard.current == null) return;

        bool changed = false;
        
        // Avatarスクリプトから現在の値を取得
        float currentScale = _avatar.CurrentScale;
        float currentRotationY = _avatar.CurrentRotationY;

        // スケール：上矢印（拡大+0.1）、下矢印（縮小-0.1）
        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            currentScale += 0.1f;
            changed = true;
        }
        if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            currentScale -= 0.1f;
            if (currentScale < 0.1f) currentScale = 0.1f;
            changed = true;
        }

        // 回転：左矢印（反時計回り22.5度）、右矢印（時計回り22.5度）
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            currentRotationY -= 22.5f;
            changed = true;
        }
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            currentRotationY += 22.5f;
            changed = true;
        }

        if (changed)
        {
            // 値の更新リクエストをAvatarコンポーネントに送る
            _avatar.RequestTransformUpdate(currentScale, currentRotationY);
        }
    }
}
