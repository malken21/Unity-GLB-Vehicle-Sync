using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Camera))]
public class AvatarFollowCamera : MonoBehaviour
{
    [SerializeField] private float followDistance = 8f;
    [SerializeField] private float heightOffset = 3f;
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

        Vector3 desiredPosition = _horizTarget.position
            - _horizTarget.forward * followDistance
            + Vector3.up * heightOffset;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

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

    public void SetTarget(Transform horiz)
    {
        _horizTarget = horiz;
        Debug.Log($"[AvatarFollowCamera] Target set to: {horiz?.name ?? "null"}");
    }

    public void ClearTarget()
    {
        _horizTarget = null;
    }
}
