using UnityEngine;
using Unity.Netcode;

public class AvatarCameraController : NetworkBehaviour
{
    private static readonly Vector3 OverheadPosition = new Vector3(0f, 50f, 0f);
    private static readonly Quaternion OverheadRotation = Quaternion.Euler(90f, 0f, 0f);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner) return;

        bool summonAvatar = ConnectionManager.Instance != null && ConnectionManager.Instance.summonAvatar;

        if (summonAvatar)
        {
            SetupFollowCamera();
        }
        else
        {
            SetupOverheadCamera();
        }
    }

    private void SetupFollowCamera()
    {
        if (Camera.main == null)
        {
            Debug.LogWarning("[AvatarCamera] Main Camera not found!");
            return;
        }

        var followCam = Camera.main.GetComponent<AvatarFollowCamera>();
        if (followCam == null)
        {
            followCam = Camera.main.gameObject.AddComponent<AvatarFollowCamera>();
        }

        var horizTransform = transform.Find("Horiz") ?? transform;
        followCam.SetTarget(horizTransform);

        Camera.main.transform.SetParent(null);

        Debug.Log($"[AvatarCamera] AvatarFollowCamera target set to: {horizTransform.name}");
    }

    private void SetupOverheadCamera()
    {
        Debug.Log("[AvatarCamera] Summoning disabled. Setting up Overhead Camera.");

        if (Camera.main == null) return;

        var followCam = Camera.main.GetComponent<AvatarFollowCamera>();
        if (followCam != null)
        {
            followCam.ClearTarget();
        }

        var cameraTransform = Camera.main.transform;
        cameraTransform.SetParent(null);
        cameraTransform.position = OverheadPosition;
        cameraTransform.rotation = OverheadRotation;
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && Camera.main != null)
        {
            var followCam = Camera.main.GetComponent<AvatarFollowCamera>();
            if (followCam != null)
            {
                followCam.ClearTarget();
            }

            if (Camera.main.transform.IsChildOf(transform))
            {
                Camera.main.transform.SetParent(null);
            }
        }
        base.OnNetworkDespawn();
    }
}
