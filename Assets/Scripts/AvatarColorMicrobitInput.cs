using UnityEngine;
using Unity.Netcode;

/// <summary>
/// MicrobitからのBLE入力を受け取り、AvatarColorControllerに色変更を指示するクラス。
/// </summary>
[RequireComponent(typeof(AvatarColorController))]
public class AvatarColorMicrobitInput : NetworkBehaviour
{
    private AvatarColorController colorController;

    private void Awake()
    {
        colorController = GetComponent<AvatarColorController>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsOwner && MicrobitBLEManager.Instance != null)
        {
            MicrobitBLEManager.Instance.OnDataReceived += HandleDataReceived;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && MicrobitBLEManager.Instance != null)
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
        
        if (data.StartsWith("C:"))
        {
            ApplyColorCommand(data);
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
                colorController.SetHue(hue);
                Debug.Log($"[AvatarColorMicrobitInput] Hue を {hue} に変更するようネットワークに送信しました");
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
}
