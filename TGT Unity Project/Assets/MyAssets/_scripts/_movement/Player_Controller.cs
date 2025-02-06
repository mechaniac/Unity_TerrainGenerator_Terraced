using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class Player_Controller : MonoBehaviour
{

    [SerializeField]
    VariableJoystick joystick;

    [SerializeField]
    Canvas canvas;

    Rigidbody rB;

    [SerializeField]
    GameObject artAsset;

    [SerializeField]
    Transform targetSphere;

    [SerializeField, Range(0f, 100f)]
    float speed = 5f, speedBackward;

    [SerializeField, Range(0f, 100f)]
    float jumpHeight = 2f;

    [SerializeField, Range(0f, 100f)]
    float rotationSpeed = 5f;
    Vector3 m;

    [SerializeField]
    Animator a;

    private Vector3 smoothVelocity = Vector3.zero;
    public float smoothTime = 0.1f; // Adjust this value to control the smoothness of the animation



    [SerializeField]
    Transform camholder;

    [SerializeField]
    Transform camOrient;

    [SerializeField]
    Transform cam;

    Vector3 initialCamPosition;


    public float camRotationOffset = 10f; // Adjustable offset in degrees
    public float camRotationSpeed = 5f; // Speed at which the camera catches up

    public float snapBackRotationSpeed = 1f; // Speed at which the camera returns to original rotation

    public Transform pivot; // The pivot point the camera will rotate around
    public float lookRotationSpeed = 5.0f; // Speed of rotation

    private float yaw = 0.0f; // Horizontal rotation angle
    private float pitch = 0.0f; // Vertical rotation angle

    private bool isMoving;


    bool isGrounded;

    bool isRotating;

    bool desiredJump;

    bool isInside;
    readonly float turnSmoothTime = 1f;
    float turnSmoothVelocity;

    void Awake()
    {
        canvas.gameObject.SetActive(true);
        rB = GetComponent<Rigidbody>();
        m = transform.position;


    }

    void Start()
    {
        initialCamPosition = cam.localPosition;
        
    }

    void Update()
    {
        HandlePlayerInput();
        if (isMoving)
        {
            a.SetBool("isMoving", true);
            SetSmoothProceduralAnimation(m);
        }
        else
        {
            a.SetBool("isMoving", false);
            // ResetProceduralAnimation();
        }
        // Debug.Log(isGrounded);
        // cC.Move(mDir.normalized * Time.deltaTime * speed);


    }

    void FixedUpdate()
    {
        if (desiredJump)
        {
            desiredJump = false;
            Jump();
        }
        Vector3 mDir = HandleRotation();

        // rB.MovePosition(transform.position + (mDir * Time.deltaTime * speed));
        Vector3 forwardForce = mDir * Time.deltaTime * speed;
        // rB.AddForce(forwardForce);
        mDir += HandleGravity();

        if (m.z < 0)
        {
            rB.AddForce(mDir * Time.deltaTime * speedBackward * 100);
        }
        else
        {
            rB.AddForce(mDir * Time.deltaTime * speed * 100);
        }

        UpdateCamera();

    }

    void UpdateCamera()
    {
        // camholder.transform.position = transform.position;
        // camholder.transform.rotation = transform.rotation;

        camholder.transform.position = transform.position; // Update position to follow the player

        // Calculate the desired rotation of the camholder with the same pitch and roll as the camholder, but yaw from the player
        Quaternion desiredRotation = Quaternion.Euler(camholder.transform.eulerAngles.x, transform.eulerAngles.y, camholder.transform.eulerAngles.z);

        // Determine the current angle difference in Y
        float angleDifference = Mathf.DeltaAngle(camholder.transform.eulerAngles.y, transform.eulerAngles.y);

        // If the angle difference is greater than the specified offset, start rotating the camera
        if (Mathf.Abs(angleDifference) > camRotationOffset)
        {
            // Smoothly interpolate to the desired rotation at the specified speed
            camholder.transform.rotation = Quaternion.Lerp(camholder.transform.rotation, desiredRotation, Time.deltaTime * camRotationSpeed);
        }

        RotateTowards(camholder, transform);
        UpdateCameraFromInput();
    }

    void RotateTowards(Transform t, Transform target)
    {
        if (!isRotating)
        {
            // Get the current rotation and target rotation for the Y axis
            Quaternion currentRotation = t.rotation;
            Quaternion targetRotation = Quaternion.Euler(t.eulerAngles.x, target.eulerAngles.y, t.eulerAngles.z);

            // Smoothly interpolate the rotation towards the target rotation at the specified speed
            t.rotation = Quaternion.Slerp(currentRotation, targetRotation, Time.deltaTime * snapBackRotationSpeed);
        }
    }

    void UpdateCameraFromInput()
    {
        if (Input.GetMouseButton(0))
        {
            // Capture mouse movement, Mouse X for horizontal, Mouse Y for vertical
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = -Input.GetAxis("Mouse Y"); // Inverted Y for a natural feel

            // Debug.Log(mouseX + ", " + mouseY);

            yaw += mouseX * lookRotationSpeed;
            pitch += mouseY * lookRotationSpeed;

            // Clamp the vertical rotation to prevent flipping
            pitch = Mathf.Clamp(pitch, -25f, 30f);
            yaw = Mathf.Clamp(yaw, -60f, 60f);

            // Apply rotation
            camOrient.transform.localEulerAngles = new Vector3(pitch, yaw, 0.0f);

            

            if(isInside){
                cam.localPosition = initialCamPosition + new Vector3(0, -2f, 12f) + new Vector3(0, 0, -pitch * .05f);
            } else{
                cam.localPosition = initialCamPosition + new Vector3(0, 0, -pitch * .2f);
            }
            // Keep the camera looking at the pivot point
            // camOrient.transform.position = pivot.position - transform.forward * Vector3.Distance(transform.position, pivot.position);
        }
        else
        {
            camOrient.transform.localEulerAngles = new Vector3(0, 0, 0);

            if(isInside){
                cam.localPosition = initialCamPosition + new Vector3(0, -2f, 12f);
            } else{
                cam.localPosition = initialCamPosition;
            }

            pitch = 0;
            yaw = 0;
        }

    }



    void HandlePlayerInput()
    {
        m.x = Input.GetAxis("Horizontal") + joystick.Direction.x;
        m.z = Input.GetAxis("Vertical") + joystick.Direction.y;
        desiredJump |= Input.GetButtonDown("Jump");
        // m.y = 2f;
        // m = Vector3.ClampMagnitude(m, 1f);
        targetSphere.transform.localPosition = m + new Vector3(0, 1, 0);

        float isMovingStop = .3f;
        float isRotatingStop = .1f;

        // if (m.z < -.3f) { isRotatingStop = .8f; }

        isMoving = m.z > isMovingStop || m.z < -isMovingStop;
        isRotating = m.x > isRotatingStop || m.x < -isRotatingStop;
    }

    public void OnJumpButtonPress()
    {
        Debug.Log("Jump");
        desiredJump = true;
    }

    public void OnCloseButtonPress()
    {
        Application.Quit();
    }


    Vector3 HandleRotation()
    {
        if (isRotating)
        {
            // Quaternion targetRotation = Quaternion.LookRotation(m, Vector3.up);
            // transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            transform.Rotate(Vector3.up, m.x * rotationSpeed * Time.deltaTime);

        }

        if (isMoving)
        {
            float tragetAngle = Mathf.Atan2(m.x, m.z) * Mathf.Rad2Deg + camholder.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, tragetAngle, ref turnSmoothVelocity, turnSmoothTime);

            // transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 mDir = Quaternion.Euler(0f, tragetAngle, 0f) * Vector3.forward;
            // Debug.Log("Moooov");
            // Debug.Log(mDir);
            return mDir;

        }
        return Vector3.zero;
    }

    void Jump()
    {
        if (isGrounded)
        {
            Vector3 v = rB.velocity;
            v.y += Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
            rB.velocity = v;
        }


    }
    Vector3 HandleGravity()
    {
        if (isGrounded)
        {
            return new Vector3(0, -0.1f, 0);
        }
        else
        {
            // Debug.Log("Big Force");
            return new Vector3(0, -1f, 0);
        }
    }

    void OnCollisionExit(Collision c)
    {
        isGrounded = false;
    }


    void OnCollisionEnter(Collision c)
    {
        Debug.Log("onCollisionEnter");
        EvaluateCollision(c);
        // Debug.Log("enter");
        if (c.collider.tag == "Inside")
        {
            isInside = true;
            Debug.Log("isInside");
        }
        else
        {
            isInside = false;
        }
    }

    void OnCollisionStay(Collision c)
    {
        EvaluateCollision(c);
        // Debug.Log("Stay");



    }
    void EvaluateCollision(Collision c)
    {
        for (int i = 0; i < c.contactCount; i++)
        {
            Vector3 normal = c.GetContact(i).normal;
            isGrounded |= normal.y >= 0.5f;


        }

    }

    void SetProceduralAnimation(Vector3 movementInput)
    {
        if (artAsset == null) return;
        Vector3 r = artAsset.transform.localEulerAngles;
        r.x = movementInput.z * 5;
        r.y = movementInput.x * 5f;
        r.z = -movementInput.x * 5;
        artAsset.transform.localEulerAngles = r;
    }

    void SetSmoothProceduralAnimation(Vector3 movementInput)
    {
        if (artAsset == null) return;

        float targetRotationX = movementInput.z * 5f;
        float targetRotationY = movementInput.x * 5f;
        float targetRotationZ = -movementInput.x * 5f;

        Vector3 currentRotation = artAsset.transform.localEulerAngles;
        float smoothVelocityX = 0f;
        float smoothVelocityY = 0f;
        float smoothVelocityZ = 0f;

        // Smoothly interpolate each angle component
        float smoothedRotationX = Mathf.SmoothDampAngle(currentRotation.x, targetRotationX, ref smoothVelocityX, smoothTime);
        float smoothedRotationY = Mathf.SmoothDampAngle(currentRotation.y, targetRotationY, ref smoothVelocityY, smoothTime);
        float smoothedRotationZ = Mathf.SmoothDampAngle(currentRotation.z, targetRotationZ, ref smoothVelocityZ, smoothTime);

        // Apply the smoothed rotation to the art asset
        artAsset.transform.localEulerAngles = new Vector3(smoothedRotationX, smoothedRotationY, smoothedRotationZ);
    }


    void ResetProceduralAnimation()
    {
        artAsset.transform.localEulerAngles = new Vector3();
    }
}
