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
    [SerializeField] private float rotationSpeed = 30f;
    [SerializeField] private float moveTorqueStrength = 3f;
    [SerializeField] private float groundCheckDistance = 1.0f;
    [SerializeField] private float groundCheckOffset = 0.7f;
    [SerializeField] private LayerMask groundLayer = -1;
    [SerializeField] private Transform horizTransform;
    [SerializeField] private KeyboardRotator keyboardRotator;

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
            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Horiz")
                {
                    horizTransform = child;
                    break;
                }
            }
            if (horizTransform == null)
            {
                Debug.LogWarning("[AvatarMovementController] 'Horiz' child not found. Using root forward.");
                horizTransform = transform;
            }
        }

        if (keyboardRotator == null)
        {
            keyboardRotator = GetComponentInChildren<KeyboardRotator>();
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

        // Keyboard inputs are now handled by KeyboardRotator (Space jump), 
        // but we keep the logic for Microbit jump buffering here if it needs to interact with movement.

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

        rotateDir = Mathf.Clamp(rotateDir, -1f, 1f);

        float moveDir = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) moveDir += 1f;
            if (Keyboard.current.sKey.isPressed) moveDir -= 1f;
        }

        if (mbitInputA == 1) moveDir += 1f;
        if (mbitInputB == 1) moveDir -= 1f;

        moveDir = Mathf.Clamp(moveDir, -1f, 1f);

        if (rotateDir != 0f)
        {
            // 左右キー（左右の入力）は、アバターのベースであるHorizのY軸（up）回りの回転（Yaw旋回）としてHoriz自体に適用
            if (horizTransform != null)
            {
                KeepHoriz keepHoriz = horizTransform.GetComponent<KeepHoriz>();
                if (keepHoriz != null)
                {
                    keepHoriz.AddYaw(rotateDir * rotationSpeed * Time.fixedDeltaTime);
                }
                else
                {
                    horizTransform.Rotate(Vector3.up, rotateDir * rotationSpeed * Time.fixedDeltaTime, Space.World);
                }
            }
            else
            {
                transform.Rotate(Vector3.up, rotateDir * rotationSpeed * Time.fixedDeltaTime, Space.World);
            }
        }

        if (moveDir != 0f)
        {
            // 前後キー（前後の入力）は、HorizのX軸（right）回りのトルク（前後回転・Pitch転がり）として適用
            Vector3 torqueAxis = horizTransform != null ? horizTransform.right : transform.right;
            Vector3 torque = torqueAxis * moveDir * moveTorqueStrength;
            rb.AddTorque(torque, ForceMode.Force);
        }

        if (jumpBufferTimer > 0f)
        {
            // Scale jump force between 50% and 100% of jumpForce based on strength (50-180)
            float scale = 1.0f;
            if (currentJumpStrength >= jumpThreshold)
            {
                scale = Mathf.Lerp(0.5f, 1.0f, (currentJumpStrength - jumpThreshold) / (180f - jumpThreshold));
            }

            if (keyboardRotator != null)
            {
                keyboardRotator.Jump(scale);
                // We assume KeyboardRotator handles grounding and actual force application
                jumpBufferTimer = 0f;
                currentJumpStrength = 0;
            }
        }
    }

    // IsGrounded logic has been moved to KeyboardRotator
}
