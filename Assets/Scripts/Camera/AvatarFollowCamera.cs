using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Main Camera に直接アタッチして、ローカルオーナーのアバターを追従するスクリプト。
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

    [Tooltip("カメラの追従スムーズ量（低いほど素早い）")]
    [SerializeField] private float smoothSpeed = 10f;

    private Transform _horizTarget;

    private void LateUpdate()
    {
        // まだターゲットが設定されていない場合、ローカルオーナーのアバターを探す
        if (_horizTarget == null)
        {
            TryFindHorizTarget();
            return;
        }

        // Horiz の正面（+forward）がアバターの進行方向なので、その背後に位置する
        Vector3 desiredPosition = _horizTarget.position
            - _horizTarget.forward * followDistance
            + Vector3.up * heightOffset;

        // スムーズに移動
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // アバターの少し上を見る
        Vector3 lookTarget = _horizTarget.position + Vector3.up * (heightOffset * 0.5f);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(lookTarget - transform.position),
            smoothSpeed * Time.deltaTime
        );
    }

    private void TryFindHorizTarget()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient) return;

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
                        // Horiz が存在しなければルート transform を使用
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
