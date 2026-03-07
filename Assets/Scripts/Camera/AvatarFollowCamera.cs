using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Main Camera に直接アタッチして、ローカルオーナーのアバターを後方から追従するスクリプト。
/// アバターの Horiz 子オブジェクトを基準にカメラ位置・向きを計算する。
/// カメラはアバターの子にならないため、アバター破棄時も消失しない。
/// </summary>
[RequireComponent(typeof(Camera))]
public class AvatarFollowCamera : MonoBehaviour
{
    [Tooltip("アバター後方への距離")]
    [SerializeField] private float followDistance = 8f;

    [Tooltip("アバター基準の高さオフセット")]
    [SerializeField] private float heightOffset = 3f;

    [Tooltip("カメラ追従のスムーズ量（高いほど素早く追従）")]
    [SerializeField] private float smoothSpeed = 8f;

    private Transform _horizTarget;
    private float _searchTimer = 0f;
    private const float SearchInterval = 0.5f;

    private void Start()
    {
        Debug.Log("[AvatarFollowCamera] Start() called. Camera is active.");
    }

    private void LateUpdate()
    {
        if (_horizTarget == null)
        {
            _searchTimer += Time.deltaTime;
            if (_searchTimer >= SearchInterval)
            {
                _searchTimer = 0f;
                TryFindHorizTarget();
            }
            return;
        }

        // 三人称後方視点: Horiz の真後ろ・少し上に配置
        Vector3 desiredPosition = _horizTarget.position
            - _horizTarget.forward * followDistance
            + Vector3.up * heightOffset;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // アバターの少し上を見る
        Vector3 lookTarget = _horizTarget.position + Vector3.up * (heightOffset * 0.3f);
        Quaternion desiredRotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, smoothSpeed * Time.deltaTime);
    }

    private void TryFindHorizTarget()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.Log("[AvatarFollowCamera] NetworkManager not found.");
            return;
        }

        if (!NetworkManager.Singleton.IsConnectedClient && !NetworkManager.Singleton.IsHost)
        {
            Debug.Log("[AvatarFollowCamera] Not connected yet.");
            return;
        }

        Debug.Log("[AvatarFollowCamera] Searching for local avatar...");

        // シーン内の全アバターから、ローカルオーナーのものを特定する
        foreach (var netObj in FindObjectsByType<NetworkObject>(FindObjectsSortMode.None))
        {
            if (netObj.IsOwner)
            {
                var avatar = netObj.GetComponent<Avatar>();
                if (avatar != null)
                {
                    var horiz = netObj.transform.Find("Horiz");
                    if (horiz != null)
                    {
                        _horizTarget = horiz;
                        Debug.Log($"[AvatarFollowCamera] Target 'Horiz' found on {netObj.name}.");
                    }
                    else
                    {
                        _horizTarget = netObj.transform;
                        Debug.LogWarning("[AvatarFollowCamera] 'Horiz' not found. Using avatar root.");
                    }
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 外部（AvatarCameraController 等）からターゲットを明示的に設定する
    /// </summary>
    public void SetTarget(Transform horiz)
    {
        _horizTarget = horiz;
        Debug.Log($"[AvatarFollowCamera] Target set to: {horiz?.name ?? "null"}");
    }

    /// <summary>
    /// ターゲットのクリア（俯瞰モードへの切り替え時等に使用）
    /// </summary>
    public void ClearTarget()
    {
        _horizTarget = null;
    }
}
