using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAspectController : MonoBehaviour
{
    // The target horizontal Field of View in degrees.
    [Tooltip("The fixed horizontal Field of View in degrees.")]
    public float targetHorizontalFOV = 90f;

    private Camera m_Camera;
    private float lastAspect = -1f;

    void Awake()
    {
        m_Camera = GetComponent<Camera>();
        UpdateFOV();
    }

    void Update()
    {
        if (m_Camera != null && !Mathf.Approximately(m_Camera.aspect, lastAspect))
        {
            UpdateFOV();
        }
    }

    private void UpdateFOV()
    {
        lastAspect = m_Camera.aspect;

        // Convert horizontal FOV to vertical FOV based on current aspect ratio
        // hFOV = 2 * atan(tan(vFOV/2) * aspect)
        // -> tan(vFOV/2) = tan(hFOV/2) / aspect
        // -> vFOV = 2 * atan(tan(hFOV/2) / aspect)

        float hFOVRad = targetHorizontalFOV * Mathf.Deg2Rad;
        float vFOVRad = 2f * Mathf.Atan(Mathf.Tan(hFOVRad / 2f) / lastAspect);
        m_Camera.fieldOfView = vFOVRad * Mathf.Rad2Deg;
    }
}
