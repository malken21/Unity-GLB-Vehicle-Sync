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

    private Vector3 initialPosition;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        initialPosition = transform.position;

        avatarUrlNetwork.OnValueChanged += OnAvatarUrlChanged;
        modelScaleNetwork.OnValueChanged += OnModelTransformChanged;
        modelRotationYNetwork.OnValueChanged += OnModelTransformChanged;
        isVisibleNetwork.OnValueChanged += OnVisibilityChanged;

        // スポーン時にすでにURLが設定されている場合（例：途中参加）、それを読み込みます
        if (!avatarUrlNetwork.Value.IsEmpty)
        {
            _ = LoadAvatar(avatarUrlNetwork.Value.ToString());
        }

        // 初期可視性の適用
        UpdateVisibility(isVisibleNetwork.Value);

        if (IsOwner)
        {
            if (ConnectionManager.Instance.summonAvatar)
            {
                // カメラ追跡ロジック
                if (Camera.main != null)
                {
                    var cameraTransform = Camera.main.transform;
                    
                    // 安定したカメラ追跡のために "Horiz" 子オブジェクトを検索または作成します
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
                    // アバターの後ろ、かつ少し上の位置に調整します
                    // 以前の設定では横からの視点になるとの報告があり、モデルがX軸を向いていることを示唆しています。
                    // X軸前方にあるモデルの背面を見るため、カメラを-Xに移動します。
                    cameraTransform.localPosition = new Vector3(8f, 3f, 0f); 
                    cameraTransform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                    Debug.Log($"[Avatar] Main Camera attached to {horizTransform.name} with X-axis alignment.");
                }
                else
                {
                     Debug.LogWarning("[Avatar] Main Camera not found!");
                }

                // メインスレッドで実行します
                var filePath = WindowsFileDialog.Open("GLB Files (*.glb)|*.glb", "Select Avatar");
                if (!string.IsNullOrEmpty(filePath))
                {
                    StartCoroutine(UploadAndLoad(filePath));
                }
                else
                {
                    // フォールバックまたは何もしない
                    Debug.Log("No file selected, loading default.");
                    // デフォルトの場合、他の人に見えるようにするには同期する必要がありますが、
                    // 通常、デフォルトはプレースホルダーです。必要に応じてデフォルトの同期を試みます。
                    // 取りあえず、何も選択されていない場合はデフォルトのURLを同期します。
                    // 実は、以前のロジックのままRPC経由にします
                    SetAvatarUrlServerRpc(ConnectionManager.Instance.serverUrl + "/default"); // サーバーが処理するか空であると仮定し、現時点では簡略化しています
                }
            }
            else
            {
                // 俯瞰（オーバーヘッド）ビューモード
                Debug.Log("[Avatar] Summoning disabled via command line argument. Entering Overhead View Mode.");

                // サーバーに不可視状態を通知します
                SetVisibilityServerRpc(false);

                if (Camera.main != null)
                {
                    // カメラのみ操作し、レンダラーなどはNetworkVariableの変更で処理されます
                    var cameraTransform = Camera.main.transform;
                    // cameraTransform.SetParent(null);
                    // cameraTransform.position = new Vector3(0f, 50f, 0f);
                    // cameraTransform.rotation = Quaternion.Euler(90f, 0f, 0f);
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
            // 他のプレイヤーを操作できないように、所有者以外からの入力操作を無効にします
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

    private void OnModelTransformChanged(float previousValue, float newValue)
    {
        UpdateModelTransform();
    }

    private void OnVisibilityChanged(bool previousValue, bool newValue)
    {
        UpdateVisibility(newValue);
    }

    private void UpdateVisibility(bool isVisible)
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true)) // include inactive incase they were disabled
        {
            r.enabled = isVisible;
        }
        foreach (var c in GetComponentsInChildren<Collider>(true))
        {
            c.enabled = isVisible;
        }
        Debug.Log($"[Avatar] Visibility updated to: {isVisible}");
    }

    private void UpdateModelTransform()
    {
        if (modelContainer != null)
        {
            modelContainer.transform.localScale = Vector3.one * modelScaleNetwork.Value;
            modelContainer.transform.localRotation = Quaternion.Euler(0, modelRotationYNetwork.Value, 0);
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
                    // JSONとして解析を試みます
                    var responseJson = JsonUtility.FromJson<UploadResponse>(responseText);
                    if (responseJson != null && !string.IsNullOrEmpty(responseJson.url))
                    {
                        loadUrl = responseJson.url;
                    }
                }
                catch (Exception)
                {
                    // JSONでない場合、または解析に失敗した場合は生のテキストにフォールバックします
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

    [SerializeField] private Transform customModelParent; // ユーザー指定の親オブジェクト

    public async Task LoadAvatar(string url)
    {
        using (var gltf = new GltfImport())
        {
            var success = await gltf.Load(url);

            if (success)
            {
                // 以前のコンテナをクリーンアップします
                if (modelContainer != null)
                {
                    Destroy(modelContainer);
                }
                
                // モデル用の新しいコンテナを作成します
                modelContainer = new GameObject("ModelContainer");

                // 設定に基づいて親を設定します
                if (customModelParent != null)
                {
                    modelContainer.transform.SetParent(customModelParent, false);
                }
                else
                {
                    modelContainer.transform.SetParent(transform, false);
                }
                
                // ジオメトリ用のサブコンテナを作成します（ピボット調整用）
                GameObject geometryContainer = new GameObject("GeometryContainer");
                geometryContainer.transform.SetParent(modelContainer.transform, false);

                // サブコンテナ内にインスタンス化します
                await gltf.InstantiateMainSceneAsync(geometryContainer.transform);
                
                // 不足しているマテリアルにシェーダーのフォールバックを適用します
                ApplyShaderFallback(geometryContainer);

                // メッシュの底面がコンテナの原点（ピボット）に来るように位置を調整します
                // 調整は geometryContainer に対して行い、modelContainer は原点のままにします
                Bounds bounds = new Bounds(geometryContainer.transform.position, Vector3.zero);
                bool hasBounds = false;
                
                // コンテナ内のレンダラーから境界（Bounds）を計算します
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
                    // modelContainerの位置（原点）を基準にします
                    float targetY = modelContainer.transform.position.y;
                    float shiftY = targetY - currentMinY;

                    // 高さを調整するためにジオメトリコンテナを移動します
                    geometryContainer.transform.position += new Vector3(0, shiftY, 0);
                }

                // 初期のネットワーク変換を適用します（modelContainerに対して適用されます）
                UpdateModelTransform();

                // 読み込み完了後に現在の可視性設定を適用します
                UpdateVisibility(isVisibleNetwork.Value);

                Debug.Log($"Avatar loaded successfully from {url}");
            }
            else
            {
                Debug.LogError($"Loading avatar failed from {url}");
            }
        }
    }

    private void ApplyShaderFallback(GameObject container)
    {
        Shader fallbackShader = Shader.Find("Universal Render Pipeline/Lit");
        if (fallbackShader == null)
        {
            Debug.LogWarning("[Avatar] フォールバックシェーダー 'Universal Render Pipeline/Lit' が見つかりませんでした。最終手段としてデフォルトの 'Standard' を使用します。");
            fallbackShader = Shader.Find("Standard");
        }

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
                if (materials[i] == null || materials[i].shader == null || materials[i].shader.name == "Hidden/InternalErrorShader" || materials[i].shader.name == "" || materials[i].shader.name.Contains("glTF"))
                {
                    Debug.Log($"[Avatar] フォールバックシェーダーを適用中: {renderer.name} 実行インデックス {i}");
                    
                    // テクスチャと色情報の保持を試みる
                    Texture mainTexture = null;
                    Color baseColor = Color.white;

                    Material oldMat = materials[i];
                    if (oldMat != null)
                    {
                        // 一般的なテクスチャ名を確認 (HasPropertyはErrorShaderの場合falseを返すため、直接取得を試みる)
                        if (mainTexture == null) mainTexture = oldMat.GetTexture("baseColorTexture");
                        if (mainTexture == null) mainTexture = oldMat.GetTexture("_BaseMap");
                        if (mainTexture == null) mainTexture = oldMat.GetTexture("_MainTex");
                        if (mainTexture == null) mainTexture = oldMat.GetTexture("_BaseColorMap");

                        // 一般的な色名を確認
                        if (oldMat.HasProperty("_BaseColor")) baseColor = oldMat.GetColor("_BaseColor");
                        else if (oldMat.HasProperty("_Color")) baseColor = oldMat.GetColor("_Color");
                        else if (oldMat.HasProperty("baseColorFactor")) baseColor = oldMat.GetColor("baseColorFactor");
                        
                        // どうしても見つからない場合のログ
                        if (mainTexture == null)
                        {
                             Debug.LogWarning($"[Avatar] テクスチャが見つかりませんでした: {renderer.name}");
                        }
                    }

                    Material newMat = new Material(fallbackShader);
                    
                    // 新しいシェーダータイプに応じて保持したプロパティを適用
                    if (fallbackShader.name.Contains("Universal Render Pipeline/Lit") || fallbackShader.name.Contains("URP"))
                    {
                        if (mainTexture != null) newMat.SetTexture("_BaseMap", mainTexture);
                        if (mainTexture != null) newMat.SetTexture("_MainTex", mainTexture); // 一部のURPシェーダーは_MainTexも使用する
                        newMat.SetColor("_BaseColor", baseColor);
                        newMat.SetColor("_Color", baseColor); // フォールバック
                    }
                    else // Standard またはその他
                    {
                        if (mainTexture != null) newMat.SetTexture("_MainTex", mainTexture);
                        newMat.SetColor("_Color", baseColor);
                    }

                    materials[i] = newMat;
                    modified = true;
                }
            }

            if (modified)
            {
                renderer.sharedMaterials = materials;
            }
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            // 矢印キーでのスケール（上/下）と回転（左/右）の操作
            HandleManualAdjustments();

            if (transform.position.y <= -100f)
            {
                Respawn();
            }
        }
    }


    private void HandleManualAdjustments()
    {
        if (UnityEngine.InputSystem.Keyboard.current == null) return;

        bool changed = false;
        float currentScale = modelScaleNetwork.Value;
        float currentRotationY = modelRotationYNetwork.Value;

        // スケール：上矢印（拡大+0.1）、下矢印（縮小-0.1）
        if (UnityEngine.InputSystem.Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            currentScale += 0.1f;
            changed = true;
        }
        if (UnityEngine.InputSystem.Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            currentScale -= 0.1f;
            if (currentScale < 0.1f) currentScale = 0.1f;
            changed = true;
        }

        // 回転：左矢印（反時計回り22.5度）、右矢印（時計回り22.5度）
        if (UnityEngine.InputSystem.Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            currentRotationY -= 22.5f;
            changed = true;
        }
        if (UnityEngine.InputSystem.Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            currentRotationY += 22.5f;
            changed = true;
        }

        if (changed)
        {
            UpdateModelTransformServerRpc(currentScale, currentRotationY);
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
