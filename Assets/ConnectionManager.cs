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
