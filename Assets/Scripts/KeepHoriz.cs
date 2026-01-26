using UnityEngine;

public class KeepHoriz : MonoBehaviour
{


    void LateUpdate()
    {
        // Force the object to be horizontal in World Space
        Vector3 forward = transform.forward;
        forward.y = 0;
        
        // If looking straight up/down, keep previous rotation or default to forward
        if (forward.sqrMagnitude < 0.001f)
        {
             forward = Vector3.forward;
        }

        // Set rotation directly to align with World Up, preserving Y rotation (yaw)
        transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
    }
}
