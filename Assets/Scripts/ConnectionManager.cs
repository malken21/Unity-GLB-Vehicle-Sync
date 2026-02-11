using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

using Unity.Collections;

public class ConnectionManager : NetworkBehaviour
{
    public static ConnectionManager Instance { get; private set; }

    [Header("Development Settings")]
    [SerializeField] private string initialServerUrl = "http://localhost:3000";
    [SerializeField] private string initialEnvironmentUrl = "";
    public bool autoStartClientInEditor = true;
    public bool summonAvatar = true;

    private NetworkVariable<FixedString512Bytes> _serverUrl = new NetworkVariable<FixedString512Bytes>();
    private NetworkVariable<FixedString512Bytes> _environmentUrl = new NetworkVariable<FixedString512Bytes>();

    public string serverUrl => _serverUrl.Value.IsEmpty ? initialServerUrl : _serverUrl.Value.ToString();
    public string environmentUrl => _environmentUrl.Value.IsEmpty ? initialEnvironmentUrl : _environmentUrl.Value.ToString();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _serverUrl.Value = new FixedString512Bytes(initialServerUrl);
            if (!string.IsNullOrEmpty(initialEnvironmentUrl))
            {
                _environmentUrl.Value = new FixedString512Bytes(initialEnvironmentUrl);
            }
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
        // コマンドライン引数を取得
        string[] args = System.Environment.GetCommandLineArgs();
        
        string cliMode = null;
        string cliPort = null;
        string cliAssetUrl = null;
        string cliEnvironmentUrl = null;
        string cliServerIp = null;
        string cliSummonAvatar = null;

        // 引数の解析
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-mode" && i + 1 < args.Length)
            {
                cliMode = args[i + 1];
            }
            else if (args[i] == "-port" && i + 1 < args.Length)
            {
                cliPort = args[i + 1];
            }
            else if (args[i] == "-assetUrl" && i + 1 < args.Length)
            {
                cliAssetUrl = args[i + 1];
            }
            else if (args[i] == "-envUrl" && i + 1 < args.Length)
            {
                cliEnvironmentUrl = args[i + 1];
            }
            else if (args[i] == "-serverIp" && i + 1 < args.Length)
            {
                cliServerIp = args[i + 1];
            }
            else if (args[i] == "-summonAvatar" && i + 1 < args.Length)
            {
                cliSummonAvatar = args[i + 1];
            }
        }
        
        if (!string.IsNullOrEmpty(cliSummonAvatar) && bool.TryParse(cliSummonAvatar, out bool parsedSummonAvatar))
        {
            summonAvatar = parsedSummonAvatar;
            Debug.Log($"[Boot] Summon Avatar set to: {summonAvatar}");
        }

        if (!string.IsNullOrEmpty(cliMode))
        {
            Debug.Log($"[Boot] Command Line Config Detected: Mode={cliMode}");
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            
            // ポート設定の読み込み
            ushort port = 7777; // Default
            if (!string.IsNullOrEmpty(cliPort) && ushort.TryParse(cliPort, out ushort parsedPort))
            {
                port = parsedPort;
            }

            if (cliMode.ToUpper() == "HOST")
            {
                // HOST モード
                if (!string.IsNullOrEmpty(cliAssetUrl))
                {
                    Debug.Log($"[Boot] Asset Server URL set to: {cliAssetUrl}");
                    _serverUrl.Value = new FixedString512Bytes(cliAssetUrl);
                }

                if (!string.IsNullOrEmpty(cliEnvironmentUrl))
                {
                    Debug.Log($"[Boot] Environment URL set to: {cliEnvironmentUrl}");
                    _environmentUrl.Value = new FixedString512Bytes(cliEnvironmentUrl);
                }

                // UnityTransportの設定
                transport.SetConnectionData("0.0.0.0", port);
                
                Debug.Log($"[Boot] Starting as Host on Port {port}...");
                NetworkManager.Singleton.StartHost();
                return;
            }
            else if (cliMode.ToUpper() == "CLIENT")
            {
                // CLIENT モード
                string targetIp = "127.0.0.1"; // Default
                if (!string.IsNullOrEmpty(cliServerIp))
                {
                    targetIp = cliServerIp;
                }

                transport.SetConnectionData(targetIp, port);

                Debug.Log($"[Boot] Starting as Client connecting to {targetIp}:{port}...");
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
                Debug.Log($"[Boot] Starting as Host (Original Instance)...");
                if (autoStartClientInEditor)
                {
                    Debug.Log("[Boot] autoStartClientInEditor is TRUE -> Starting as Client...");
                    NetworkManager.Singleton.StartClient();
                }
                else
                {
                    Debug.Log("[Boot] autoStartClientInEditor is FALSE -> Starting as Host...");
                    NetworkManager.Singleton.StartHost();
                }
            }
#else
            // スタンドアロン / 通常ビルド
            // スタンドアロン / 通常ビルド
            // デフォルトはクライアントとして起動 (サーバーとして起動したい場合は -mode HOST を指定)
            Debug.Log("[Boot] Starting as Client (Default for Standalone)...");
            NetworkManager.Singleton.StartClient();
#endif
        }
    }
}
