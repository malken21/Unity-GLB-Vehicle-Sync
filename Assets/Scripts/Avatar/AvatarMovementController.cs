using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

/// <summary>
/// micro:bit と PCキーボードの両方からの入力を受け取り、アバターの移動（ジャンプ・回転）を制御するクラス。
/// </summary>
public class AvatarMovementController : NetworkBehaviour
{
    // 最新のマイクロビット入力データ
    private int mbitInputA = 0;
    private int mbitInputB = 0;
    private int mbitInputJ = 0;
    private float mbitInputR = 0f;

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

    private void HandleDataReceived(string data)
    {
        if (!IsOwner) return;

        // MicrobitBLEManager が既に行単位でデータを渡しているため、
        // ここでのバッファリングおよび行分割は不要。直接処理を行う。
        if (!string.IsNullOrEmpty(data))
        {
            ProcessMicrobitCommand(data.Trim());
        }
    }

    private void ProcessMicrobitCommand(string command)
    {
        if (command.StartsWith("C:")) return;

        string[] parts = command.Split(',');
        if (parts.Length == 4)
        {
            if (int.TryParse(parts[0], out mbitInputA) &&
                int.TryParse(parts[1], out mbitInputB) &&
                int.TryParse(parts[2], out mbitInputJ) &&
                float.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out mbitInputR))
            {
                // 解析成功のログを出力
                Debug.Log($"[AvatarMovementController] Processed command: A={mbitInputA}, B={mbitInputB}, J={mbitInputJ}, R={mbitInputR}");

                // マイクロビットでのジャンプ検知
                if (mbitInputJ == 1)
                {
                    triggerJump = true;
                }
            }
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        // キーボードでのジャンプ検知 (Spaceキー)
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            triggerJump = true;
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        // --- 回転処理の統合 ---
        float rotateDir = 0f;

        // 1. キーボード入力の取得
        if (Keyboard.current != null)
        {
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) rotateDir += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) rotateDir -= 1f;
        }

        // 2. マイクロビット入力の統合
        // A/Bボタン
        if (mbitInputA == 1) rotateDir -= 1f;
        if (mbitInputB == 1) rotateDir += 1f;
        
        // 傾き(R)による回転 (デッドゾーン設定)
        if (mbitInputR < -15f) rotateDir -= 1f;
        else if (mbitInputR > 15f) rotateDir += 1f;

        // 回転の適用
        if (rotateDir != 0f)
        {
            float clampedRotate = Mathf.Clamp(rotateDir, -1f, 1f);
            transform.Rotate(Vector3.up, clampedRotate * rotationSpeed * Time.fixedDeltaTime);
        }

        // --- ジャンプ処理 ---
        if (triggerJump)
        {
            if (IsGrounded())
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                Debug.Log("[AvatarMovementController] Jump triggered");
            }
            triggerJump = false;
        }
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance);
    }
}
