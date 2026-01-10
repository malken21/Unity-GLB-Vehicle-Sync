using System;
using System.IO;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using GLTFast;

public class Avatar : MonoBehaviour
{
    public string serverUrl = "http://localhost:3000";
    public string avatarUrl = "https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/Duck/glTF/Duck.gltf";

    void Start()
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
            _ = LoadAvatar(avatarUrl);
        }
    }

    IEnumerator UploadAndLoad(string filePath)
    {
        byte[] fileData = File.ReadAllBytes(filePath);
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", fileData, Path.GetFileName(filePath), "model/gltf-binary");

        using (UnityWebRequest www = UnityWebRequest.Post(serverUrl + "/upload", form))
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
                     loadUrl = $"{serverUrl}/{loadUrl}"; 
                }
                
                 _ = LoadAvatar(loadUrl);
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
