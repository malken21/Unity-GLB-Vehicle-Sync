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
    [SerializeField] private float rotationSpeed = 100f;

    [Tooltip("W/Sキー入力時の回転軸（ローカル座標系）。デフォルトはX軸（前転・後転）。")]
    [SerializeField] private Vector3 wsAxis = Vector3.forward;

    [Tooltip("A/Dキー入力時の回転軸（ローカル座標系）。デフォルトはY軸（右旋回・左旋回）。横転させたい場合は(0, 0, -1)などに設定してください。")]
    [SerializeField] private Vector3 adAxis = Vector3.up;

    [Header("Resistance Settings (PID)")]
    [Tooltip("Proportional Gain: 回転速度に対する抵抗力（P制御）。動きを止めようとする基本的な力。")]
    [SerializeField] private float pGain = 1.0f;

    [Tooltip("Integral Gain: 蓄積された回転に対する補正力（I制御）。定常的な回転を抑える力。")]
    [SerializeField] private float iGain = 0.0f;

    [Tooltip("Derivative Gain: 回転速度の変化に対する抵抗力（D制御）。急激な速度変化を抑制する力。")]
    [SerializeField] private float dGain = 0.1f;

    // PID制御用変数
    private Vector3 integralError;
    private Vector3 lastError;

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
    }


    private void FixedUpdate()
    {
        // キーボードが接続されていない場合は処理しない
        if (Keyboard.current == null) return;

        // 入力を取得 (New Input System)
        float inputWS = 0f;
        // 前方向
        if (Keyboard.current.wKey.isPressed) inputWS += 1f;
        // 後ろ方向
        if (Keyboard.current.sKey.isPressed) inputWS -= 1f;

        float inputAD = 0f;
        // 右へ
        if (Keyboard.current.dKey.isPressed) inputAD += 1f;
        // 左へ
        if (Keyboard.current.aKey.isPressed) inputAD -= 1f;

        // W/Sキー: トルクによる回転（目標角速度への追従）
        if (inputWS != 0f && targetRigidbody != null)
        {
            var torque = targetTransform.TransformDirection(wsAxis) * inputWS * torqueStrength;
            targetRigidbody.AddTorque(torque, ForceMode.Force);
        }

        // 回転抵抗の適用 (PID制御)
        if (targetRigidbody != null)
        {
            Vector3 currentAngularVelocity = targetRigidbody.angularVelocity;
            
            // 目標角速度は常にゼロ（停止状態）
            Vector3 targetAngularVelocity = Vector3.zero;
            
            // 誤差計算 (目標 - 現在)
            // 例: 右回転(正)している場合、誤差は負になり、左回転(負)のトルクが発生してブレーキとなる
            Vector3 error = targetAngularVelocity - currentAngularVelocity;
            
            // P項 (比例)
            Vector3 p = error * pGain;
            
            // I項 (積分)
            integralError += error * Time.fixedDeltaTime;
            Vector3 i = integralError * iGain;
            
            // D項 (微分)
            Vector3 d = (error - lastError) / Time.fixedDeltaTime;
            Vector3 derivative = d * dGain;
            
            // PID出力（抵抗トルク）
            Vector3 resistanceTorque = p + i + derivative;
            
            targetRigidbody.AddTorque(resistanceTorque, ForceMode.Force);
            
            // 次回用に誤差を保存
            lastError = error;
        }

        // A/Dキー: 直接回転（Transform.Rotate）
        // Rigidbodyがなくても動作する
        if (inputAD != 0f && targetTransform != null)
        {
            float rotateAmount = inputAD * rotationSpeed * Time.fixedDeltaTime;
            targetTransform.Rotate(adAxis, rotateAmount);
        }
    }
}
