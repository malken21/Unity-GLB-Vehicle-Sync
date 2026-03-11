using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class AvatarVisibilityController : NetworkBehaviour
{
    private Renderer[] renderers;
    private static bool showOtherAvatars = false;
    private int lastRendererCount = -1;

    public override void OnNetworkSpawn()
    {
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
        else
        {
            Renderer[] currentRenderers = GetComponentsInChildren<Renderer>(true);
            if (currentRenderers.Length != lastRendererCount)
            {
                SetVisibility(showOtherAvatars);
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
        renderers = GetComponentsInChildren<Renderer>(true);
        lastRendererCount = renderers.Length;

        foreach (var r in renderers)
        {
            if (r != null)
            {
                r.enabled = isVisible;
            }
        }
    }
}