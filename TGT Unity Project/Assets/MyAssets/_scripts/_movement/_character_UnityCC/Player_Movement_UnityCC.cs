using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Player_Movement_UnityCC : MonoBehaviour
{

    [SerializeField]
    VariableJoystick joystick;

    CharacterController cC;
    float movementInput;
    float rotationInput;
    Vector3 m = new Vector3();
    bool desiredJump = false;
    bool isMoving;
    bool isRotating;


    public float jumpHeight = 12f;

    public float rotationSpeed = 15f;

    void Start()
    {

    }

    void Awake()
    {
        cC = GetComponent<CharacterController>();
    }


    void Update()
    {
        HandlePlayerInput();
    }

    void FixedUpdate()
    {
        HandleRotation();

        m = AddMovement();

        if (desiredJump)
        {
            Debug.Log("add jump");
            if (cC.isGrounded)
            {
                Debug.Log("jumping");
                m += AddJump();
            }
            desiredJump = false;
        }

        if (!cC.isGrounded)
        {
            m += AddGravity();
        }
        cC.Move(m * Time.deltaTime);
    }


    void HandlePlayerInput()
    {
        rotationInput = Input.GetAxis("Horizontal") + joystick.Direction.x;
        movementInput = Input.GetAxis("Vertical") + joystick.Direction.y;
        desiredJump |= Input.GetButtonDown("Jump");

        float minimumMovement = .01f;
        float minumumRotation = .01f;
        isMoving = movementInput > minimumMovement || movementInput < -minimumMovement;     //no movement below this value (positive OR negative)
        isRotating = rotationInput > minumumRotation || rotationInput < -minumumRotation;   //no rotation below this value (positive OR negative)
    }

    Vector3 AddMovement()
    {
        if (movementInput != 0)
        {
            m += new Vector3(0, 0, movementInput);
            
        }
        return Vector3.zero;
    }

    void HandleRotation()
    {
        if (isRotating)
        {

            // Quaternion targetRotation = Quaternion.LookRotation(m, Vector3.up);
            // transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            transform.Rotate(Vector3.up, rotationInput * rotationSpeed * Time.deltaTime);

        }

        if (isMoving)
        {

        }
    }

    Vector3 AddJump()
    {
        Vector3 v = new Vector3(0, jumpHeight, 0);
        return v;
    }

    Vector3 AddGravity()
    {
        return new Vector3(0, -1f, 0);
    }

}
