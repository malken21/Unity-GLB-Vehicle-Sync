using System;
using System.Collections.Concurrent;
using System.Text;
using UnityEngine;

#if ENABLE_WINMD_SUPPORT || UNITY_EDITOR_WIN
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
#endif

/// <summary>
/// BBC micro:bit との Bluetooth LE (BLE) 通信を管理するクラス。
/// Windows のネイティブ Bluetooth API (WinRT) を使用します。
/// </summary>
public class MicrobitBLEManager : MonoBehaviour
{
    public static MicrobitBLEManager Instance { get; private set; }

#if ENABLE_WINMD_SUPPORT || UNITY_EDITOR_WIN
    [Header("設定")]
    [SerializeField] private string targetDeviceNameStart = "BBC micro:bit";
    // Nordic UART サービスの UUID
    [SerializeField] private string serviceUUID = "6E400001-B5A3-F393-E0A9-E50E24DCCA9E";
    // RX 特性 (書き込み用) - 今回のシンプルな制御では使用しませんが、拡張用に定義
    [SerializeField] private string rxCharUUID = "6E400003-B5A3-F393-E0A9-E50E24DCCA9E";
    // TX 特性 (通知用) - micro:bit からのデータをリッスンします
    [SerializeField] private string txCharUUID = "6E400002-B5A3-F393-E0A9-E50E24DCCA9E";
#endif

    [Header("連携機能の有効/無効")]
    public bool enableMicrobit = true;

    [Header("ステータス")]
    public bool isConnected = false;
    public string lastReceivedData = "";
    public string statusMessage = "未接続";

    // イベント
    public event Action<string> OnDataReceived;
#pragma warning disable 67
    public event Action OnConnected;
    public event Action OnDisconnected;
#pragma warning restore 67

    // メインスレッドで処理を実行するためのキュー
    private ConcurrentQueue<string> mainThreadActionQueue = new ConcurrentQueue<string>();
    private ConcurrentQueue<string> dataQueue = new ConcurrentQueue<string>();

#if ENABLE_WINMD_SUPPORT || UNITY_EDITOR_WIN
    private BluetoothLEDevice bluetoothDevice;
    private GattCharacteristic txCharacteristic;
#endif

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (CommandLineParser.Instance != null)
            {
                enableMicrobit = CommandLineParser.Instance.EnableMicrobit;
                if (!enableMicrobit)
                {
                    statusMessage = "機能無効化 (-enableMicrobit false)";
                }
                Debug.Log($"[MicrobitBLE] CommandLineParser により連携機能を{(enableMicrobit ? "有効化" : "無効化")}しました。");
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

#if ENABLE_WINMD_SUPPORT || UNITY_EDITOR_WIN
        // スキャンを開始
        StartScanning();
#else
        Debug.LogWarning("[MicrobitBLE] このプラットフォームでネイティブ Bluetooth を使用するにはセットアップが必要です。エディタのデバッグキー (1, 2, 3) を使用してください。");
        statusMessage = "デバッグキー (1, 2, 3) を使用中";
#endif
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
            lastReceivedData = data;
            OnDataReceived?.Invoke(data);
        }

        // メインスレッドでステータスメッセージを更新
        while (mainThreadActionQueue.TryDequeue(out string status))
        {
            statusMessage = status;
            Debug.Log($"[MicrobitBLE] {status}");
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// デバイスがない状態でのテスト用デバッグ入力を処理します。
    /// </summary>
    private void ProcessDebugInput()
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.f1Key.wasPressedThisFrame) EnqueueDebugCommand("1,0,0,0", "ボタンA");
        else if (keyboard.f2Key.wasPressedThisFrame) EnqueueDebugCommand("0,1,0,0", "ボタンB");
        else if (keyboard.f3Key.wasPressedThisFrame) EnqueueDebugCommand("0,0,1,0", "ジャンプ");
        else if (keyboard.f4Key.wasPressedThisFrame) EnqueueDebugCommand("C:RED", "赤色変更");
        else if (keyboard.f5Key.wasPressedThisFrame) EnqueueDebugCommand("C:BLUE", "青色変更");
        else if (keyboard.f6Key.wasPressedThisFrame) EnqueueDebugCommand("C:GREEN", "緑色変更");
        else if (keyboard.f7Key.wasPressedThisFrame) EnqueueDebugCommand("0,0,0,0", "停止状態へ");
    }

    /// <summary>
    /// 外部（デバッグウィンドウ等）から擬似的な受信データを注入します。
    /// </summary>
    /// <param name="data">注入するデータ文字列 (例: "0,0,1,0")</param>
    public void InjectDebugData(string data)
    {
        dataQueue.Enqueue(data);
        mainThreadActionQueue.Enqueue($"外部デバッグ注入: '{data}'");
    }

    private void EnqueueDebugCommand(string command, string description)
    {
        dataQueue.Enqueue(command);
        mainThreadActionQueue.Enqueue($"デバッグ: '{command}' ({description}) を受信");
    }
#endif

    /// <summary>
    /// スキャンを停止し、再度新規に開始します。
    /// </summary>
    public void RestartScanning()
    {
#if ENABLE_WINMD_SUPPORT || UNITY_EDITOR_WIN
        mainThreadActionQueue.Enqueue("スキャンを再試行します...");
        // 既存の接続をクリーンアップ
        if (bluetoothDevice != null)
        {
            bluetoothDevice.Dispose();
            bluetoothDevice = null;
        }
        isConnected = false;
        StartScanning();
#else
        Debug.LogWarning("[MicrobitBLE] エディタ実行時や非Windows環境では、実際のBLEスキャンは利用できません。");
        mainThreadActionQueue.Enqueue("実機スキャン不可（エディタ環境等）");
#endif
    }

    /// <summary>
    /// 対応する Bluetooth LE デバイスのスキャンを開始します。
    /// </summary>
    public void StartScanning()
    {
#if ENABLE_WINMD_SUPPORT || UNITY_EDITOR_WIN
        statusMessage = "micro:bit を検索中...";
        string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

        // Bluetooth LE デバイスを監視
        // AQS (Advanced Query Syntax) を使用して BLE プロトコルを指定
        string aqs = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-7950a0021805}\")";
        DeviceWatcher watcher = DeviceInformation.CreateWatcher(aqs, requestedProperties, DeviceInformationKind.AssociationEndpoint);

        watcher.Added += (DeviceWatcher sender, DeviceInformation deviceInfo) =>
        {
            // デバイス名が指定の文字列で始まるものをターゲットとする
            if (deviceInfo.Name.StartsWith(targetDeviceNameStart))
            {
                mainThreadActionQueue.Enqueue($"発見: {deviceInfo.Name}");
                ConnectToDevice(deviceInfo.Id);
            }
        };

        watcher.Updated += (DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate) =>
        {
            // 必要に応じて更新処理を記述
        };

        watcher.Start();
#endif
    }

#if ENABLE_WINMD_SUPPORT || UNITY_EDITOR_WIN
    /// <summary>
    /// ID を使用して特定の Bluetooth デバイスに接続し、UART サービスをセットアップします。
    /// </summary>
    private async void ConnectToDevice(string deviceId)
    {
        try
        {
            // デバイスのインスタンスを取得
            bluetoothDevice = await BluetoothLEDevice.FromIdAsync(deviceId);
            
            if (bluetoothDevice == null)
            {
                mainThreadActionQueue.Enqueue("デバイスへの接続に失敗しました。");
                return;
            }

            mainThreadActionQueue.Enqueue($"{bluetoothDevice.Name} に接続しました");

            // GATT サービスを取得
            GattDeviceServicesResult servicesResult = await bluetoothDevice.GetGattServicesAsync();
            
            if (servicesResult.Status == GattCommunicationStatus.Success)
            {
                // 特定のサービス UUID を検索
                Guid serviceGuid;
                if (!Guid.TryParse(serviceUUID, out serviceGuid))
                {
                     mainThreadActionQueue.Enqueue("サービス UUID の形式が不正です");
                     return;
                }

                var serviceResult = await bluetoothDevice.GetGattServicesForUuidAsync(serviceGuid);
                if (serviceResult.Status == GattCommunicationStatus.Success && serviceResult.Services.Count > 0)
                {
                    var service = serviceResult.Services[0];
                    mainThreadActionQueue.Enqueue("UART サービスを検出");

                    // キャラクタリスティック (特性) を取得
                    Guid txGuid;
                    Guid.TryParse(txCharUUID, out txGuid);
                    
                    var charResult = await service.GetCharacteristicsForUuidAsync(txGuid);
                    
                    if (charResult.Status == GattCommunicationStatus.Success && charResult.Characteristics.Count > 0)
                    {
                        txCharacteristic = charResult.Characteristics[0];
                        
                        // 通知 (Notifications) を有効にする
                        var status = await txCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                        
                        if (status == GattCommunicationStatus.Success)
                        {
                            txCharacteristic.ValueChanged += TxCharacteristic_ValueChanged;
                            isConnected = true;
                            mainThreadActionQueue.Enqueue("データの受信待機中...");
                            OnConnected?.Invoke();
                        }
                        else
                        {
                            mainThreadActionQueue.Enqueue("通知の有効化に失敗しました");
                        }
                    }
                    else
                    {
                        mainThreadActionQueue.Enqueue("TX 特性が見つかりません");
                    }
                }
                else
                {
                    mainThreadActionQueue.Enqueue("デバイス上に UART サービスが見つかりません");
                }
            }
            else
            {
                mainThreadActionQueue.Enqueue("サービスの取得に失敗しました");
            }
        }
        catch (Exception ex)
        {
            mainThreadActionQueue.Enqueue($"接続エラー: {ex.Message}");
        }
    }

    /// <summary>
    /// キャラクタリスティックの値が変更された（通知を受信した）際のコールバック。
    /// </summary>
    private void TxCharacteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
    {
        // データの読み取り
        var reader = DataReader.FromBuffer(args.CharacteristicValue);
        byte[] input = new byte[reader.UnconsumedBufferLength];
        reader.ReadBytes(input);
        
        // UTF8 文字列として解析しキューに追加
        string receivedString = Encoding.UTF8.GetString(input);
        dataQueue.Enqueue(receivedString);
    }
#endif

    private void OnDestroy()
    {
#if ENABLE_WINMD_SUPPORT || UNITY_EDITOR_WIN
        if (bluetoothDevice != null)
        {
            // リソースの解放
            bluetoothDevice.Dispose();
            bluetoothDevice = null;
        }
#endif
    }
}
