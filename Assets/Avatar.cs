using UnityEngine;
using Unity.Netcode;
using UnityEngine.Networking;
using Unity.Collections; // FixedString用
using GLTFast;
using System.Collections;
using System.IO;

public class Avatar : NetworkBehaviour
{
    [Header("Settings")]
    // Node.jsサーバーのアドレス
    [SerializeField] private string uploadEndpoint = "http://localhost:3000/upload";
    
    // ロード中に表示する箱（ProjectウィンドウからPrefabをアセット）
    [SerializeField] private GameObject loadingPlaceholderPrefab;

    // 同期変数: アバターのURL (512バイトまでの文字列)
    // PermissionをServerOnlyにすることで、書き換えをServerRpc経由に強制し安全性を確保
    public NetworkVariable<FixedString512Bytes> avatarUrl = new NetworkVariable<FixedString512Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    // 内部参照用
    private GameObject currentModel;
    private GameObject activePlaceholder;

    // ---------------------------------------------------------
    // 1. 初期化と同期フック
    // ---------------------------------------------------------
    public override void OnNetworkSpawn()
    {
        // URL変数が変わったら、全クライアントでロード処理を実行
        avatarUrl.OnValueChanged += (prev, current) =>
        {
            string url = current.ToString();
            if (!string.IsNullOrEmpty(url))
            {
                StartCoroutine(LoadAvatarProcess(url));
            }
        };

        // 途中参加時、すでにURLが入っていたらロード
        if (!string.IsNullOrEmpty(avatarUrl.Value.ToString()))
        {
            StartCoroutine(LoadAvatarProcess(avatarUrl.Value.ToString()));
        }
    }

    // ---------------------------------------------------------
    // 2. ドラッグ＆ドロップ検知 (外部アセットのイベントからこの関数を呼ぶ)
    // ---------------------------------------------------------
    public void OnGlbFileDropped(string filePath)
    {
        // 自分のキャラでなければ無視
        if (!IsOwner) return;

        // アップロード開始
        StartCoroutine(UploadFileCoroutine(filePath));
    }

    // ---------------------------------------------------------
    // 3. アップロード処理 (HTTP)
    // ---------------------------------------------------------
    private IEnumerator UploadFileCoroutine(string path)
    {
        Debug.Log($"Uploading: {path} ...");

        byte[] fileData = File.ReadAllBytes(path);
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", fileData, Path.GetFileName(path), "model/gltf-binary");

        using (UnityWebRequest www = UnityWebRequest.Post(uploadEndpoint, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                // JSON解析してURLを取り出す
                var response = JsonUtility.FromJson<ServerResponse>(www.downloadHandler.text);
                Debug.Log($"Upload Success! URL: {response.url}");

                // サーバーに「URL変わったよ」と通知
                RequestAvatarChangeServerRpc(response.url);
            }
            else
            {
                Debug.LogError($"Upload Error: {www.error}");
            }
        }
    }

    // JSON受け取り用クラス
    [System.Serializable]
    private class ServerResponse { public string url; }

    // ---------------------------------------------------------
    // 4. サーバーへの通知 (UDP - ServerRpc)
    // ---------------------------------------------------------
    [ServerRpc]
    private void RequestAvatarChangeServerRpc(string newUrl)
    {
        // サーバーは変数を書き換えるだけ。
        // 重い処理は一切しない。
        avatarUrl.Value = newUrl;
        Debug.Log($"[Dedicated Server] Syncing new URL: {newUrl}");
    }

    // ---------------------------------------------------------
    // 5. モデルのロード処理 (非同期・クライアントのみ)
    // ---------------------------------------------------------
    private IEnumerator LoadAvatarProcess(string url)
    {
        // 【重要】Dedicated Server (画面なし) ならロード処理を即中断
        // これによりサーバーのメモリとCPUを守る
        if (IsServer && !IsClient) yield break;

        // --- 以下は各プレイヤーPCでの処理 ---

        Debug.Log($"Start Loading: {url}");

        // 古いモデルとプレースホルダーの削除
        if (currentModel != null) Destroy(currentModel);
        if (activePlaceholder != null) Destroy(activePlaceholder);

        // A. プレースホルダー（箱）を出す
        if (loadingPlaceholderPrefab != null)
        {
            activePlaceholder = Instantiate(loadingPlaceholderPrefab, transform);
            activePlaceholder.transform.localPosition = Vector3.zero;
        }

        // B. glTFastで非同期ダウンロード
        var gltf = new GltfImport();
        var task = gltf.Load(url);

        // 【重要】タスク完了まで待機 (WaitUntil)
        // ここでメインスレッドをブロックしないため、
        // 回線が遅い人がダウンロード中でも、他の人は普通にゲームが動く
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Result) // 成功
        {
            // プレースホルダーを消す
            if (activePlaceholder != null) Destroy(activePlaceholder);

            // モデル生成
            var instantiator = new GameObjectInstantiator(gltf, transform);
            var success = gltf.InstantiateMainScene(instantiator);

            // 生成されたオブジェクトを取得 (transformの最後の子要素と仮定)
            if (transform.childCount > 0)
            {
                currentModel = transform.GetChild(transform.childCount - 1).gameObject;
                // 位置合わせ
                currentModel.transform.localPosition = Vector3.zero;
                currentModel.transform.localRotation = Quaternion.identity;
            }
        }
        else
        {
            Debug.LogError($"Failed to load model from {url}");
        }
    }
}