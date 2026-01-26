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

    [Header("Resistance Settings")]
    [Tooltip("回転速度に比例する抵抗係数。値が大きいほど高速回転時の抵抗が強くなります。")]
    [SerializeField] private float resistanceCoefficient = 1.5f;

    [Tooltip("最小抵抗値。回転している限り常に発生する抵抗のベース値です。")]
    [SerializeField] private float minResistance = 0.05f;

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

        // 回転抵抗の適用 (回転速度が速いほど抵抗が大きくなる)
        if (targetRigidbody != null)
        {
            Vector3 angularVel = targetRigidbody.angularVelocity;
            float sqrMag = angularVel.sqrMagnitude;
            
            // 完全に停止していない場合のみ抵抗をかける
            if (sqrMag > 0.0001f)
            {
                float speed = Mathf.Sqrt(sqrMag);
                // 抵抗の強さ = 最小抵抗 + (速度 * 係数)
                float resistanceMagnitude = minResistance + (speed * resistanceCoefficient);
                
                // 回転方向の逆向きにトルクをかける
                // angularVelはワールド座標系
                Vector3 resistanceTorque = -angularVel.normalized * resistanceMagnitude;
                
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
}
