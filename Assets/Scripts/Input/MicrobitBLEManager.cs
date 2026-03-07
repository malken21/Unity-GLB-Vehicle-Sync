using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// MicroBridge (WebSocketサーバー)経由で BBC micro:bit との通信を管理するクラス。
/// 従来のBLE通信機能をWebSocket通信に置き換えた実装です。
/// </summary>
public class MicrobitBLEManager : MonoBehaviour
{
    public static MicrobitBLEManager Instance { get; private set; }

    [Header("設定")]
    [SerializeField] private string websocketUrl = "ws://127.0.0.1:4000";
    [SerializeField] private int maxReconnectDelayMs = 5000;

    [Header("連携機能の有効/無効")]
    public bool enableMicrobit = true;

    [Header("ステータス")]
    public bool isConnected = false;
    public string lastReceivedData = "";
    public string statusMessage = "未接続";

    // イベント
    public event Action<string> OnDataReceived;
    public event Action OnConnected;
    public event Action OnDisconnected;

    // メインスレッドで処理を実行するためのキュー
    private ConcurrentQueue<string> mainThreadActionQueue = new ConcurrentQueue<string>();
    private ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
    private ConcurrentQueue<string> dataQueue = new ConcurrentQueue<string>();
    
    // 未処理の受信バッファ（パース用）
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
                    statusMessage = "機能無効化 (-enableMicrobit false)";
                }
                Debug.Log($"[MicrobitBLE] ConnectionManager により連携機能を{(enableMicrobit ? "有効化" : "無効化")}しました。");
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

#if UNITY_EDITOR
        ProcessDebugInput();
#endif
    }

    /// <summary>
    /// メインスレッドで実行する必要があるキューの処理を行います。
    /// </summary>
    private void ProcessQueues()
    {
        // メインスレッドで受信データイベントをディスパッチ
        while (dataQueue.TryDequeue(out string data))
        {
            receiveBuffer += data;
        }

        // 行単位で分割してイベントを発火
        int newLineIdx;
        while ((newLineIdx = receiveBuffer.IndexOf('\n')) >= 0)
        {
            string line = receiveBuffer.Substring(0, newLineIdx).Trim();
            // 次の処理のためにバッファを更新
            receiveBuffer = receiveBuffer.Substring(newLineIdx + 1);

            if (!string.IsNullOrEmpty(line))
            {
                ProcessLine(line);
            }
        }

        // 改行が含まれていないが、カンマやコロンが含まれている場合に備えた予備処理
        // MicroBridgeが非常に短い間隔でデータを送り、バッファが溜まった場合に、
        // 最後に改行がないデータもカンマベースで区切って処理する
        if (receiveBuffer.Length > 20) // ある程度の長さが溜まっていても改行がない場合
        {
            int lastCommaIdx = receiveBuffer.LastIndexOf(',');
            if (lastCommaIdx > 0)
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

        // メインスレッドでステータスメッセージを更新
        while (mainThreadActionQueue.TryDequeue(out string status))
        {
            statusMessage = status;
            Debug.Log($"[MicrobitBLE] {status}");
        }

        // メインスレッドでコールバックアクションを実行
        while (mainThreadActions.TryDequeue(out Action action))
        {
            try { action?.Invoke(); } catch (Exception ex) { Debug.LogError($"[MicrobitBLE] Action execution error: {ex.Message}"); }
        }
    }

    private void ProcessLine(string line)
    {
        lastReceivedData = line;
        Debug.Log($"[MicrobitBLE] Received: {line}");
        OnDataReceived?.Invoke(line);
    }

#if UNITY_EDITOR
    /// <summary>
    /// サーバーがない状態でのテスト用デバッグ入力を処理します。
    /// </summary>
    private void ProcessDebugInput()
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.f1Key.wasPressedThisFrame) EnqueueDebugCommand("a:1,b:0,j:0,r:0\n", "ボタンA");
        else if (keyboard.f2Key.wasPressedThisFrame) EnqueueDebugCommand("a:0,b:1,j:0,r:0\n", "ボタンB");
        else if (keyboard.f3Key.wasPressedThisFrame) EnqueueDebugCommand("a:0,b:0,j:1,r:0\n", "ジャンプ");
        else if (keyboard.f4Key.wasPressedThisFrame) EnqueueDebugCommand("C:RED\n", "赤色変更");
        else if (keyboard.f5Key.wasPressedThisFrame) EnqueueDebugCommand("C:BLUE\n", "青色変更");
        else if (keyboard.f6Key.wasPressedThisFrame) EnqueueDebugCommand("C:GREEN\n", "緑色変更");
        else if (keyboard.f7Key.wasPressedThisFrame) EnqueueDebugCommand("a:0,b:0,j:0,r:0\n", "停止状態へ");
    }

    /// <summary>
    /// 外部（デバッグウィンドウ等）から擬似的な受信データを注入します。
    /// </summary>
    /// <param name="data">注入するデータ文字列</param>
    public void InjectDebugData(string data)
    {
        if (!data.EndsWith("\n")) data += "\n";
        dataQueue.Enqueue(data);
        mainThreadActionQueue.Enqueue($"外部デバッグ注入: '{data.Trim()}'");
    }

    private void EnqueueDebugCommand(string command, string description)
    {
        dataQueue.Enqueue(command);
        mainThreadActionQueue.Enqueue($"デバッグ: '{command.Trim()}' ({description}) を受信");
    }
#endif

    /// <summary>
    /// 接続を再試行します。
    /// </summary>
    public void RestartScanning()
    {
        mainThreadActionQueue.Enqueue("WebSocketへの接続を再試行します...");
        Disconnect();
        StartScanning();
    }

    /// <summary>
    /// MicroBridgeサーバーへの接続を開始します。（以前のスキャンと同等の位置づけ）
    /// </summary>
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
                    mainThreadActionQueue.Enqueue($"{serverUri} に接続中...");

                    await ws.ConnectAsync(serverUri, token);
                    
                    if (ws.State == WebSocketState.Open)
                    {
                        isConnected = true;
                        mainThreadActionQueue.Enqueue("MicroBridgeに接続しました。データ受信待機中...");
                        
                        // イベントの発行はメインスレッド側のキューへディスパッチする
                        mainThreadActions.Enqueue(() => { try { OnConnected?.Invoke(); } catch { } });

                        delayMs = 1000; // 成功したらリトライ間隔をリセット

                        await ReceiveLoopAsync(ws, token);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    mainThreadActionQueue.Enqueue($"接続エラー: {ex.Message}");
                }
            }
            finally
            {
                if (isConnected)
                {
                    isConnected = false;
                    mainThreadActionQueue.Enqueue("MicroBridgeから切断されました。");
                    mainThreadActions.Enqueue(() => { try { OnDisconnected?.Invoke(); } catch { } });
                }
            }

            if (!token.IsCancellationRequested)
            {
                mainThreadActionQueue.Enqueue($"{delayMs / 1000.0}秒後に再接続を試行します...");
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
            // using ブロックを抜ける際に自動的に Dispose されるため、ここでは呼ばない
            ws = null;
        }
    }

    private void OnDestroy()
    {
        Disconnect();
    }
}
