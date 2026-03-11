using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class MicrobitBLEManager : MonoBehaviour
{
    public static MicrobitBLEManager Instance { get; private set; }

    [SerializeField] private string websocketUrl = "ws://127.0.0.1:4000";
    [SerializeField] private int maxReconnectDelayMs = 5000;

    public bool enableMicrobit = true;

    public bool isConnected = false;
    public string lastReceivedData = "";
    public string statusMessage = "Disconnected";

    public event Action<string> OnDataReceived;
    public event Action OnConnected;
    public event Action OnDisconnected;

    private ConcurrentQueue<string> mainThreadActionQueue = new ConcurrentQueue<string>();
    private ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
    private ConcurrentQueue<string> dataQueue = new ConcurrentQueue<string>();
    
    private string receiveBuffer = "";

    private ClientWebSocket ws;
    private CancellationTokenSource cancellationTokenSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (ConnectionManager.Instance != null)
            {
                enableMicrobit = ConnectionManager.Instance.enableMicrobit;
                if (!enableMicrobit)
                {
                    statusMessage = "Disabled (-enableMicrobit false)";
                }
                Debug.Log($"[MicrobitBLE] Microbit integration {(enableMicrobit ? "enabled" : "disabled")}.");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (!enableMicrobit) return;

        StartScanning();
    }

    private void Update()
    {
        if (!enableMicrobit) return;

        ProcessQueues();
    }

    private void ProcessQueues()
    {
        while (dataQueue.TryDequeue(out string data))
        {
            receiveBuffer += data;
        }

        int newLineIdx;
        while ((newLineIdx = receiveBuffer.IndexOf('\n')) >= 0)
        {
            string line = receiveBuffer.Substring(0, newLineIdx).Trim();
            receiveBuffer = receiveBuffer.Substring(newLineIdx + 1);

            if (!string.IsNullOrEmpty(line))
            {
                ProcessLine(line);
            }
        }

        if (receiveBuffer.Length > 0)
        {
            int lastCommaIdx = receiveBuffer.LastIndexOf(',');
            if (lastCommaIdx >= 0)
            {
                string completeData = receiveBuffer.Substring(0, lastCommaIdx);
                receiveBuffer = receiveBuffer.Substring(lastCommaIdx + 1);
                
                string[] parts = completeData.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    ProcessLine(p.Trim());
                }
            }
        }

        while (mainThreadActionQueue.TryDequeue(out string status))
        {
            statusMessage = status;
            Debug.Log($"[MicrobitBLE] {status}");
        }

        while (mainThreadActions.TryDequeue(out Action action))
        {
            try { action?.Invoke(); } catch (Exception ex) { Debug.LogError($"[MicrobitBLE] Action execution error: {ex.Message}"); }
        }
    }

    private void ProcessLine(string line)
    {
        if (lastReceivedData != line)
        {
            Debug.Log($"[MicrobitBLE] Received: {line}");
        }
        lastReceivedData = line;
        OnDataReceived?.Invoke(line);
    }

    public void RestartScanning()
    {
        mainThreadActionQueue.Enqueue("Retrying connection...");
        Disconnect();
        StartScanning();
    }

    public void StartScanning()
    {
        if (cancellationTokenSource != null)
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }

        cancellationTokenSource = new CancellationTokenSource();
        _ = ConnectLoopAsync(cancellationTokenSource.Token);
    }

    private async Task ConnectLoopAsync(CancellationToken token)
    {
        int delayMs = 1000;

        while (!token.IsCancellationRequested)
        {
            try
            {
                using (ws = new ClientWebSocket())
                {
                    Uri serverUri = new Uri(websocketUrl);
                    mainThreadActionQueue.Enqueue($"Connecting to {serverUri}...");

                    await ws.ConnectAsync(serverUri, token);
                    
                    if (ws.State == WebSocketState.Open)
                    {
                        isConnected = true;
                        mainThreadActionQueue.Enqueue("Connected to MicroBridge.");
                        
                        mainThreadActions.Enqueue(() => { try { OnConnected?.Invoke(); } catch { } });

                        delayMs = 1000;

                        await ReceiveLoopAsync(ws, token);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    mainThreadActionQueue.Enqueue($"Connection error: {ex.Message}");
                }
            }
            finally
            {
                if (isConnected)
                {
                    isConnected = false;
                    mainThreadActionQueue.Enqueue("Disconnected from MicroBridge.");
                    mainThreadActions.Enqueue(() => { try { OnDisconnected?.Invoke(); } catch { } });
                }
            }

            if (!token.IsCancellationRequested)
            {
                mainThreadActionQueue.Enqueue($"Retrying in {delayMs / 1000.0}s...");
                await Task.Delay(delayMs, token);
                delayMs = Mathf.Min(delayMs * 2, maxReconnectDelayMs);
            }
        }
    }

    private async Task ReceiveLoopAsync(ClientWebSocket webSocket, CancellationToken token)
    {
        var buffer = new byte[1024 * 4];

        while (webSocket.State == WebSocketState.Open && !token.IsCancellationRequested)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, token);
                break;
            }
            else
            {
                string receivedString = Encoding.UTF8.GetString(buffer, 0, result.Count);
                dataQueue.Enqueue(receivedString);
            }
        }
    }

    private void Disconnect()
    {
        if (cancellationTokenSource != null)
        {
            try { cancellationTokenSource.Cancel(); } catch { }
            try { cancellationTokenSource.Dispose(); } catch { }
            cancellationTokenSource = null;
        }

        if (ws != null)
        {
            if (ws.State == WebSocketState.Open)
            {
                try { _ = ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None); } catch { }
            }
            ws = null;
        }
    }

    private void OnDestroy()
    {
        Disconnect();
    }
}
