using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(HoverMotor))]
public class HoverInput : NetworkBehaviour
{
    private HoverMotor motor;

    // Define InputActions for compatibility with the New Input System
    private InputAction moveAction;
    private InputAction brakeAction;

    void Awake()
    {
        motor = GetComponent<HoverMotor>();

        // Setup default bindings for WASD/Arrows and Gamepad
        moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");
        moveAction.AddCompositeBinding("Dpad")
            .With("Up", "<Keyboard>/w")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/s")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/a")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/d")
            .With("Right", "<Keyboard>/rightArrow");

        brakeAction = new InputAction("Brake", binding: "<Keyboard>/space");
        brakeAction.AddBinding("<Gamepad>/buttonSouth"); // A button
    }

    void OnEnable()
    {
        moveAction.Enable();
        brakeAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        brakeAction.Disable();
    }

    void Update()
    {
        if (!IsOwner)
        {
            motor.throttleInput = 0f;
            motor.turnInput = 0f;
            motor.isBraking = false;
            return;
        }

        // Read values from the New Input System
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        
        motor.throttleInput = moveInput.y;
        motor.turnInput = moveInput.x;
        motor.isBraking = brakeAction.IsPressed();
    }
}
