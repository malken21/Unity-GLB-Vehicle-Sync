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
            // Setup Camera Follow
            if (Camera.main != null)
            {
                var cameraFollow = Camera.main.gameObject.GetComponent<CameraFollow>();
                if (cameraFollow == null)
                {
                    cameraFollow = Camera.main.gameObject.AddComponent<CameraFollow>();
                }
                cameraFollow.SetTarget(transform);
            }
            else
            {
                Debug.LogWarning("Main Camera not found. Camera follow will not work.");
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

    public async Task LoadAvatar(string url)
    {
        var gltf = new GltfImport();
        var success = await gltf.Load(url);

        if (success)
        {
            await gltf.InstantiateMainSceneAsync(transform);
            Debug.Log($"Avatar loaded successfully from {url}");
        }
        else
        {
            Debug.LogError($"Loading avatar failed from {url}");
        }
    }
}
