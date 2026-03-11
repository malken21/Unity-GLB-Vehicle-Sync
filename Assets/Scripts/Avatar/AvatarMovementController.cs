using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class AvatarMovementController : NetworkBehaviour
{
    private int mbitInputA = 0;
    private int mbitInputB = 0;
    private int mbitInputJ = 0;
    private float mbitInputR = 0f;

    private int prevInputA = 0;
    private int prevInputB = 0;
    private int prevInputJ = 0;
    private float prevInputR = 0f;

    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float moveTorqueStrength = 10f;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private float groundCheckOffset = 0.1f;
    [SerializeField] private Transform horizTransform;

    private float jumpBufferTimer = 0f;
    [SerializeField] private float jumpBufferTime = 0.5f;
    [SerializeField] private int jumpThreshold = 50;
    private int currentJumpStrength = 0;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (MicrobitBLEManager.Instance != null)
        {
            MicrobitBLEManager.Instance.OnDataReceived += HandleDataReceived;
        }

        if (horizTransform == null)
        {
            horizTransform = transform.Find("Horiz");
            if (horizTransform == null)
            {
                Debug.LogWarning("[AvatarMovementController] 'Horiz' child not found. Using root forward.");
                horizTransform = transform;
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (MicrobitBLEManager.Instance != null)
        {
            MicrobitBLEManager.Instance.OnDataReceived -= HandleDataReceived;
        }
        base.OnNetworkDespawn();
    }

    private void HandleDataReceived(string data)
    {
        if (!IsOwner) return;

        if (!string.IsNullOrEmpty(data))
        {
            ProcessMicrobitCommand(data.Trim());
        }
    }

    private void ProcessMicrobitCommand(string command)
    {
        if (command.StartsWith("C:")) return;

        if (command.Contains(":"))
        {
            string[] parts = command.Split(',');
            foreach (var part in parts)
            {
                string[] kv = part.Trim().Split(':');
                if (kv.Length == 2)
                {
                    string key = kv[0].ToLower().Trim();
                    string val = kv[1].Trim();

                    if (key == "r") 
                    {
                        if (float.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float r))
                        {
                            mbitInputR = r;
                        }
                    }
                    else if (key == "j" || key == "jump")
                    {
                        if (int.TryParse(val, out int j))
                        {
                            mbitInputJ = j;
                            // Trigger jump if value is 1 (old protocol) or exceeds threshold (new protocol 0-180)
                            if (mbitInputJ == 1 || (mbitInputJ >= jumpThreshold))
                            {
                                int strength = mbitInputJ == 1 ? 180 : mbitInputJ;
                                if (jumpBufferTimer <= 0f) currentJumpStrength = strength;
                                else currentJumpStrength = Mathf.Max(currentJumpStrength, strength);
                                
                                jumpBufferTimer = jumpBufferTime;
                            }
                        }
                    }
                    else if (key == "a") int.TryParse(val, out mbitInputA);
                    else if (key == "b") int.TryParse(val, out mbitInputB);
                }
            }
        }
        else
        {
            // Case for commands like "JUMP" without key-value pair
            string cmdUpper = command.ToUpper();
            if (cmdUpper == "JUMP" || cmdUpper == "J")
            {
                jumpBufferTimer = jumpBufferTime;
                Debug.Log($"[AvatarMovementController] Processed special command: {cmdUpper}");
            }
        }

        if (mbitInputA != prevInputA || mbitInputB != prevInputB || mbitInputJ != prevInputJ || !Mathf.Approximately(mbitInputR, prevInputR))
        {
            Debug.Log($"[AvatarMovementController] Processed KV: A={mbitInputA}, B={mbitInputB}, J={mbitInputJ}, R={mbitInputR}");
            prevInputA = mbitInputA;
            prevInputB = mbitInputB;
            prevInputJ = mbitInputJ;
            prevInputR = mbitInputR;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            jumpBufferTimer = jumpBufferTime;
            currentJumpStrength = 180; // Keyboard jump is max strength
        }

        if (jumpBufferTimer > 0f)
        {
            jumpBufferTimer -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        float rotateDir = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.dKey.isPressed) rotateDir += 1f;
            if (Keyboard.current.aKey.isPressed) rotateDir -= 1f;
        }

        if (mbitInputR < -15f) rotateDir -= 1f;
        else if (mbitInputR > 15f) rotateDir += 1f;

        float moveDir = 0f;
        if (mbitInputA == 1) moveDir += 1f;
        if (mbitInputB == 1) moveDir -= 1f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) moveDir += 1f;
            if (Keyboard.current.sKey.isPressed) moveDir -= 1f;
        }

        if (rotateDir != 0f)
        {
            float clampedRotate = Mathf.Clamp(rotateDir, -1f, 1f);
            Vector3 upAxis = horizTransform != null ? horizTransform.up : Vector3.up;
            transform.Rotate(upAxis, clampedRotate * rotationSpeed * Time.fixedDeltaTime, Space.World);
        }

        if (moveDir != 0f)
        {
            Vector3 torqueAxis = horizTransform != null ? horizTransform.right : transform.right;
            Vector3 torque = torqueAxis * moveDir * moveTorqueStrength;
            rb.AddTorque(torque, ForceMode.Force);
        }

        if (jumpBufferTimer > 0f)
        {
            if (IsGrounded())
            {
                // Scale jump force between 50% and 100% of jumpForce based on strength (50-180)
                // If strength is 1-49 (old protocol), treat as max strength for safety or mid-range
                float scale = 1.0f;
                if (currentJumpStrength >= jumpThreshold)
                {
                    scale = Mathf.Lerp(0.5f, 1.0f, (currentJumpStrength - jumpThreshold) / (180f - jumpThreshold));
                }
                
                rb.AddForce(Vector3.up * jumpForce * scale, ForceMode.Impulse);
                Debug.Log($"[AvatarMovementController] Jump triggered (buffer: {jumpBufferTimer:F2}s, strength: {currentJumpStrength}, scale: {scale:F2})");
                
                jumpBufferTimer = 0f;
                currentJumpStrength = 0;
            }
        }
    }

    private bool IsGrounded()
    {
        // Raycast from slightly above the center down to the floor
        Vector3 origin = transform.position + Vector3.up * groundCheckOffset;
        bool grounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance + groundCheckOffset);
        
        // Debug visualization in Scene view
        Debug.DrawRay(origin, Vector3.down * (groundCheckDistance + groundCheckOffset), grounded ? Color.green : Color.red);
        
        return grounded;
    }
}
