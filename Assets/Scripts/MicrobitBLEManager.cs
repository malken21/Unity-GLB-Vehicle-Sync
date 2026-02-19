using System;
using System.Collections.Concurrent;
using System.Text;
using UnityEngine;

#if ENABLE_WINMD_SUPPORT
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

#if ENABLE_WINMD_SUPPORT
    [Header("設定")]
    [SerializeField] private string targetDeviceNameStart = "BBC micro:bit";
    // Nordic UART サービスの UUID
    [SerializeField] private string serviceUUID = "6E400001-B5A3-F393-E0A9-E50E24DCCA9E";
    // RX 特性 (書き込み用) - 今回のシンプルな制御では使用しませんが、拡張用に定義
    [SerializeField] private string rxCharUUID = "6E400003-B5A3-F393-E0A9-E50E24DCCA9E";
    // TX 特性 (通知用) - micro:bit からのデータをリッスンします
    [SerializeField] private string txCharUUID = "6E400002-B5A3-F393-E0A9-E50E24DCCA9E";
#endif

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

#if ENABLE_WINMD_SUPPORT
    private BluetoothLEDevice bluetoothDevice;
    private GattCharacteristic txCharacteristic;
#endif

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
#if ENABLE_WINMD_SUPPORT
        // スキャンを開始
        StartScanning();
#else
        Debug.LogWarning("[MicrobitBLE] このプラットフォームでネイティブ Bluetooth を使用するにはセットアップが必要です。エディタのデバッグキー (1, 2, 3) を使用してください。");
        statusMessage = "デバッグキー (1, 2, 3) を使用中";
#endif
    }

    private void Update()
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

#if UNITY_EDITOR
        // デバイスがない状態でのテスト用デバッグ入力
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            dataQueue.Enqueue("L");
            mainThreadActionQueue.Enqueue("デバッグ: 'L' (左) を受信");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            dataQueue.Enqueue("R");
            mainThreadActionQueue.Enqueue("デバッグ: 'R' (右) を受信");
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            dataQueue.Enqueue("S");
            mainThreadActionQueue.Enqueue("デバッグ: 'S' (停止) を受信");
        }
#endif
    }

    /// <summary>
    /// 対応する Bluetooth LE デバイスのスキャンを開始します。
    /// </summary>
    public void StartScanning()
    {
#if ENABLE_WINMD_SUPPORT
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

#if ENABLE_WINMD_SUPPORT
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
#if ENABLE_WINMD_SUPPORT
        if (bluetoothDevice != null)
        {
            // リソースの解放
            bluetoothDevice.Dispose();
            bluetoothDevice = null;
        }
#endif
    }
}
