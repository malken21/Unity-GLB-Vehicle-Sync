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

    private void Start()
    {
        rotator = GetComponent<KeyboardRotator>();
        
        if (MicrobitBLEManager.Instance != null)
        {
            // BLE マネージャーからのデータ受信イベントを購読
            MicrobitBLEManager.Instance.OnDataReceived += HandleDataReceived;
            Debug.Log("[MicrobitController] BLE マネージャーのイベントを購読しました");
        }
        else
        {
            Debug.LogWarning("[MicrobitController] MicrobitBLEManager のインスタンスが見つかりません。");
        }
    }

    public override void OnDestroy()
    {
        if (MicrobitBLEManager.Instance != null)
        {
            // イベントの購読を解除
            MicrobitBLEManager.Instance.OnDataReceived -= HandleDataReceived;
        }
        base.OnDestroy();
    }

    /// <summary>
    /// BLE から受信した文字列を処理します。
    /// </summary>
    private void HandleDataReceived(string data)
    {
        // 文字列のトリミング（改行コードの削除など）と大文字化
        currentCommand = data.Trim().ToUpper();
    }

    private void FixedUpdate()
    {
        // ネットワーク上の所有者（Owner）でない場合は処理しない
        if (!IsOwner) return;
        
        // KeyboardRotator が存在することを確認
        if (rotator == null) return;
        
        // Rigidbody の取得
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        float rotate = 0f;
        
        // コマンド判定ロジック
        // "L" -> 左回転
        // "R" -> 右回転
        // "F" -> 前進 (拡張用)
        // "B" -> 後退 (拡張用)
        
        if (currentCommand == "L")
        {
             rotate = -1f;
        }
        else if (currentCommand == "R")
        {
             rotate = 1f;
        }

        // 回転速度。必要に応じて SerializedField に変更可能
        float rotationSpeed = 100f;
        
        // KeyboardRotator と同様に Transform.Rotate を使用して回転を適用
        if (rotate != 0f)
        {
             transform.Rotate(Vector3.up, rotate * rotationSpeed * Time.fixedDeltaTime);
        }
    }
}
