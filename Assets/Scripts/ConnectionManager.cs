using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

using Unity.Collections;

public class ConnectionManager : NetworkBehaviour
{
    public static ConnectionManager Instance { get; private set; }

    [Header("Development Settings")]
    [SerializeField] private string initialServerUrl = "http://localhost:3000";
    public bool autoStartClientInEditor = true;

    private NetworkVariable<FixedString512Bytes> _serverUrl = new NetworkVariable<FixedString512Bytes>();

    public string serverUrl => _serverUrl.Value.IsEmpty ? initialServerUrl : _serverUrl.Value.ToString();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _serverUrl.Value = new FixedString512Bytes(initialServerUrl);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        // 環境変数を確認
        string envMode = System.Environment.GetEnvironmentVariable("VEHICLE_SYNC_MODE");
        
        if (!string.IsNullOrEmpty(envMode))
        {
            Debug.Log($"[Boot] Environment Config Detected: Mode={envMode}");
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            
            // ポート設定の読み込み
            string envPort = System.Environment.GetEnvironmentVariable("VEHICLE_SYNC_PORT");
            ushort port = 7777; // Default
            if (!string.IsNullOrEmpty(envPort) && ushort.TryParse(envPort, out ushort parsedPort))
            {
                port = parsedPort;
            }

            if (envMode.ToUpper() == "HOST")
            {
                // HOST モード
                string envAssetUrl = System.Environment.GetEnvironmentVariable("VEHICLE_SYNC_ASSET_URL");
                if (!string.IsNullOrEmpty(envAssetUrl))
                {
                    Debug.Log($"[Boot] Asset Server URL set to: {envAssetUrl}");
                    _serverUrl.Value = new FixedString512Bytes(envAssetUrl);
                }

                // UnityTransportの設定 (Hostは自分自身のバインドのため、通常は "0.0.0.0" またはそのままでポートだけ設定することが多いが、
                // SetConnectionData の第一引数は Address なので、Listenする場合は "0.0.0.0" でよい)
                // しかし、UnityTransportのデフォルト実装では StartHost 時に ConnectionData がローカルアドレスに使われることがある。
                // 安全のため、既に設定されているアドレス(デフォルト)を維持しつつポートだけ変えるか、
                // 明示的に "0.0.0.0" を指定する。
                // ここでは "0.0.0.0" (Any) を指定し、ポートを設定。
                transport.SetConnectionData("0.0.0.0", port);
                
                Debug.Log($"[Boot] Starting as Host on Port {port}...");
                NetworkManager.Singleton.StartHost();
                return;
            }
            else if (envMode.ToUpper() == "CLIENT")
            {
                // CLIENT モード
                string envIp = System.Environment.GetEnvironmentVariable("VEHICLE_SYNC_SERVER_IP");
                if (string.IsNullOrEmpty(envIp))
                {
                    envIp = "127.0.0.1"; // Default fall back
                }

                transport.SetConnectionData(envIp, port);

                Debug.Log($"[Boot] Starting as Client connecting to {envIp}:{port}...");
                NetworkManager.Singleton.StartClient();
                return;
            }
        }

        // サーバービルド (Headless Mode) かどうか判定
        if (Application.isBatchMode)
        {
            Debug.Log("[Boot] Starting as Dedicated Server (BatchMode)...");
            NetworkManager.Singleton.StartServer();
        }
        else
        {
            // エディタ接続ロジック
#if UNITY_EDITOR
            // ParrelSyncによる判定
            if (ParrelSync.ClonesManager.IsClone())
            {
                Debug.Log("[Boot] Starting as Client (Clone Instance)...");
                NetworkManager.Singleton.StartClient();
            }
            else
            {
                Debug.Log("[Boot] Starting as Host (Original Instance)...");
                NetworkManager.Singleton.StartHost();
            }
#else
            // スタンドアロン / 通常ビルド
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
#endif
        }
    }
}
