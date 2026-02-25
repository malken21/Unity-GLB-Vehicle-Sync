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

    [Header("コンポーネント設定")]
    [SerializeField]
    [Tooltip("色を変更する対象のRenderer（ExerciseBall）。未指定の場合は子オブジェクトから自動検索します。")]
    private Renderer targetBallRenderer;

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

        if (targetBallRenderer == null)
        {
            FindTargetRenderer();
        }
    }

    /// <summary>
    /// 対象となる ExerciseBall の Renderer を子階層から検索します。
    /// </summary>
    private void FindTargetRenderer()
    {
        Transform ballTransform = transform.Find("Avatar/ExerciseBall");
        if (ballTransform == null)
        {
            ballTransform = transform.Find("ExerciseBall");
        }

        if (ballTransform != null)
        {
            targetBallRenderer = ballTransform.GetComponent<Renderer>();
        }
        
        if (targetBallRenderer == null)
        {
            Debug.LogWarning("[MicrobitController] 対象の ExerciseBall Renderer が見つかりませんでした。");
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
        data = data.Trim().ToUpper();
        
        if (data.StartsWith("C:"))
        {
            ApplyColorCommand(data);
        }
        else
        {
            // 回転等のコマンド用
            currentCommand = data;
        }
    }

    /// <summary>
    /// 受信したカラーコマンドをアバターの ExerciseBall に適用します。
    /// </summary>
    private void ApplyColorCommand(string colorCmd)
    {
        string colorData = colorCmd.Substring(2).Trim();
        
        if (TryParseMicrobitColor(colorData, out Color newColor))
        {
            if (targetBallRenderer != null && targetBallRenderer.material != null)
            {
                targetBallRenderer.material.color = newColor;
                Debug.Log($"[MicrobitController] ExerciseBall の色を {newColor} に変更しました");
            }
            else
            {
                Debug.LogWarning("[MicrobitController] 対象のRendererが設定されていないため色を変更できません");
            }
        }
    }

    /// <summary>
    /// 文字列データを色情報に変換します。
    /// </summary>
    private bool TryParseMicrobitColor(string colorData, out Color parsedColor)
    {
        parsedColor = Color.white;

        // "R,G,B" フォーマットのチェック (例: "255,128,0")
        string[] rgbParts = colorData.Split(',');
        if (rgbParts.Length == 3)
        {
            if (byte.TryParse(rgbParts[0].Trim(), out byte r) && 
                byte.TryParse(rgbParts[1].Trim(), out byte g) && 
                byte.TryParse(rgbParts[2].Trim(), out byte b))
            {
                parsedColor = new Color32(r, g, b, 255);
                return true;
            }
        }

        // カラー名のマッピング
        switch (colorData)
        {
            case "RED": parsedColor = Color.red; return true;
            case "GREEN": parsedColor = Color.green; return true;
            case "BLUE": parsedColor = Color.blue; return true;
            case "YELLOW": parsedColor = Color.yellow; return true;
            case "WHITE": parsedColor = Color.white; return true;
            case "BLACK": parsedColor = Color.black; return true;
        }

        // 16進数カラーコードのチェック
        string htmlColor = colorData.StartsWith("#") ? colorData : "#" + colorData;
        if (ColorUtility.TryParseHtmlString(htmlColor, out Color resultColor))
        {
            parsedColor = resultColor;
            return true;
        }

        return false;
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
