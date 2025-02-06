// using Unity.VisualScripting;
using UnityEngine;

public class MovingSphere : MonoBehaviour
{
    [SerializeField]
    Transform target_sphere;

    Vector3 velocity, desiredVelocity;

    [SerializeField, Range(0f, 100f)]
    float maxSpeed = 3f;

    [SerializeField, Range(0f, 100f)]
    float maxAcceleration = 10f, maxAirAcceleration = 1f;

    [SerializeField, Range(0f,100f)]
    float rotationSpeed;
    Rigidbody body;

    [SerializeField, Range(0f, 10f)]
    float jumpHeight = 2f;

    [SerializeField, Range(0, 5)]
    int maxAirJumps = 0;
    int jumpPhase;
    bool desiredJump;

    bool onGround;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
    }
    void Update()
    {
        if (target_sphere == null)
        {
            Debug.LogError("Set target sphere on character controller");
            return;
        }


        Vector3 playerInput;
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.z = Input.GetAxis("Vertical");
        playerInput.y = 0f;
        playerInput = Vector3.ClampMagnitude(playerInput, 1f);

        target_sphere.localPosition = playerInput * 2;


        desiredVelocity = playerInput * maxSpeed;

        Quaternion targetRotation = Quaternion.LookRotation(playerInput, Vector3.up);
        if(playerInput.x != 0f){
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        

        desiredJump = Input.GetButtonDown("Jump");
    }

    void FixedUpdate()
    {
        UpdateState();

        float acceleration = onGround ? maxAcceleration : maxAirAcceleration;
        float maxSpeedChange = acceleration * Time.deltaTime;
        velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);
        velocity.z = Mathf.MoveTowards(velocity.z, desiredVelocity.z, maxSpeedChange);
        if (desiredJump)
        {
            desiredJump = false;
            Jump();
        }

        body.velocity = velocity;
        onGround = false;
    }

    void UpdateState()
    {
        velocity = body.velocity;
        if (onGround)
        {
            jumpPhase = 0;
        }
    }
    void Jump()
    {
        if (onGround || jumpPhase < maxAirJumps)
        {
            jumpPhase += 1;
            float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
            if (velocity.y > 0f)
            {
                jumpSpeed = Mathf.Max(jumpSpeed - velocity.y, 0f);
            }
            velocity.y += jumpSpeed;
        }
    }
    void OnCollisionEnter(Collision c)
    {
        EvaluateCollision(c);
    }
    void OnCollisionStay(Collision c)
    {
        EvaluateCollision(c);
    }
    void EvaluateCollision(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            onGround |= normal.y >= 0.9f;
        }
    }
}

