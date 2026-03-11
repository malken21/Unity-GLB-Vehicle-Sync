using System;
using System.IO;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using GLTFast;
using GLTFast.Logging;
using GLTFast.Materials;

using Unity.Netcode;
using Unity.Collections;

public class Avatar : NetworkBehaviour
{
    private readonly NetworkVariable<FixedString512Bytes> avatarUrlNetwork = new NetworkVariable<FixedString512Bytes>();
    private readonly NetworkVariable<float> modelScaleNetwork = new NetworkVariable<float>(1.0f);
    private readonly NetworkVariable<float> modelRotationYNetwork = new NetworkVariable<float>(0.0f);
    
    private readonly NetworkVariable<bool> isVisibleNetwork = new NetworkVariable<bool>(true);

    public static bool s_hideOtherPlayers = false;

    private Vector3 initialPosition;
    
    public float CurrentScale => modelScaleNetwork.Value;
    public float CurrentRotationY => modelRotationYNetwork.Value;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        initialPosition = transform.position;

        avatarUrlNetwork.OnValueChanged += OnAvatarUrlChanged;
        modelScaleNetwork.OnValueChanged += OnModelTransformChanged;
        modelRotationYNetwork.OnValueChanged += OnModelTransformChanged;
        isVisibleNetwork.OnValueChanged += OnVisibilityChanged;

        if (!avatarUrlNetwork.Value.IsEmpty && ConnectionManager.Instance.summonAvatar)
        {
            _ = LoadAvatar(avatarUrlNetwork.Value.ToString());
        }

        UpdateLocalVisibility();

        if (IsOwner)
        {
            if (ConnectionManager.Instance.summonAvatar)
            {
                string filePath = null;

                if (!string.IsNullOrEmpty(ConnectionManager.Instance.avatarGlbPath))
                {
                    if (File.Exists(ConnectionManager.Instance.avatarGlbPath))
                    {
                        filePath = ConnectionManager.Instance.avatarGlbPath;
                        Debug.Log($"[Avatar] Loading GLB from command line argument: {filePath}");
                    }
                    else
                    {
                        Debug.LogWarning($"[Avatar] GLB file not found at path specified by command line: {ConnectionManager.Instance.avatarGlbPath}");
                    }
                }

                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = WindowsFileDialog.Open("GLB Files (*.glb)|*.glb|All Files (*.*)|*.*", "Select GLB Avatar");
                    if (string.IsNullOrEmpty(filePath))
                    {
                        Debug.Log("[Avatar] No GLB path provided. Using default instead of opening file dialog for debugging.");
                    }
                }

                if (!string.IsNullOrEmpty(filePath))
                {
                    StartCoroutine(UploadAndLoad(filePath));
                }
                else
                {
                    Debug.Log("[Avatar] No file selected. Disabling avatar visibility.");
                    SetVisibilityServerRpc(false);
                }
            }
            else
            {
                Debug.Log("[Avatar] Summoning disabled. Informing server of invisibility.");
                SetVisibilityServerRpc(false);
            }
        }
        
        if (!IsOwner || !ConnectionManager.Instance.summonAvatar)
        {
             var movement = GetComponent<AvatarMovementController>();
             if (movement != null) movement.enabled = false;
        }

        if (IsOwner)
        {
            if (MicrobitBLEManager.Instance == null && ConnectionManager.Instance.enableMicrobit)
            {
                var mgrGo = new GameObject("MicrobitBLEManager");
                mgrGo.AddComponent<MicrobitBLEManager>();
                Debug.Log("[Avatar] MicrobitBLEManager created.");
            }
        }
    }

    private void OnAvatarUrlChanged(FixedString512Bytes previousValue, FixedString512Bytes newValue)
    {
        if (!newValue.IsEmpty)
        {
            _ = LoadAvatar(newValue.ToString());
        }
    }

    private void OnModelTransformChanged(float previousValue, float newValue)
    {
        UpdateModelTransform();
    }

    private void OnVisibilityChanged(bool previousValue, bool newValue)
    {
        UpdateLocalVisibility();
    }

    public void UpdateLocalVisibility()
    {
        bool targetVisibility = isVisibleNetwork.Value;
        if (!IsOwner && s_hideOtherPlayers)
        {
            targetVisibility = false;
        }

        if (modelContainer != null)
        {
             modelContainer.SetActive(targetVisibility);
        }

        foreach (var c in GetComponents<Collider>())
        {
            c.enabled = targetVisibility;
        }
        
        Debug.Log($"[Avatar] Visibility updated to: {targetVisibility}");
    }

    private void UpdateModelTransform()
    {
        if (modelContainer != null)
        {
            modelContainer.transform.localScale = Vector3.one * modelScaleNetwork.Value;
            modelContainer.transform.localRotation = Quaternion.Euler(0, modelRotationYNetwork.Value, 0);
        }
    }
    
    public void RequestTransformUpdate(float scale, float rotationY)
    {
         if (IsOwner)
         {
              UpdateModelTransformServerRpc(scale, rotationY);
         }
    }

    [ServerRpc]
    private void SetAvatarUrlServerRpc(string url)
    {
        avatarUrlNetwork.Value = new FixedString512Bytes(url);
    }

    [ServerRpc]
    private void UpdateModelTransformServerRpc(float scale, float rotationY)
    {
        modelScaleNetwork.Value = scale;
        modelRotationYNetwork.Value = rotationY;
    }

    [ServerRpc]
    private void SetVisibilityServerRpc(bool isVisible)
    {
        isVisibleNetwork.Value = isVisible;
    }

    IEnumerator UploadAndLoad(string filePath)
    {
        byte[] fileData = File.ReadAllBytes(filePath);
        WWWForm form = new WWWForm();
        string extension = Path.GetExtension(filePath);
        string safeFileName = System.Guid.NewGuid().ToString() + extension;
        form.AddBinaryData("file", fileData, safeFileName, "model/gltf-binary");

        using (UnityWebRequest www = UnityWebRequest.Post(ConnectionManager.Instance.serverUrl + "/upload", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Upload failed: {www.error}");
            }
            else
            {
                string responseText = www.downloadHandler.text;
                Debug.Log($"Upload successful.");

                string loadUrl = "";
                try 
                {
                    var responseJson = JsonUtility.FromJson<UploadResponse>(responseText);
                    if (responseJson != null && !string.IsNullOrEmpty(responseJson.url))
                    {
                        loadUrl = responseJson.url;
                    }
                }
                catch (Exception) { }

                if (string.IsNullOrEmpty(loadUrl)) loadUrl = responseText.Trim();
                if (!loadUrl.StartsWith("http")) loadUrl = $"{ConnectionManager.Instance.serverUrl}/{loadUrl}"; 
                
                if (loadUrl.Contains("localhost") || loadUrl.Contains("127.0.0.1"))
                {
                    string currentServerUrl = ConnectionManager.Instance.serverUrl;
                    if (!currentServerUrl.Contains("localhost") && !currentServerUrl.Contains("127.0.0.1"))
                    {
                        try 
                        {
                            Uri serverUri = new Uri(currentServerUrl);
                            Uri loadUri = new Uri(loadUrl);
                            loadUrl = loadUrl.Replace(loadUri.Host, serverUri.Host);
                            Debug.Log($"[Avatar] Replaced localhost with {serverUri.Host} -> {loadUrl}");
                        }
                        catch(Exception e) { Debug.LogError($"[Avatar] Failed to replace localhost: {e.Message}"); }
                    }
                }

                SetAvatarUrlServerRpc(loadUrl);
            }
        }
    }

    [System.Serializable]
    private class UploadResponse
    {
        public string url;
    }

    private GameObject modelContainer;

    [SerializeField] private Transform customModelParent;
    [SerializeField] private Shader urpLitShader;

    private GltfImport currentLoader;
    
    public async Task LoadAvatar(string url)
    {
        if (IsOwner && !ConnectionManager.Instance.summonAvatar)
        {
            Debug.Log("[Avatar] Summoning is disabled. Aborting LoadAvatar.");
            return;
        }

        if (currentLoader != null)
        {
            currentLoader.Dispose();
            currentLoader = null;
        }

        var fallbackShader = urpLitShader ?? Shader.Find("Universal Render Pipeline/Lit");
        var materialGenerator = new CustomMaterialGenerator(fallbackShader);
        var logger = new ConsoleLogger();

        var settings = new ImportSettings {
            GenerateMipMaps = true,
            AnisotropicFilterLevel = 3,
            NodeNameMethod = NameImportMethod.Original
        };

        currentLoader = new GltfImport(null, null, materialGenerator, logger);
        var success = await currentLoader.Load(url, settings);

        if (success)
        {
            if (modelContainer != null) Destroy(modelContainer);
            
            modelContainer = new GameObject("ModelContainer");

            if (customModelParent != null) modelContainer.transform.SetParent(customModelParent, false);
            else modelContainer.transform.SetParent(transform, false);
            
            GameObject geometryContainer = new GameObject("GeometryContainer");
            geometryContainer.transform.SetParent(modelContainer.transform, false);

            var successInstantiate = await currentLoader.InstantiateMainSceneAsync(geometryContainer.transform);
            if (!successInstantiate)
            {
                Debug.LogError($"[Avatar] Instantiate failed: {url}");
                return;
            }
            
            Debug.Log($"[Avatar] InstantiateMainSceneAsync completed. GeometryContainer children: {geometryContainer.transform.childCount}");
            
            Bounds bounds = new Bounds(geometryContainer.transform.position, Vector3.zero);
            bool hasBounds = false;
            
            foreach (var renderer in geometryContainer.GetComponentsInChildren<Renderer>())
            {
                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (hasBounds)
            {
                // Align bottom to Y=0 and center on X/Z
                Vector3 currentCenter = bounds.center;
                float currentMinY = bounds.min.y;
                
                // We want: geometry center (X, Z) to be at (0, 0)
                // We want: geometry bottom (Y) to be at 0
                Vector3 shift = new Vector3(-currentCenter.x, -currentMinY, -currentCenter.z);
                geometryContainer.transform.localPosition = shift;
            }

            UpdateModelTransform();
            UpdateLocalVisibility();
            Debug.Log($"Avatar loaded successfully: {url}");
        }
        else
        {
            Debug.LogError($"Avatar load failed: {url}");
        }
    }


    private void Update()
    {
        if (IsOwner)
        {
            if (transform.position.y <= -100f)
            {
                Respawn();
            }
        }
    }

    public void Respawn()
    {
        transform.position = initialPosition;
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        Debug.Log("[Avatar] Respawned.");
    }

    public override void OnDestroy()
    {
        if (currentLoader != null)
        {
            currentLoader.Dispose();
            currentLoader = null;
        }
        base.OnDestroy();
    }
}
