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

public class MicrobitBLEManager : MonoBehaviour
{
    public static MicrobitBLEManager Instance { get; private set; }

#if ENABLE_WINMD_SUPPORT
    [Header("Settings")]
    [SerializeField] private string targetDeviceNameStart = "BBC micro:bit";
    // Nordic UART Service UUID
    [SerializeField] private string serviceUUID = "6E400001-B5A3-F393-E0A9-E50E24DCCA9E";
    // RX Characteristic (Write) - Not used for this simple control but good to have
    [SerializeField] private string rxCharUUID = "6E400003-B5A3-F393-E0A9-E50E24DCCA9E";
    // TX Characteristic (Notify) - We listen to this
    [SerializeField] private string txCharUUID = "6E400002-B5A3-F393-E0A9-E50E24DCCA9E";
#endif

    [Header("Status")]
    public bool isConnected = false;
    public string lastReceivedData = "";
    public string statusMessage = "Not Connected";

    // Events
    public event Action<string> OnDataReceived;
    public event Action OnConnected;
    public event Action OnDisconnected;

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
        StartScanning();
#else
        Debug.LogWarning("[MicrobitBLE] Setup required for Native Bluetooth on this platform. Use Editor Debug Keys (1, 2, 3).");
        statusMessage = "Use Debug Keys (1, 2, 3)";
#endif
    }

    private void Update()
    {
        // Dispatch events on main thread
        while (dataQueue.TryDequeue(out string data))
        {
            lastReceivedData = data;
            OnDataReceived?.Invoke(data);
        }

        while (mainThreadActionQueue.TryDequeue(out string status))
        {
            statusMessage = status;
            Debug.Log($"[MicrobitBLE] {status}");
        }

#if UNITY_EDITOR
        // Debug inputs for testing without device
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            dataQueue.Enqueue("L");
            mainThreadActionQueue.Enqueue("Debug: Sent L");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            dataQueue.Enqueue("R");
            mainThreadActionQueue.Enqueue("Debug: Sent R");
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            dataQueue.Enqueue("S");
            mainThreadActionQueue.Enqueue("Debug: Sent S");
        }
#endif
    }

    public void StartScanning()
    {
#if ENABLE_WINMD_SUPPORT
        statusMessage = "Scanning for Micro:bit...";
        string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

        // Watch for Bluetooth LE devices
        // Note: In a production app, you might want a more robust watcher or picker.
        // For simplicity, we'll try to find a paired device or watch for advertisement.
        // Using DeviceWatcher to find specifically the micro:bit efficiently.
        
        string aqs = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-7950a0021805}\")";
        DeviceWatcher watcher = DeviceInformation.CreateWatcher(aqs, requestedProperties, DeviceInformationKind.AssociationEndpoint);

        watcher.Added += (DeviceWatcher sender, DeviceInformation deviceInfo) =>
        {
            if (deviceInfo.Name.StartsWith(targetDeviceNameStart))
            {
                mainThreadActionQueue.Enqueue($"Found: {deviceInfo.Name}");
                ConnectToDevice(deviceInfo.Id);
            }
        };

        watcher.Updated += (DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate) =>
        {
            // Handle updates if needed
        };

        watcher.Start();
#endif
    }

#if ENABLE_WINMD_SUPPORT
    private async void ConnectToDevice(string deviceId)
    {
        try
        {
            // Stop scanning if possible, or just proceed. 
            // In this simple example we don't hold a reference to watcher to stop it immediately, 
            // but connecting usually works fine.

            bluetoothDevice = await BluetoothLEDevice.FromIdAsync(deviceId);
            
            if (bluetoothDevice == null)
            {
                mainThreadActionQueue.Enqueue("Failed to connect to device.");
                return;
            }

            mainThreadActionQueue.Enqueue($"Connected to {bluetoothDevice.Name}");

            // Get GATT Services
            GattDeviceServicesResult servicesResult = await bluetoothDevice.GetGattServicesAsync();
            
            if (servicesResult.Status == GattCommunicationStatus.Success)
            {
                foreach (var service in servicesResult.Services)
                {
                    if (service.Uuid.ToString().ToUpper() == serviceUUID.Replace("-", "")) // format check might be needed
                    {
                        // Found UART Service (UUID comparison usually needs normalization)
                        // Actually let's try FindAsync for specific service to be cleaner
                    }
                }

                // Better way: get specific service
                // Note: Guid.Parse needs the dashes usually.
                Guid serviceGuid;
                if (!Guid.TryParse(serviceUUID, out serviceGuid))
                {
                     mainThreadActionQueue.Enqueue("Invalid Service UUID format");
                     return;
                }

                var serviceResult = await bluetoothDevice.GetGattServicesForUuidAsync(serviceGuid);
                if (serviceResult.Status == GattCommunicationStatus.Success && serviceResult.Services.Count > 0)
                {
                    var service = serviceResult.Services[0];
                    mainThreadActionQueue.Enqueue("Found UART Service");

                    // Get Characteristics
                    Guid txGuid;
                    Guid.TryParse(txCharUUID, out txGuid);
                    
                    var charResult = await service.GetCharacteristicsForUuidAsync(txGuid);
                    
                    if (charResult.Status == GattCommunicationStatus.Success && charResult.Characteristics.Count > 0)
                    {
                        txCharacteristic = charResult.Characteristics[0];
                        
                        // Enable Notifications
                        var status = await txCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                        
                        if (status == GattCommunicationStatus.Success)
                        {
                            txCharacteristic.ValueChanged += TxCharacteristic_ValueChanged;
                            isConnected = true;
                            mainThreadActionQueue.Enqueue("Listening for data...");
                            
                            // Invoking event on main thread via update check or simple flag
                            // Since we are async void, be careful.
                        }
                        else
                        {
                            mainThreadActionQueue.Enqueue("Failed to enable notifications");
                        }
                    }
                    else
                    {
                        mainThreadActionQueue.Enqueue("TX Characteristic not found");
                    }
                }
                else
                {
                    mainThreadActionQueue.Enqueue("UART Service not found on device");
                }
            }
            else
            {
                mainThreadActionQueue.Enqueue("Failed to get services");
            }
        }
        catch (Exception ex)
        {
            mainThreadActionQueue.Enqueue($"Connection Error: {ex.Message}");
        }
    }

    private void TxCharacteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
    {
        // Read data
        var reader = DataReader.FromBuffer(args.CharacteristicValue);
        byte[] input = new byte[reader.UnconsumedBufferLength];
        reader.ReadBytes(input);
        
        string receivedString = Encoding.UTF8.GetString(input);
        dataQueue.Enqueue(receivedString);
    }
#endif

    private void OnDestroy()
    {
#if ENABLE_WINMD_SUPPORT
        if (bluetoothDevice != null)
        {
            bluetoothDevice.Dispose();
            bluetoothDevice = null;
        }
#endif
    }
}
