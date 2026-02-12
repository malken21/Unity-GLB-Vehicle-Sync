using UnityEngine;

public class KeepHoriz : MonoBehaviour
{


    void LateUpdate()
    {
        // ワールド空間でオブジェクトを水平に保ちます
        Vector3 forward = transform.forward;
        forward.y = 0;
        
        // 真上や真下を見ている場合は、以前の回転を維持するかデフォルトで前方にします
        if (forward.sqrMagnitude < 0.001f)
        {
             forward = Vector3.forward;
        }

        // World Upに合わせて回転を設定し、Y回転（ヨー）を維持します
        transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
    }
}
