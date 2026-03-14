using UnityEngine;
using Unity.Netcode;

public class AvatarCameraController : NetworkBehaviour
{
    private Vector3 _initialCameraPosition;
    private Quaternion _initialCameraRotation;
    private bool _hasInitialTransform;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner) return;

        SaveInitialCameraTransform();

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
        followCam.enabled = true;

        Transform horizTransform = null;
        foreach (var child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "Horiz")
            {
                horizTransform = child;
                break;
            }
        }
        horizTransform = horizTransform ?? transform;
        followCam.SetTarget(horizTransform);

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
            followCam.enabled = false;
        }

        var cameraTransform = Camera.main.transform;
        if (_hasInitialTransform)
        {
            cameraTransform.position = _initialCameraPosition;
            cameraTransform.rotation = _initialCameraRotation;
            Debug.Log($"[AvatarCamera] Camera restored to initial position: {_initialCameraPosition}");
        }
        else
        {
            Debug.LogWarning("[AvatarCamera] Initial camera transform not saved. Cannot restore.");
        }
    }

    private void SaveInitialCameraTransform()
    {
        if (Camera.main != null && !_hasInitialTransform)
        {
            _initialCameraPosition = Camera.main.transform.position;
            _initialCameraRotation = Camera.main.transform.rotation;
            _hasInitialTransform = true;
            Debug.Log($"[AvatarCamera] Initial camera transform saved: {_initialCameraPosition}");
        }
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
