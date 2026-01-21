using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(HoverMotor))]
public class HoverInput : NetworkBehaviour
{
    private HoverMotor motor;

    void Awake()
    {
        motor = GetComponent<HoverMotor>();
    }

    void Update()
    {
        if (!IsOwner)
        {
            // Reset inputs for non-local players to avoid ghost inputs sticking
            motor.throttleInput = 0f;
            motor.turnInput = 0f;
            motor.isBraking = false;
            return;
        }

        motor.throttleInput = Input.GetAxis("Vertical");
        motor.turnInput = Input.GetAxis("Horizontal");
        motor.isBraking = Input.GetKey(KeyCode.Space);
    }
}
