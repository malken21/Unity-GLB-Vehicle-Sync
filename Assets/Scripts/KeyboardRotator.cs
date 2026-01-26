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
        if (Keyboard.current.wKey.isPressed) inputWS = 1f;  // 前方向
        else if (Keyboard.current.sKey.isPressed) inputWS = -1f; // 後ろ方向

        float inputAD = 0f;
        if (Keyboard.current.dKey.isPressed) inputAD = 1f;  // 右へ
        else if (Keyboard.current.aKey.isPressed) inputAD = -1f; // 左へ

        // W/Sキー: トルクによる回転
        if (inputWS != 0f && targetRigidbody != null)
        {
            var torque = targetTransform.TransformDirection(wsAxis) * inputWS * torqueStrength;
            targetRigidbody.AddTorque(torque, ForceMode.Force);
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
