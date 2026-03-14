using UnityEngine;

public class KeepHoriz : MonoBehaviour
{


    private float currentYaw = 0f;

    void Start()
    {
        // 初期状態でのY軸の回転角度（Yaw）を取得して保存
        currentYaw = transform.eulerAngles.y;
    }

    void LateUpdate()
    {
        // ワールド空間でY軸の回転（ヨー）のみを適用し、水平（X, Z軸は0）を保ちます
        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
    }

    public void AddYaw(float angleDelta)
    {
        // 内部のYaw角度を更新
        currentYaw += angleDelta;
        
        // 角度が大きくなりすぎないように正規化 (任意ですが、安全のため)
        currentYaw %= 360f;
        if (currentYaw < 0f) currentYaw += 360f;
    }
}
