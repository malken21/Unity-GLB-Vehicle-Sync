using UnityEngine;
using Unity.Netcode;

public class KeepHoriz : NetworkBehaviour
{
    private readonly NetworkVariable<float> currentYaw = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            // 初期状態でのY軸の回転角度（Yaw）を取得して保存
            currentYaw.Value = transform.eulerAngles.y;
        }
    }

    void LateUpdate()
    {
        // ワールド空間でY軸の回転（ヨー）のみを適用し、水平（X, Z軸は0）を保ちます
        transform.rotation = Quaternion.Euler(0f, currentYaw.Value, 0f);
    }

    public void AddYaw(float angleDelta)
    {
        if (!IsOwner) return;

        // 内部のYaw角度を更新
        float newYaw = currentYaw.Value + angleDelta;
        
        // 角度が大きくなりすぎないように正規化 (任意ですが、安全のため)
        newYaw %= 360f;
        if (newYaw < 0f) newYaw += 360f;

        currentYaw.Value = newYaw;
    }
}
