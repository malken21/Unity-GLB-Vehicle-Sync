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
    
    // 現在のコマンド (S = 停止)
    private string currentCommand = "S";

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
    /// </summary>
    private void HandleDataReceived(string data)
    {
        if (!IsOwner) return;

        data = data.Trim().ToUpper();
        
        if (!data.StartsWith("C:"))
        {
            // 回転等のコマンド用
            currentCommand = data;
        }
    }

    private void FixedUpdate()
    {
        // ネットワーク上の所有者（Owner）でない場合は処理しない
        if (!IsOwner) return;
        
        // Rigidbody の取得
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        float rotate = 0f;
        
        // コマンド判定ロジック
        if (currentCommand == "L")
        {
             rotate = -1f;
        }
        else if (currentCommand == "R")
        {
             rotate = 1f;
        }

        // 回転速度
        float rotationSpeed = 100f;
        
        // 回転を適用
        if (rotate != 0f)
        {
             transform.Rotate(Vector3.up, rotate * rotationSpeed * Time.fixedDeltaTime);
        }
    }
}
