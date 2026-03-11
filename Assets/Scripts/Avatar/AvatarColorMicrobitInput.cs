using UnityEngine;
using Unity.Netcode;

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

    private void HandleDataReceived(string data)
    {
        if (!IsOwner) return;

        data = data.Trim().ToUpper();
        
        if (data.StartsWith("C:"))
        {
            ApplyColorCommand(data);
        }
    }

    private void ApplyColorCommand(string colorCmd)
    {
        string colorData = colorCmd.Substring(2).Trim();
        
        if (TryParseMicrobitColorToHue(colorData, out float hue))
        {
            if (IsOwner)
            {
                colorController.SetHue(hue);
                Debug.Log($"[AvatarColorMicrobitInput] Sent color change request: {hue}");
            }
        }
    }

    private bool TryParseMicrobitColorToHue(string colorData, out float hue)
    {
        hue = 0f;
        Color color = Color.white;
        bool success = false;

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
            switch (colorData)
            {
                case "RED": color = Color.red; success = true; break;
                case "GREEN": color = Color.green; success = true; break;
                case "BLUE": color = Color.blue; success = true; break;
                case "YELLOW": color = Color.yellow; success = true; break;
                case "WHITE": color = Color.white; success = true; break;
                case "BLACK": color = Color.black; success = true; break;
                default:
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
