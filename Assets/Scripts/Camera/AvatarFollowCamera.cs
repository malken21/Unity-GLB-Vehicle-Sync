using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Camera))]
public class AvatarFollowCamera : MonoBehaviour
{
    [SerializeField] private float followDistance = 8f;
    [SerializeField] private float heightOffset = 3f;

    private Transform _horizTarget;
    private float _searchTimer = 0f;
    private const float SearchInterval = 0.5f;

    private void Start()
    {
        Debug.Log("[AvatarFollowCamera] Start() called. Camera is active.");
    }

    private void Update()
    {
        if (_horizTarget == null && ConnectionManager.Instance != null && ConnectionManager.Instance.summonAvatar)
        {
            _searchTimer += Time.deltaTime;
            if (_searchTimer >= SearchInterval)
            {
                _searchTimer = 0f;
                TryFindHorizTarget();
            }
        }
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

        foreach (var netObj in FindObjectsByType<NetworkObject>(FindObjectsSortMode.None))
        {
            if (netObj.IsOwner)
            {
                var avatar = netObj.GetComponent<Avatar>();
                if (avatar != null)
                {
                    Transform horiz = null;
                    foreach (var child in netObj.GetComponentsInChildren<Transform>(true))
                    {
                        if (child.name == "Horiz")
                        {
                            horiz = child;
                            break;
                        }
                    }

                    if (horiz != null)
                    {
                        SetTarget(horiz);
                        Debug.Log($"[AvatarFollowCamera] Target 'Horiz' found on {netObj.name}.");
                    }
                    else
                    {
                        SetTarget(netObj.transform);
                        Debug.LogWarning("[AvatarFollowCamera] 'Horiz' not found. Using avatar root.");
                    }
                    break;
                }
            }
        }
    }

    public void SetTarget(Transform horiz)
    {
        _horizTarget = horiz;
        if (horiz != null)
        {
            if (transform.parent != horiz)
            {
                transform.SetParent(horiz);
                // 親が設定された直後のみローカル座標と回転を初期化する
                transform.localPosition = new Vector3(0, heightOffset, -followDistance);
                transform.localRotation = Quaternion.identity;
            }
            // 常にターゲットを注視するようにしておく（Start時の位置調整用）
            transform.LookAt(horiz);
        }
        else
        {
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
        }
        Debug.Log($"[AvatarFollowCamera] Target set to: {horiz?.name ?? "null"}");
    }

    public void ClearTarget()
    {
        _horizTarget = null;
        transform.SetParent(null);
    }
}
