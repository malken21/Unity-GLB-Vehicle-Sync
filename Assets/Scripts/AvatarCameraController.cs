using UnityEngine;
using Unity.Netcode;

/// <summary>
/// アバターに追従するカメラ制御、または俯瞰カメラ制御を担当するクラス。
/// Avatar.cs にあったカメラロジックを分離。
/// </summary>
public class AvatarCameraController : NetworkBehaviour
{
    private Vector3 _overheadPosition = new Vector3(0f, 50f, 0f);
    private Quaternion _overheadRotation = Quaternion.Euler(90f, 0f, 0f);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (!IsOwner) return;

        bool summonAvatar = true;
        
        if (CommandLineParser.Instance != null && !CommandLineParser.Instance.SummonAvatar)
        {
            summonAvatar = false;
        }
        else if (ConnectionManager.Instance != null && !ConnectionManager.Instance.summonAvatar)
        {
            // CommandLineParserがない場合のフォールバック
            summonAvatar = false;
        }

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
                Debug.Log("[AvatarCamera] Created 'Horiz' child with KeepHoriz script.");
            }

            cameraTransform.SetParent(horizTransform);
            // アバターの後ろ、かつ少し上の位置に調整します
            // X軸前方にあるモデルの背面を見るため、カメラを-Xに移動します。
            cameraTransform.localPosition = new Vector3(8f, 3f, 0f); 
            cameraTransform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            Debug.Log($"[AvatarCamera] Main Camera attached to {horizTransform.name} with default alignment.");
        }
        else
        {
            Debug.LogWarning("[AvatarCamera] Main Camera not found!");
        }
    }

    private void SetupOverheadCamera()
    {
        Debug.Log("[AvatarCamera] Summoning disabled. Setting up Overhead Camera.");

        if (Camera.main != null)
        {
            var cameraTransform = Camera.main.transform;
            cameraTransform.SetParent(null);
            cameraTransform.position = _overheadPosition;
            cameraTransform.rotation = _overheadRotation;
        }
    }
}
