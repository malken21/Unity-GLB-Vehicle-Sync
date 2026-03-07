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

    // ログ出力抑制用の前回値保持
    private int prevInputA = 0;
    private int prevInputB = 0;
    private int prevInputJ = 0;
    private float prevInputR = 0f;

    // ジャンプトリガー
    private bool triggerJump = false;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float moveTorqueStrength = 10f;
    [SerializeField] private float groundCheckDistance = 1.1f;
    [SerializeField] private Transform horizTransform;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (MicrobitBLEManager.Instance != null)
        {
            MicrobitBLEManager.Instance.OnDataReceived += HandleDataReceived;
        }

        if (horizTransform == null)
        {
            horizTransform = transform.Find("Horiz");
            if (horizTransform == null)
            {
                Debug.LogWarning("[AvatarMovementController] 'Horiz' child not found. Using root forward.");
                horizTransform = transform;
            }
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

        // --- キー・値形式の解析 (例: "r:-178,j:0" or "r:-178") ---
        // 効率化のため、コロンを含まない場合は処理をスキップ
        if (!command.Contains(":")) return;

        string[] parts = command.Split(',');
        foreach (var part in parts)
        {
            string[] kv = part.Trim().Split(':');
            if (kv.Length == 2)
            {
                string key = kv[0].ToLower().Trim();
                string val = kv[1].Trim();

                if (key == "r") 
                {
                    if (float.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float r))
                    {
                        mbitInputR = r;
                    }
                }
                else if (key == "j")
                {
                    if (int.TryParse(val, out int j))
                    {
                        mbitInputJ = j;
                        if (mbitInputJ == 1) triggerJump = true;
                    }
                }
                else if (key == "a") int.TryParse(val, out mbitInputA);
                else if (key == "b") int.TryParse(val, out mbitInputB);
            }
        }

        // 値に変化があった場合のみログ出力
        if (mbitInputA != prevInputA || mbitInputB != prevInputB || mbitInputJ != prevInputJ || !Mathf.Approximately(mbitInputR, prevInputR))
        {
            Debug.Log($"[AvatarMovementController] Processed KV: A={mbitInputA}, B={mbitInputB}, J={mbitInputJ}, R={mbitInputR}");
            prevInputA = mbitInputA;
            prevInputB = mbitInputB;
            prevInputJ = mbitInputJ;
            prevInputR = mbitInputR;
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
        // 傾き(R)による回転 (デッドゾーン設定)
        if (mbitInputR < -15f) rotateDir -= 1f;
        else if (mbitInputR > 15f) rotateDir += 1f;

        // 3. 移動入力の取得 (A/Bボタン)
        float moveDir = 0f;
        if (mbitInputA == 1) moveDir += 1f;
        if (mbitInputB == 1) moveDir -= 1f;

        // 回転の適用
        if (rotateDir != 0f)
        {
            float clampedRotate = Mathf.Clamp(rotateDir, -1f, 1f);
            transform.Rotate(Vector3.up, clampedRotate * rotationSpeed * Time.fixedDeltaTime);
        }

        // 移動トルクの適用
        if (moveDir != 0f)
        {
            Vector3 forwardDir = horizTransform != null ? horizTransform.forward : transform.forward;
            Vector3 torque = forwardDir * moveDir * moveTorqueStrength;
            rb.AddTorque(torque, ForceMode.Force);
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
