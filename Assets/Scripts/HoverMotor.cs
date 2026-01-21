using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HoverMotor : MonoBehaviour
{
    [Header("Hover Settings")]
    public float hoverHeight = 4f;
    public float hoverForce = 1500f; // Maximum force applied
    public LayerMask groundLayer;
    public PIDController hoverPID = new PIDController(10f, 0.01f, 5f);

    [Header("Movement Settings")]
    public float speed = 1500f;
    public float turnSpeed = 1000f;
    public float brakingFactor = 0.95f; // Velocity multiplier when braking
    public float bankFactor = 0.1f; // How much to bank when turning

    [Header("Physics Tweaks")]
    public float stability = 0.3f;
    public float stabilitySpeed = 2.0f;
    
    // Inputs
    [HideInInspector] public float throttleInput;
    [HideInInspector] public float turnInput;
    [HideInInspector] public bool isBraking;

    private Rigidbody rb;
    private float drag;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        drag = rb.linearDamping;
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // 1. Hover Force (PID Controlled)
        RaycastHit hit;
        bool grounded = Physics.Raycast(transform.position, -transform.up, out hit, hoverHeight * 2.0f, groundLayer);
        
        if (grounded)
        {
            float heightError = hoverHeight - hit.distance;
            float force = hoverPID.GetOutput(heightError, Time.fixedDeltaTime);
            
            // Apply upward force relative to the ground normal, or just Up?
            // Usually Up is safer for stability, but varying with normal makes it traverse slopes better.
            // Let's stick to Up for now or mixture.
            Vector3 upwardForce = transform.up * force * hoverForce; // Applying relative UP
            // Limit checks could go here
            rb.AddForce(upwardForce, ForceMode.Force);
        }

        // 2. Drive Force
        if (isBraking)
        {
            // Apply braking by reducing velocity
            rb.linearVelocity *= brakingFactor;
        }
        else
        {
            Vector3 forwardForce = transform.forward * throttleInput * speed;
            rb.AddForce(forwardForce, ForceMode.Force);
        }

        // 3. Turning (Yaw)
        if (Mathf.Abs(turnInput) > 0.01f)
        {
            Vector3 turnTorque = transform.up * turnInput * turnSpeed;
            rb.AddTorque(turnTorque, ForceMode.Force);
        }

        // 4. Banking (Roll) - Visual/Physics feedback
        // To bank, we want to rotate around the local Z axis based on turnInput
        // Or we can apply a torque to roll it
        // A simple way is to target a specific roll angle
        // But for physics, maybe just adding torque is enough, or using the "Stability" logic below.
        
        // 5. Stability & Alignment
        // Keep the ship upright-ish but allow for banking
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, Vector3.up) * transform.rotation;
        
        // If we want banking:
        if (Mathf.Abs(turnInput) > 0.01f)
        {
            // Add a roll to the target rotation
            // Negative turn (left) -> Positive Roll (right bank?) or Left Bank?
            // Usually turn Left -> Bank Left (Roll Left)
            float targetBank = -turnInput * bankFactor; 
            // This is complex to blend with FromToRotation. 
            // Simplified: Add Torque to upright.
        }

        // Simple Upright Force (Stabilizer)
        Vector3 predictedUp = Quaternion.AngleAxis(
            rb.angularVelocity.magnitude * Mathf.Rad2Deg * stability / stabilitySpeed,
            rb.angularVelocity
        ) * transform.up;

        Vector3 torqueVector = Vector3.Cross(predictedUp, Vector3.up);
        // rb.AddTorque(torqueVector * stabilitySpeed * stabilitySpeed, ForceMode.Acceleration);
        // NOTE: The above is a common stabilizer trick, but can glitch. 
        // Let's stick to a simpler "Align to ground normal" or "Align to World Up" torque if not turning hard.

        // Sideways Friction / Anti-Drift
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        float sidewaysSpeed = localVelocity.x;
        // Apply force against sideways movement
        Vector3 sideForce = -transform.right * sidewaysSpeed * rb.mass; // Correct massive drift
        rb.AddForce(sideForce, ForceMode.Force);
    }
}
