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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        avatarUrlNetwork.OnValueChanged += OnAvatarUrlChanged;

        // If there's already a URL set when we spawn (e.g. late join), load it
        if (!avatarUrlNetwork.Value.IsEmpty)
        {
            _ = LoadAvatar(avatarUrlNetwork.Value.ToString());
        }

        if (IsOwner)
        {

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
                SetAvatarUrlServerRpc(ConnectionManager.Instance.serverUrl + "/default_avatar.glb"); // Example fallback or just keep local if intended.
                // Actually, let's stick to the previous logic but via RPC
                 SetAvatarUrlServerRpc(ConnectionManager.Instance.serverUrl + "/default"); // Simplified for now, assuming server handles it or just empty
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
        var success = await gltf.Load(url);

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
}
