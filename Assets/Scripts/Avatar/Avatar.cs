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
    // ネットワーク上で同期されるアバターのURLを保存します
    private readonly NetworkVariable<FixedString512Bytes> avatarUrlNetwork = new NetworkVariable<FixedString512Bytes>();
    private readonly NetworkVariable<float> modelScaleNetwork = new NetworkVariable<float>(1.0f);
    private readonly NetworkVariable<float> modelRotationYNetwork = new NetworkVariable<float>(0.0f);
    
    // アバターの可視性を同期します
    private readonly NetworkVariable<bool> isVisibleNetwork = new NetworkVariable<bool>(true);

    // 他プレイヤーを非表示にする設定（ローカルのみ）
    public static bool s_hideOtherPlayers = false;

    private Vector3 initialPosition;
    
    // 外部からのアクセス用プロパティ
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

        // スポーン時にすでにURLが設定されている場合（例：途中参加）、それを読み込みます
        // 【修正】召喚が無効な場合は読み込みをスキップ
        if (!avatarUrlNetwork.Value.IsEmpty && ConnectionManager.Instance.summonAvatar)
        {
            _ = LoadAvatar(avatarUrlNetwork.Value.ToString());
        }

        // 初期可視性の適用
        UpdateLocalVisibility();

        if (IsOwner)
        {
            // 動的にInputHandlerとCameraControllerを追加
            if (GetComponent<AvatarInputHandler>() == null) gameObject.AddComponent<AvatarInputHandler>();
            if (GetComponent<AvatarCameraController>() == null) gameObject.AddComponent<AvatarCameraController>();

            if (ConnectionManager.Instance.summonAvatar)
            {
                // メインスレッドで実行します
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
                // 俯瞰（オーバーヘッド）ビューモード
                Debug.Log("[Avatar] Summoning disabled. Informing server of invisibility.");
                SetVisibilityServerRpc(false);
            }
        }
        
        // 観戦者や他プレイヤー操作を無効にする
        if (!IsOwner || !ConnectionManager.Instance.summonAvatar)
        {
             var movement = GetComponent<AvatarMovementController>();
             if (movement != null) movement.enabled = false;
        }

        // 所有者の場合に Microbit コントローラーを初期化
        if (IsOwner)
        {
            // シーン内に BLE マネージャーが存在することを確認
            // (通常は別のオブジェクトとして存在しているはずだが、必須の場合生成)
            if (MicrobitBLEManager.Instance == null && ConnectionManager.Instance.enableMicrobit)
            {
                var mgrGo = new GameObject("MicrobitBLEManager");
                mgrGo.AddComponent<MicrobitBLEManager>();
                Debug.Log("[Avatar] MicrobitBLEManager シングルトンを作成しました。");
            }

            // ムーブメントコントローラー（マイクロビット/キーボード統合）がアタッチされていない場合は追加
            var movementController = GetComponent<AvatarMovementController>();
            if (movementController == null)
            {
                movementController = gameObject.AddComponent<AvatarMovementController>();
                Debug.Log("[Avatar] AvatarMovementController (Microbit/Keyboard) を追加しました。");
            }

            // カラー制御コントローラーがアタッチされていない場合は追加
            if (GetComponent<AvatarColorController>() == null) gameObject.AddComponent<AvatarColorController>();
            if (GetComponent<AvatarColorKeyboardInput>() == null) gameObject.AddComponent<AvatarColorKeyboardInput>();
            if (GetComponent<AvatarColorMicrobitInput>() == null) gameObject.AddComponent<AvatarColorMicrobitInput>();
            
            // マイクロビット設定が無効な場合は、マイクロビットからの受信を停止（オプション）
            // ここでは単にコントローラーを追加するだけに留める（キーボード操作のため）
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

        // 【修正】Rendererのenabledをいじるのではなく、モデルコンテナ自体のSetActiveを使うと
        // GLBの内部的な非表示設定を壊さずに済みます。
        if (modelContainer != null)
        {
             modelContainer.SetActive(targetVisibility);
        }

        // Colliderは自身のものを切り替えます
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
    
    // 外部のInputHandler等から呼ばれるメソッド
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
                catch (Exception) { /* json parse error fallback */ }

                if (string.IsNullOrEmpty(loadUrl)) loadUrl = responseText.Trim();
                if (!loadUrl.StartsWith("http")) loadUrl = $"{ConnectionManager.Instance.serverUrl}/{loadUrl}"; 
                
                // localhost の置換ロジック
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

    [SerializeField] private Transform customModelParent; // ユーザー指定の親オブジェクト
    [SerializeField] private Shader urpLitShader; // ビルド時にシェーダーが含まれるように参照を保持

    private GltfImport currentLoader;
    public async Task LoadAvatar(string url)
    {
        // 【修正】所有者かつ召喚が無効な場合は読み込みを拒否
        if (IsOwner && !ConnectionManager.Instance.summonAvatar)
        {
            Debug.Log("[Avatar] Summoning is disabled. Aborting LoadAvatar.");
            return;
        }

        // 以前のローダーを破棄します
        if (currentLoader != null)
        {
            currentLoader.Dispose();
            currentLoader = null;
        }

        currentLoader = new GltfImport();
        var success = await currentLoader.Load(url);

        if (success)
        {
            if (modelContainer != null) Destroy(modelContainer);
            
            modelContainer = new GameObject("ModelContainer");

            if (customModelParent != null) modelContainer.transform.SetParent(customModelParent, false);
            else modelContainer.transform.SetParent(transform, false);
            
            GameObject geometryContainer = new GameObject("GeometryContainer");
            geometryContainer.transform.SetParent(modelContainer.transform, false);

            await currentLoader.InstantiateMainSceneAsync(geometryContainer.transform);
            
            ApplyShaderFallback(geometryContainer);

            // メッシュの底面がコンテナの原点（ピボット）に来るように位置を調整します
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
                float currentMinY = bounds.min.y;
                float targetY = modelContainer.transform.position.y;
                float shiftY = targetY - currentMinY;
                geometryContainer.transform.position += new Vector3(0, shiftY, 0);
            }

            UpdateModelTransform();
            UpdateLocalVisibility();
            Debug.Log($"アバターを正常に読み込みました: {url}");
        }
        else
        {
            Debug.LogError($"アバターの読み込みに失敗しました: {url}");
        }
    }

    private void ApplyShaderFallback(GameObject container)
    {
        Shader fallbackShader = urpLitShader ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        if (fallbackShader == null)
        {
            Debug.LogError("[Avatar] 適切なフォールバックシェーダーが見つかりませんでした。");
            return;
        }

        Renderer[] renderers = container.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            bool modified = false;

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null || materials[i].shader == null || materials[i].shader.name == "Hidden/InternalErrorShader" || materials[i].shader.name == "")
                {
                    Texture mainTexture = null;
                    Color baseColor = Color.white;

                    Material oldMat = materials[i];
                    if (oldMat != null)
                    {
                        if (mainTexture == null) mainTexture = oldMat.GetTexture("baseColorTexture");
                        if (mainTexture == null) mainTexture = oldMat.GetTexture("_BaseMap");
                        if (mainTexture == null) mainTexture = oldMat.GetTexture("_MainTex");
                        if (mainTexture == null) mainTexture = oldMat.GetTexture("_BaseColorMap");

                        if (oldMat.HasProperty("_BaseColor")) baseColor = oldMat.GetColor("_BaseColor");
                        else if (oldMat.HasProperty("_Color")) baseColor = oldMat.GetColor("_Color");
                        else if (oldMat.HasProperty("baseColorFactor")) baseColor = oldMat.GetColor("baseColorFactor");
                    }

                    Material newMat = new Material(fallbackShader);
                    
                    if (fallbackShader.name.Contains("URP") || fallbackShader.name.Contains("Universal Render Pipeline"))
                    {
                        if (mainTexture != null) newMat.SetTexture("_BaseMap", mainTexture);
                        if (mainTexture != null) newMat.SetTexture("_MainTex", mainTexture);
                        newMat.SetColor("_BaseColor", baseColor);
                        newMat.SetColor("_Color", baseColor);
                    }
                    else
                    {
                        if (mainTexture != null) newMat.SetTexture("_MainTex", mainTexture);
                        newMat.SetColor("_Color", baseColor);
                    }

                    materials[i] = newMat;
                    modified = true;
                }
            }

            if (modified) renderer.sharedMaterials = materials;
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
