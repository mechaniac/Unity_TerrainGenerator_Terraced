using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement_UnityCC_02 : MonoBehaviour
{
    [SerializeField]
    VariableJoystick joystick;

    private CharacterController controller;

    Player_Animation_Blendtree pA;

    Vector3 playerInput;

    private float verticalVelocity;
    private float groundedTimer;        // to allow jumping when going down ramps
    private float walkSpeed = 2.0f;
    private float runSpeed = 16f;


    private float jumpHeight = 1.0f;
    private float gravityValue = 9.81f;

    public float rotationSpeed = 50f;         // Initial rotation speed
    public float rotationSpeedMax = 200f;     // Maximum rotation speed
    public float rotationAccelerationRate = 50f;      // Speed at which rotation speed increases

    private float currentRotationSpeed;





    private void Start()
    {
        controller = GetComponent<CharacterController>();
        pA = GetComponent<Player_Animation_Blendtree>();
    }

    void Update()
    {
        HandleInput();
        HandleRotation();
        HandleMovement();
    }

    void HandleInput()
    {
        playerInput = new Vector3(Input.GetAxis("Horizontal") + joystick.Direction.x, 0, Input.GetAxis("Vertical") + joystick.Direction.y);
        pA.UpdatePlayerInput(playerInput);
    }

    void HandleMovement()
    {
        bool groundedPlayer = controller.isGrounded;
        if (groundedPlayer)
        {
            // cooldown interval to allow reliable jumping even whem coming down ramps
            groundedTimer = 0.2f;
        }
        if (groundedTimer > 0)
        {
            groundedTimer -= Time.deltaTime;
        }

        // slam into the ground
        if (groundedPlayer && verticalVelocity < 0)
        {
            // hit ground
            verticalVelocity = 0f;
        }

        // apply gravity always, to let us track down ramps properly
        verticalVelocity -= gravityValue * Time.deltaTime;

        // gather lateral input control
        Vector3 move = new Vector3(0, 0, playerInput.z);

        // Calculate the speed factor based on exponential scaling
        float speedFactor = Mathf.Pow(playerInput.z, 3); // Cubing the input value for exponential scaling

        // Calculate the final speed by scaling with the max speed (runSpeed)
        float finalSpeed = Mathf.Lerp(walkSpeed, runSpeed, speedFactor);

        // Apply the speed to the movement vector
        move *= finalSpeed;

        pA.currentSpeed = move.z;

        // allow jump as long as the player is on the ground
        if (Input.GetButtonDown("Jump"))
        {
            // must have been grounded recently to allow jump
            if (groundedTimer > 0)
            {
                // no more until we recontact ground
                groundedTimer = 0;

                // Physics dynamics formula for calculating jump up velocity based on height and gravity
                verticalVelocity += Mathf.Sqrt(jumpHeight * 2 * gravityValue);

                if (pA != null)
                {
                    pA.isJumping = true;
                }
            }
        }

        // inject Y velocity before we use it
        move.y = verticalVelocity;
        move = this.transform.TransformDirection(move);
        // call .Move() only ONCE
        controller.Move(move * Time.deltaTime);
    }


    void HandleRotation()
    {
        float rotationInput = playerInput.x;

        if (rotationInput != 0)
        {
            // Increase rotation speed while input is pressed
            currentRotationSpeed += rotationAccelerationRate * Time.deltaTime;
            currentRotationSpeed = Mathf.Clamp(currentRotationSpeed, rotationSpeed, rotationSpeedMax);

            // Rotate the character
            transform.Rotate(Vector3.up, rotationInput * currentRotationSpeed * Time.deltaTime);
        }
        else
        {
            // Reset the current rotation speed when the input is released
            currentRotationSpeed = rotationSpeed;
        }
    }


}

