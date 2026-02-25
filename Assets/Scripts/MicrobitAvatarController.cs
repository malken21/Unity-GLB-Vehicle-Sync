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

    // ボールの色（Hue）を同期するためのNetworkVariable (0.0 - 1.0)
    private NetworkVariable<float> ballHue = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    // シェーダープロパティのID
    private static readonly int HuePropertyId = Shader.PropertyToID("_Hue");

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
        ballHue.OnValueChanged += OnBallHueChanged;
        
        // Rendererが未設定の場合は再検索
        if (targetBallRenderer == null)
        {
            FindTargetRenderer();
        }
        
        ApplyHueToRenderer(ballHue.Value);
    }

    public override void OnNetworkDespawn()
    {
        ballHue.OnValueChanged -= OnBallHueChanged;
        base.OnNetworkDespawn();
    }

    private void OnBallHueChanged(float previousValue, float newValue)
    {
        ApplyHueToRenderer(newValue);
    }

    private void ApplyHueToRenderer(float hue)
    {
        if (targetBallRenderer != null)
        {
            // カスタムシェーダーの _Hue プロパティを更新
            targetBallRenderer.material.SetFloat(HuePropertyId, hue);
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
    /// Hue値に変換して保持します。
    /// </summary>
    private void ApplyColorCommand(string colorCmd)
    {
        string colorData = colorCmd.Substring(2).Trim();
        
        if (TryParseMicrobitColorToHue(colorData, out float hue))
        {
            if (IsOwner)
            {
                ballHue.Value = hue;
                Debug.Log($"[MicrobitController] Hue を {hue} に変更するようネットワークに送信しました");
            }
        }
    }

    /// <summary>
    /// 文字列データを色相(Hue)情報に変換します。
    /// </summary>
    private bool TryParseMicrobitColorToHue(string colorData, out float hue)
    {
        hue = 0f;
        Color color = Color.white;
        bool success = false;

        // "R,G,B" フォーマットのチェック (例: "255,128,0")
        string[] rgbParts = colorData.Split(',');
        if (rgbParts.Length == 3)
        {
            if (byte.TryParse(rgbParts[0].Trim(), out byte r) && 
                byte.TryParse(rgbParts[1].Trim(), out byte g) && 
                byte.TryParse(rgbParts[2].Trim(), out byte b))
            {
                color = new Color32(r, g, b, 255);
                success = true;
            }
        }

        if (!success)
        {
            // カラー名のマッピング
            switch (colorData)
            {
                case "RED": color = Color.red; success = true; break;
                case "GREEN": color = Color.green; success = true; break;
                case "BLUE": color = Color.blue; success = true; break;
                case "YELLOW": color = Color.yellow; success = true; break;
                case "WHITE": color = Color.white; success = true; break;
                case "BLACK": color = Color.black; success = true; break;
                default:
                    // 16進数カラーコードのチェック
                    string htmlColor = colorData.StartsWith("#") ? colorData : "#" + colorData;
                    if (ColorUtility.TryParseHtmlString(htmlColor, out color))
                    {
                        success = true;
                    }
                    break;
            }
        }

        if (success)
        {
            Color.RGBToHSV(color, out hue, out _, out _);
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
    /// キーボードの 1-0 キー入力を監視し、Hue（0-1）を変更します。
    /// 1 = Hue 0.0 (赤), 0 = Hue 0.9 (または 1.0 に近い虹色の終端)
    /// ※ユーザーの「1が紫、0が赤」という前回の要望を尊重しつつ、虹色（赤から紫）をマッピングします。
    /// 1 = インテックス0, 0 = インデックス9
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
            // 0.0 ～ 1.0 の float 値を計算
            // 今回の「虹の色」要望に合わせ、0.0から1.0をリニアに割り当てます。
            float hue = keyPressedIndex / 9f;
            
            ballHue.Value = hue;
            Debug.Log($"[MicrobitController] Keyboard {((keyPressedIndex + 1) % 10)} pressed. Changing Hue to {hue}");
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
