using UnityEngine;
using UnityEngine.InputSystem;

public class KeyboardRotator : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("操作対象のRigidbody。W/Sキーのトルク回転に必要。未設定の場合は自身から取得を試みます。")]
    [SerializeField] private Rigidbody targetRigidbody;

    [Tooltip("操作対象のTransform。A/Dキーの回転に必要。未設定の場合は自身のTransformを使用します。")]
    [SerializeField] private Transform targetTransform;

    [Tooltip("W/Sキー入力時のトルク回転の強さ")]
    [SerializeField] private float torqueStrength = 10f;

    [Tooltip("A/Dキー入力時の直接回転の速度（度/秒）")]
    [SerializeField] private float rotationSpeed = 30f;

    [Tooltip("W/Sキー入力時の回転軸（ローカル座標系）。デフォルトはX軸（前転・後転）。")]
    [SerializeField] private Vector3 wsAxis = Vector3.forward;

    [Tooltip("A/Dキー入力時の回転軸（ローカル座標系）。デフォルトはY軸（右旋回・左旋回）。横転させたい場合は(0, 0, -1)などに設定してください。")]
    [SerializeField] private Vector3 adAxis = Vector3.up;

    [Header("Damping Settings")]
    [Tooltip("入力をやめた際に回転を止める抵抗力。値が大きいほど早く止まります。")]
    [SerializeField] private float dampingFactor = 5.0f;
    
    [Header("Jump Settings")]
    [Tooltip("ジャンプの強さ")]
    [SerializeField] private float jumpForce = 5f;

    [Tooltip("接地判定の距離")]
    [SerializeField] private float groundCheckDistance = 1.0f;

    [Tooltip("接地判定の開始オフセット(上方向)")]
    [SerializeField] private float groundCheckOffset = 0.7f;

    [Tooltip("地面とみなすレイヤー")]
    [SerializeField] private LayerMask groundLayer = -1;

    private void Start()
    {
        if (targetRigidbody == null)
        {
            targetRigidbody = GetComponent<Rigidbody>();
        }

        if (targetTransform == null)
        {
            targetTransform = transform;
        }

        // Rigidbody自体の標準の減衰を少し設定しておく(物理的な安定性のため)
        if (targetRigidbody != null && targetRigidbody.angularDamping == 0)
        {
            targetRigidbody.angularDamping = 0.5f;
        }
    }

    private void Update()
    {
        // キーボードが接続されていない場合は処理しない
        if (Keyboard.current == null) return;

        // ジャンプ入力 (New Input System / Frame-based check)
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Jump();
        }
    }

    private void FixedUpdate()
    {
        // キーボードが接続されていない場合は処理しない
        if (Keyboard.current == null) return;

        // 入力を取得 (New Input System)
        float inputWS = 0f;
        if (Keyboard.current.wKey.isPressed) inputWS += 1f;
        if (Keyboard.current.sKey.isPressed) inputWS -= 1f;

        float inputAD = 0f;
        if (Keyboard.current.dKey.isPressed) inputAD += 1f;
        if (Keyboard.current.aKey.isPressed) inputAD -= 1f;

        // W/Sキー: トルクによる回転
        if (targetRigidbody != null)
        {
            if (inputWS != 0f)
            {
                // 入力がある場合はトルクを加える
                var torque = targetTransform.TransformDirection(wsAxis) * inputWS * torqueStrength;
                targetRigidbody.AddTorque(torque, ForceMode.Force);
            }
            else
            {
                // 入力がない場合は、現在の角速度に対して逆向きの抵抗トルクを加える (Damping)
                // 積分バグ(行き過ぎた後戻ってくる不自然な挙動)を解消するため、単純な比例抵抗のみとする
                Vector3 currentAngularVelocity = targetRigidbody.angularVelocity;
                Vector3 resistanceTorque = -currentAngularVelocity * dampingFactor;
                targetRigidbody.AddTorque(resistanceTorque, ForceMode.Force);
            }
        }

        // A/Dキー: 直接回転（Transform.Rotate）
        // Rigidbodyがなくても動作する
        if (inputAD != 0f && targetTransform != null)
        {
            float rotateAmount = inputAD * rotationSpeed * Time.fixedDeltaTime;
            targetTransform.Rotate(adAxis, rotateAmount);
        }
    }

    /// <summary>
    /// 接地している場合にジャンプを実行します。外部からも呼び出し可能です。
    /// </summary>
    /// <param name="strengthScale">ジャンプ強度の倍率 (0.0 - 1.0)</param>
    public void Jump(float strengthScale = 1.0f)
    {
        if (targetRigidbody == null) return;

        if (IsGrounded())
        {
            targetRigidbody.AddForce(Vector3.up * jumpForce * strengthScale, ForceMode.Impulse);
            Debug.Log($"[KeyboardRotator] Jump triggered! (scale: {strengthScale:F2})");
        }
    }

    private bool IsGrounded()
    {
        if (targetTransform == null) return false;

        // アバターの中心より少し上から下に向けてレイを飛ばす
        Vector3 origin = targetTransform.position + Vector3.up * groundCheckOffset;
        // 自身のレイヤーを除外
        int layerMask = groundLayer.value & ~(1 << gameObject.layer);
        bool grounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance + groundCheckOffset, layerMask);
        
        // シーンビューでのデバッグ表示
        Debug.DrawRay(origin, Vector3.down * (groundCheckDistance + groundCheckOffset), grounded ? Color.green : Color.red);
        
        return grounded;
    }
}
