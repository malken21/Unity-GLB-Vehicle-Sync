using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(3, 2, 0);
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private float rotationSmoothSpeed = 5f;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Position Follow
        Vector3 desiredPosition = target.position + target.TransformDirection(offset);
        // Using Lerp for position
        // Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        // For physics based movement or avoiding jitter, sometimes simple MoveTowards or strictly setting relative pos is better if simple. 
        // But let's try a standard smooth follow that respects rotation.
        
        // Actually, simple 3rd person follow usually keeps the camera behind the player.
        // Let's assume the user wants the camera to stay behind the avatar.
        
        // Calculate desired position based on target's rotation
        Vector3 finalPosition = target.position + (target.rotation * offset);

        transform.position = Vector3.Lerp(transform.position, finalPosition, smoothSpeed * Time.deltaTime);

        // Rotation Follow
        // Look at the target, maybe slightly above center?
        // simple LookAt
        // transform.LookAt(target.position + Vector3.up * 1.5f);
        
        // Or smooth look at
        Quaternion targetRotation = Quaternion.LookRotation(target.position + Vector3.up * 1.5f - transform.position);
         transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
    }
}
