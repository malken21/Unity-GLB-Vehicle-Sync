using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class AvatarVisibilityController : NetworkBehaviour
{
    private Renderer[] renderers;
    private static bool showOtherAvatars = false;

    public override void OnNetworkSpawn()
    {
        renderers = GetComponentsInChildren<Renderer>();

        if (!IsOwner)
        {
            SetVisibility(showOtherAvatars);
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            // キーボードが接続されており、かつ Hキー がこのフレームで押されたか判定
            if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
            {
                ToggleOtherAvatarsVisibility();
                Debug.Log($"[AvatarVisibilityController] Toggle other players visibility: {showOtherAvatars}");
            }
        }
    }

    private void ToggleOtherAvatarsVisibility()
    {
        showOtherAvatars = !showOtherAvatars;

        AvatarVisibilityController[] allAvatars = FindObjectsByType<AvatarVisibilityController>(FindObjectsSortMode.None);

        foreach (var avatar in allAvatars)
        {
            if (!avatar.IsOwner)
            {
                avatar.SetVisibility(showOtherAvatars);
            }
        }
    }

    private void SetVisibility(bool isVisible)
    {
        if (renderers == null) return;

        foreach (var r in renderers)
        {
            if (r != null)
            {
                r.enabled = isVisible;
            }
        }
    }
}