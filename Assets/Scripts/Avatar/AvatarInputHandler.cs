using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

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

        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            Avatar.s_hideOtherPlayers = !Avatar.s_hideOtherPlayers;
            Debug.Log($"[AvatarInputHandler] Toggled other players' visibility. Hidden mode is now: {Avatar.s_hideOtherPlayers}");
            
            foreach (var avatar in FindObjectsByType<Avatar>(FindObjectsSortMode.None))
            {
                avatar.UpdateLocalVisibility();
            }
        }

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            _avatar.Respawn();
        }

        HandleManualAdjustments();
    }

    private void HandleManualAdjustments()
    {
        if (Keyboard.current == null) return;

        bool changed = false;
        
        float currentScale = _avatar.CurrentScale;
        float currentRotationY = _avatar.CurrentRotationY;

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
            _avatar.RequestTransformUpdate(currentScale, currentRotationY);
        }
    }
}
