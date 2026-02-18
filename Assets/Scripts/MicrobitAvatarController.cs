using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(KeyboardRotator))] // We reuse the logic or replicate it
public class MicrobitAvatarController : NetworkBehaviour
{
    private KeyboardRotator rotator;
    
    // Command mapping
    private string currentCommand = "S"; // S = Stop

    private void Start()
    {
        rotator = GetComponent<KeyboardRotator>();
        
        if (MicrobitBLEManager.Instance != null)
        {
            MicrobitBLEManager.Instance.OnDataReceived += HandleDataReceived;
            Debug.Log("[MicrobitController] Subscribed to BLE Manager");
        }
        else
        {
            Debug.LogWarning("[MicrobitController] MicrobitBLEManager instance not found!");
        }
    }

    public override void OnDestroy()
    {
        if (MicrobitBLEManager.Instance != null)
        {
            MicrobitBLEManager.Instance.OnDataReceived -= HandleDataReceived;
        }
        base.OnDestroy();
    }

    private void HandleDataReceived(string data)
    {
        // Clean the string (remove newlines etc)
        currentCommand = data.Trim().ToUpper();
        // Debug.Log($"[MicrobitController] Command: {currentCommand}");
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;
        // If we want to allow keyboard AND microbit, we should check if command is 'S' (stop)
        // If command is 'S', we might let keyboard take over, or just do nothing.
        
        // This is a simple implementation: simpler than KeyboardRotator's physics
        // But let's try to inject into the Rigidbody if possible, matching KeyboardRotator's style
        
        if (rotator == null) return;
        
        // Reflection or public access? 
        // KeyboardRotator fields are private serializefield. 
        // We can't access them easily unless we change KeyboardRotator or duplicate logic.
        // Let's duplicate the relevant logic for now for safety, or assume we modify KeyboardRotator.
        
        // Actually, we can just use the same components.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        float torque = 0f;
        float rotate = 0f;
        
        // Command Logic
        // "L" -> Rotate Left
        // "R" -> Rotate Right
        // "F" -> Forward (if added)
        // "B" -> Backward (if added)
        
        if (currentCommand == "L")
        {
             rotate = -1f;
        }
        else if (currentCommand == "R")
        {
             rotate = 1f;
        }
        // Add more commands as needed

        // Apply
        float rotationSpeed = 100f; // Could accept a serialized field
        
        // Same as KeyboardRotator: A/D rotates Transform directly (sometimes better for avatars)
        // Or uses Torque if desired.
        // KeyboardRotator uses transform.Rotate for A/D
        
        if (rotate != 0f)
        {
             transform.Rotate(Vector3.up, rotate * rotationSpeed * Time.fixedDeltaTime);
        }
    }
}
