using System;
using System.IO;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using GLTFast;

using Unity.Netcode;
using Unity.Collections;

public class Avatar : NetworkBehaviour
{

    // Stores the avatar URL synchronized across the network
    private readonly NetworkVariable<FixedString512Bytes> avatarUrlNetwork = new NetworkVariable<FixedString512Bytes>();

    private Vector3 initialPosition;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        initialPosition = transform.position;

        avatarUrlNetwork.OnValueChanged += OnAvatarUrlChanged;

        // If there's already a URL set when we spawn (e.g. late join), load it
        if (!avatarUrlNetwork.Value.IsEmpty)
        {
            _ = LoadAvatar(avatarUrlNetwork.Value.ToString());
        }

        if (IsOwner)
        {
            if (ConnectionManager.Instance.summonAvatar)
            {
                // Camera tracking logic
                if (Camera.main != null)
                {
                    var cameraTransform = Camera.main.transform;
                    
                    // Find or create "Horiz" child for stable camera tracking
                    var horizTransform = transform.Find("Horiz");
                    if (horizTransform == null)
                    {
                        var horizGO = new GameObject("Horiz");
                        horizTransform = horizGO.transform;
                        horizTransform.SetParent(transform, false);
                        horizGO.AddComponent<KeepHoriz>();
                        Debug.Log("[Avatar] Created 'Horiz' child with KeepHoriz script.");
                    }

                    cameraTransform.SetParent(horizTransform);
                    // Adjust position behind and slightly above the avatar
                    // User reported side view with previous settings, implying model faces X-axis.
                    // Moving camera to -X to look at the back of an X-forward model.
                    cameraTransform.localPosition = new Vector3(8f, 3f, 0f); 
                    cameraTransform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                    Debug.Log($"[Avatar] Main Camera attached to {horizTransform.name} with X-axis alignment.");
                }
                else
                {
                     Debug.LogWarning("[Avatar] Main Camera not found!");
                }

                // Execute on main thread
                var filePath = WindowsFileDialog.Open("GLB Files (*.glb)|*.glb", "Select Avatar");
                if (!string.IsNullOrEmpty(filePath))
                {
                    StartCoroutine(UploadAndLoad(filePath));
                }
                else
                {
                    // Fallback or do nothing
                    Debug.Log("No file selected, loading default.");
                    // For default, we also need to sync it if we want others to see it, 
                    // but usually default is a placeholder. attempt to sync default if needed.
                    // For now, let's just sync the default URL if nothing selected.
                    // Actually, let's stick to the previous logic but via RPC
                     SetAvatarUrlServerRpc(ConnectionManager.Instance.serverUrl + "/default"); // Simplified for now, assuming server handles it or just empty
                }
            }
            else
            {
                // Overhead view mode
                Debug.Log("[Avatar] Summoning disabled via command line argument. Entering Overhead View Mode.");

                if (Camera.main != null)
                {
                    var cameraTransform = Camera.main.transform;
                    cameraTransform.SetParent(null);
                    cameraTransform.position = new Vector3(0f, 50f, 0f);
                    cameraTransform.rotation = Quaternion.Euler(90f, 0f, 0f);
                }

                var rotator = GetComponent<KeyboardRotator>();
                if (rotator != null)
                {
                    rotator.enabled = false;
                }
            }
        }
        else
        {
            // Disable input controls for non-owners to prevent controlling other players
            var rotator = GetComponent<KeyboardRotator>();
            if (rotator != null)
            {
                rotator.enabled = false;
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

    [ServerRpc]
    private void SetAvatarUrlServerRpc(string url)
    {
        avatarUrlNetwork.Value = new FixedString512Bytes(url);
    }

    IEnumerator UploadAndLoad(string filePath)
    {
        byte[] fileData = File.ReadAllBytes(filePath);
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", fileData, Path.GetFileName(filePath), "model/gltf-binary");

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
                Debug.Log($"Upload successful. Response: {responseText}");

                string loadUrl = "";
                try 
                {
                    // Attempt to parse as JSON
                    var responseJson = JsonUtility.FromJson<UploadResponse>(responseText);
                    if (responseJson != null && !string.IsNullOrEmpty(responseJson.url))
                    {
                        loadUrl = responseJson.url;
                    }
                }
                catch (Exception)
                {
                    // Fallback to raw text if not JSON or parsing fails
                }

                if (string.IsNullOrEmpty(loadUrl))
                {
                     loadUrl = responseText.Trim();
                }

                 if (!loadUrl.StartsWith("http"))
                {
                     loadUrl = $"{ConnectionManager.Instance.serverUrl}/{loadUrl}"; 
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

    [SerializeField] private Transform customModelParent; // User specified parent

    public async Task LoadAvatar(string url)
    {
        var gltf = new GltfImport();
        var settings = new ImportSettings
        {
            generateMipMaps = true,
            anisotropicFilterLevel = 3
        };
        var success = await gltf.Load(url, settings);

        if (success)
        {
            // Clean up previous container
            if (modelContainer != null)
            {
                Destroy(modelContainer);
            }
            
            // Create a new container for the model
            modelContainer = new GameObject("ModelContainer");

            // Set parent based on configuration
            if (customModelParent != null)
            {
                modelContainer.transform.SetParent(customModelParent, false);
            }
            else
            {
                modelContainer.transform.SetParent(transform, false);
            }

            // Instantiate into the container
            await gltf.InstantiateMainSceneAsync(modelContainer.transform);
            
            // Adjust position so the bottom of the mesh is at the origin (pivot) of the container
            Bounds bounds = new Bounds(modelContainer.transform.position, Vector3.zero);
            bool hasBounds = false;
            
            // Calculate bounds from renderers inside the container
            foreach (var renderer in modelContainer.GetComponentsInChildren<Renderer>())
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
                float currentMinY = bounds.min.y;
                float targetY = modelContainer.transform.position.y;
                float shiftY = targetY - currentMinY;

                // Move the container to adjust height
                modelContainer.transform.position += new Vector3(0, shiftY, 0);
            }

            Debug.Log($"Avatar loaded successfully from {url}");
        }
        else
        {
            Debug.LogError($"Loading avatar failed from {url}");
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

    private void Respawn()
    {
        transform.position = initialPosition;
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        Debug.Log("[Avatar] Respawned due to falling below -100Y.");
    }
}
