using UnityEngine;
using Unity.Netcode;

/// <summary>
/// MicrobitBLEManager からのデータを受け取り、アバターの回転を制御するクラス。
/// KeyboardRotator のロジックを再利用または複製して動作します。
/// </summary>
[RequireComponent(typeof(KeyboardRotator))]
public class MicrobitAvatarController : NetworkBehaviour
{
    private KeyboardRotator rotator;
    
    // パース用バッファ
    private string receiveBuffer = "";

    // 最新の入力データ
    private int inputA = 0;
    private int inputB = 0;
    private int inputJ = 0;
    private float inputR = 0f;

    // ジャンプトリガー
    private bool triggerJump = false;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float groundCheckDistance = 1.1f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (MicrobitBLEManager.Instance != null)
        {
            MicrobitBLEManager.Instance.OnDataReceived += HandleDataReceived;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (MicrobitBLEManager.Instance != null)
        {
            MicrobitBLEManager.Instance.OnDataReceived -= HandleDataReceived;
        }
        base.OnNetworkDespawn();
    }

    /// <summary>
    /// BLE から受信した文字列を処理します。
    /// 複数パケットに分割された場合を考慮してバッファリングし、改行で分割します。
    /// </summary>
    private void HandleDataReceived(string data)
    {
        if (!IsOwner) return;

        receiveBuffer += data;
        
        int newLineIdx;
        while ((newLineIdx = receiveBuffer.IndexOf('\n')) >= 0)
        {
            string line = receiveBuffer.Substring(0, newLineIdx).Trim();
            receiveBuffer = receiveBuffer.Substring(newLineIdx + 1);

            if (!string.IsNullOrEmpty(line))
            {
                ProcessCommand(line);
            }
        }
    }

    // 古いデバッグコマンドやC:によるカラー変更も無視せず捌くか、単純にカンマ区切りかで判定
    private void ProcessCommand(string command)
    {
        if (command.StartsWith("C:")) return; // 色変更コマンドはここでは無視またはAvatarColorMicrobitInputで処理させる

        string[] parts = command.Split(',');
        if (parts.Length == 4)
        {
            int.TryParse(parts[0], out inputA);
            int.TryParse(parts[1], out inputB);
            int.TryParse(parts[2], out inputJ);
            float.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out inputR);

            // Jが1のときにジャンプトリガーをオン
            if (inputJ == 1)
            {
                triggerJump = true;
            }
        }
        else
        {
            // 旧フォーマット (L, R, S)
            if (command == "L") inputR = -45f;
            else if (command == "R") inputR = 45f;
            else if (command == "S") inputR = 0f;
        }
    }

    private void FixedUpdate()
    {
        // ネットワーク上の所有者（Owner）でない場合は処理しない
        if (!IsOwner) return;
        
        // Rigidbody の取得
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        // r(-180から180) のロール値を元に、左右の回転を適用
        // 傾き(r)が一定以上のときに回転させるか、傾きに比例した速度で回転させる
        // 例: -20以下なら左、20以上なら右回転とする
        float rotate = 0f;
        if (inputR < -15f)
        {
            rotate = -1f; // 左回転
        }
        else if (inputR > 15f)
        {
            rotate = 1f;  // 右回転
        }

        // AボタンやBボタンでの旋回もサポートする場合
        if (inputA == 1) rotate = -1f;
        if (inputB == 1) rotate = 1f;

        if (rotate != 0f)
        {
            transform.Rotate(Vector3.up, rotate * rotationSpeed * Time.fixedDeltaTime);
        }

        // ジャンプ処理
        if (triggerJump)
        {
            if (IsGrounded())
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
            triggerJump = false;
        }
    }

    private bool IsGrounded()
    {
        // 簡易的な接地判定（Avatarの原点から下方向へRayを飛ばす）
        // Radiusが1のSphereColliderを想定し、少し余裕を持たせた距離で判定
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance);
    }
}
