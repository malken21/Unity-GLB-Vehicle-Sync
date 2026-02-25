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

    // ボールの色を同期するためのNetworkVariable
    private NetworkVariable<Color> ballColor = new NetworkVariable<Color>(
        Color.white,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

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
    /// 対象となる ExerciseBall の Renderer を検索します。
    /// </summary>
    private void FindTargetRenderer()
    {
        // 階層を問わず子オブジェクトから Renderer を検索
        targetBallRenderer = GetComponentInChildren<Renderer>();
        
        if (targetBallRenderer != null)
        {
            Debug.Log($"[MicrobitController] 対象の Renderer を検出しました: {targetBallRenderer.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("[MicrobitController] 対象の Renderer が見つかりませんでした。");
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        ballColor.OnValueChanged += OnBallColorChanged;
        
        // Rendererが未設定の場合は再検索
        if (targetBallRenderer == null)
        {
            FindTargetRenderer();
        }
        
        ApplyColorToRenderer(ballColor.Value);
    }

    public override void OnNetworkDespawn()
    {
        ballColor.OnValueChanged -= OnBallColorChanged;
        base.OnNetworkDespawn();
    }

    private void OnBallColorChanged(Color previousValue, Color newValue)
    {
        ApplyColorToRenderer(newValue);
    }

    private void ApplyColorToRenderer(Color color)
    {
        if (targetBallRenderer != null)
        {
            // マテリアルのインスタンス化を避けるため sharedMaterial を使用するか検討が必要だが、
            // 個体ごとに色を変えるため material プロパティ（インスタンス化を伴う）を使用
            targetBallRenderer.material.color = color;
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
        if (!IsOwner) return;

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
            if (IsOwner)
            {
                ballColor.Value = newColor;
                Debug.Log($"[MicrobitController] ExerciseBall の色を {newColor} に変更するようネットワークに送信しました");
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

    private void Update()
    {
        // ネットワーク上の所有者（Owner）でない場合は処理しない
        if (!IsOwner) return;

        HandleKeyboardColorInput();
    }

    /// <summary>
    /// キーボードの 1-0 キー入力を監視し、色を変更します。
    /// 1 = 紫 (Hue 0.8), 0 = 赤 (Hue 0.0)
    /// </summary>
    private void HandleKeyboardColorInput()
    {
        if (UnityEngine.InputSystem.Keyboard.current == null) return;

        int keyPressedIndex = -1; // 0-9
        var kb = UnityEngine.InputSystem.Keyboard.current;

        if (kb.digit1Key.wasPressedThisFrame) keyPressedIndex = 0;
        else if (kb.digit2Key.wasPressedThisFrame) keyPressedIndex = 1;
        else if (kb.digit3Key.wasPressedThisFrame) keyPressedIndex = 2;
        else if (kb.digit4Key.wasPressedThisFrame) keyPressedIndex = 3;
        else if (kb.digit5Key.wasPressedThisFrame) keyPressedIndex = 4;
        else if (kb.digit6Key.wasPressedThisFrame) keyPressedIndex = 5;
        else if (kb.digit7Key.wasPressedThisFrame) keyPressedIndex = 6;
        else if (kb.digit8Key.wasPressedThisFrame) keyPressedIndex = 7;
        else if (kb.digit9Key.wasPressedThisFrame) keyPressedIndex = 8;
        else if (kb.digit0Key.wasPressedThisFrame) keyPressedIndex = 9;

        if (keyPressedIndex != -1)
        {
            // インデックス 0(1キー) = 0.0, インデックス 9(0キー) = 1.0 に正規化
            float t = keyPressedIndex / 9f;
            
            // 紫(0.8) から 赤(0.0) へ線形補完
            float hue = Mathf.Lerp(0.8f, 0.0f, t);
            Color newColor = Color.HSVToRGB(hue, 1f, 1f);
            
            ballColor.Value = newColor;
            Debug.Log($"[MicrobitController] Keyboard {((keyPressedIndex + 1) % 10)} pressed. Changing color to Hue {hue}");
        }
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
