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

    private bool triggerJump = false;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float moveTorqueStrength = 10f;
    [SerializeField] private float groundCheckDistance = 1.1f;
    [SerializeField] private Transform horizTransform;

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

        if (!command.Contains(":")) return;

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
                else if (key == "j")
                {
                    if (int.TryParse(val, out int j))
                    {
                        mbitInputJ = j;
                        if (mbitInputJ == 1) triggerJump = true;
                    }
                }
                else if (key == "a") int.TryParse(val, out mbitInputA);
                else if (key == "b") int.TryParse(val, out mbitInputB);
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
            triggerJump = true;
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
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) rotateDir += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) rotateDir -= 1f;
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

        if (triggerJump)
        {
            if (IsGrounded())
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                Debug.Log("[AvatarMovementController] Jump triggered");
            }
            triggerJump = false;
        }
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance);
    }
}
