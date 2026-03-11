using Unity.Netcode;
using UnityEngine;

public class AvatarVisibilityController : NetworkBehaviour
{
    // アバターの見た目を構成するレンダラーを保持
    private Renderer[] renderers;

    // クライアント全体で共有する「他人のアバターを表示するか」の状態（初期値は非表示）
    private static bool showOtherAvatars = false;

    public override void OnNetworkSpawn()
    {
        // 自身と子オブジェクトに含まれるすべてのRendererを取得
        renderers = GetComponentsInChildren<Renderer>();

        // 自分の管理下（ローカルプレイヤー）でなければ、現在の表示設定を適用して隠す
        if (!IsOwner)
        {
            SetVisibility(showOtherAvatars);
        }
    }

    private void Update()
    {
        // Hキーの入力判定は、自分のアバター（ローカルプレイヤー）のみで行う
        if (IsOwner)
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                ToggleOtherAvatarsVisibility();
                Debug.Log($"[AvatarVisibilityController] Toggle other players visibility: {showOtherAvatars}");
            }
        }
    }

    private void ToggleOtherAvatarsVisibility()
    {
        // 状態を反転
        showOtherAvatars = !showOtherAvatars;

        // シーン内に存在するすべてのアバターを取得
        // ※Unity 2023.1以降は FindObjectsByType を使用。古いバージョンの場合は FindObjectsOfType に変更してください。
        AvatarVisibilityController[] allAvatars = FindObjectsByType<AvatarVisibilityController>(FindObjectsSortMode.None);

        foreach (var avatar in allAvatars)
        {
            // 自分以外のアバターの表示状態を一斉に更新
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