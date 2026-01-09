using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
    [Header("Development Settings")]
    public bool autoStartClientInEditor = true;

    void Start()
    {
        // サーバービルド (Headless Mode) かどうか判定
        if (Application.isBatchMode)
        {
            Debug.Log("[Boot] Starting as Dedicated Server...");
            NetworkManager.Singleton.StartServer();
        }
        else
        {
            // エディタまたは通常のPC向けビルド
            if (autoStartClientInEditor)
            {
                Debug.Log("[Boot] Starting as Client...");
                NetworkManager.Singleton.StartClient();
            }
            else
            {
                Debug.Log("[Boot] Starting as Host...");
                NetworkManager.Singleton.StartHost();
            }
        }
    }
}
