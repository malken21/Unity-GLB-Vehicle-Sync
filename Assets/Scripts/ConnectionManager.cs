using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.InputSystem;

using Unity.Collections;

public class ConnectionManager : NetworkBehaviour
{
    public static ConnectionManager Instance { get; private set; }

    [Header("Development Settings")]
    [SerializeField] private string initialServerUrl = "http://localhost:3000";
    public bool autoStartClientInEditor = true;
    public bool summonAvatar = true;
    public string avatarGlbPath = null;

    private NetworkVariable<FixedString512Bytes> _serverUrl = new NetworkVariable<FixedString512Bytes>();

    public string serverUrl => _serverUrl.Value.IsEmpty ? initialServerUrl : _serverUrl.Value.ToString();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            string url = initialServerUrl;
            if (url.Contains("localhost") || url.Contains("127.0.0.1"))
            {
                string ip = GetLocalIPAddress();
                if (!string.IsNullOrEmpty(ip))
                {
                    url = url.Replace("localhost", ip).Replace("127.0.0.1", ip);
                    Debug.Log($"[ConnectionManager] Replaced localhost/127.0.0.1 with {ip} -> {url}");
                }
            }
            _serverUrl.Value = new FixedString512Bytes(url);
        }
    }

    private string GetLocalIPAddress()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            string bestIp = null;

            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    string ipStr = ip.ToString();
                    
                    // 優先順位: 192.168 > 10. > 172. > その他
                    if (ipStr.StartsWith("192.168."))
                    {
                        return ipStr; // 最も一般的
                    }
                    if (ipStr.StartsWith("10."))
                    {
                        if (bestIp == null || !bestIp.StartsWith("192.168.")) bestIp = ipStr;
                    }
                    else if (ipStr.StartsWith("172."))
                    {
                        // 172.16.x.x - 172.31.x.x はプライベートIP
                        int secondOctet = int.Parse(ipStr.Split('.')[1]);
                        if (secondOctet >= 16 && secondOctet <= 31)
                        {
                            if (bestIp == null) bestIp = ipStr;
                        }
                    }
                    else if (bestIp == null)
                    {
                        bestIp = ipStr;
                    }
                }
            }
            
            if (bestIp != null) return bestIp;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ConnectionManager] Failed to get local IP address: {e.Message}");
        }
        return "127.0.0.1";
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
        // CommandLineParserのインスタンスが確実に存在するように生成（アタッチ忘れ対策）
        if (CommandLineParser.Instance == null)
        {
            var go = new GameObject("CommandLineParser");
            go.AddComponent<CommandLineParser>();
        }

        var cmd = CommandLineParser.Instance;
        
        summonAvatar = cmd.SummonAvatar;
        Debug.Log($"[Boot] Summon Avatar set to: {summonAvatar}");

        if (!string.IsNullOrEmpty(cmd.AvatarGlbPath))
        {
            avatarGlbPath = cmd.AvatarGlbPath;
            Debug.Log($"[Boot] Avatar GLB Path set to: {avatarGlbPath}");
        }

        if (!string.IsNullOrEmpty(cmd.AssetUrl))
        {
            Debug.Log($"[Boot] Asset Server URL set to: {cmd.AssetUrl}");
            initialServerUrl = cmd.AssetUrl;
        }

        if (!string.IsNullOrEmpty(cmd.Mode))
        {
            Debug.Log($"[Boot] Command Line Config Detected: Mode={cmd.Mode}");
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            
            ushort port = cmd.Port;

            if (cmd.Mode == "HOST")
            {
                // HOST モード
                // UnityTransportの設定
                transport.SetConnectionData("0.0.0.0", port);
                
                Debug.Log($"[Boot] Starting as Host on Port {port}...");
                NetworkManager.Singleton.StartHost();
                return;
            }
            else if (cmd.Mode == "CLIENT")
            {
                // CLIENT モード
                string targetIp = cmd.ServerIp;

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

            // デフォルトはクライアントとして起動 (サーバーとして起動したい場合は -mode HOST を指定)
            Debug.Log("[Boot] Starting as Client (Default for Standalone)...");
            NetworkManager.Singleton.StartClient();
#endif
        }
    }
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f11Key.wasPressedThisFrame)
        {
            if (Screen.fullScreen)
            {
                Screen.fullScreenMode = FullScreenMode.Windowed;
                Screen.fullScreen = false;
            }
            else
            {
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
            }
        }
    }
}
