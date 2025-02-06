using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera_Movement : MonoBehaviour
{
    public Transform camHolder;                // Reference to the camHolder transform
    public float rotationSpeed = 100.0f;       // Speed of rotation based on thumbstick input
    public float smoothSnapSpeed = 5.0f;       // Speed at which the camera snaps back to its original position
    public float maxRotationY = 90.0f;         // Maximum rotation on the y-axis (left-right)
    public float maxRotationUpX = 60.0f;       // Maximum rotation on the x-axis (up)
    public float maxRotationDownX = 15.0f;     // Maximum rotation on the x-axis (down)

    private Vector3 initialRotation;           // The initial rotation of the camHolder
    private Vector3 targetRotation;            // The target rotation based on input or reset
    private bool isResetting = false;          // Flag to prevent multiple coroutine instances

    void Start()
    {
        // Store the initial rotation of the camHolder
        initialRotation = camHolder.localEulerAngles;
        targetRotation = initialRotation;
    }

    void Update()
    {
        HandleCameraRotation();
        HandleCameraReset();
    }

    void HandleCameraRotation()
    {
        if (!isResetting) // Prevent input while resetting
        {
            // Get the right thumbstick input using custom input axes
            float horizontalInput = Input.GetAxis("Horizontal Camera");
            float verticalInput = Input.GetAxis("Vertical Camera");

            // Adjust the target rotation based on the thumbstick input
            targetRotation.y += horizontalInput * rotationSpeed * Time.deltaTime;
            targetRotation.x -= verticalInput * rotationSpeed * Time.deltaTime;

            // Clamp the rotation on the y-axis to the desired limits
            targetRotation.y = Mathf.Clamp(targetRotation.y, initialRotation.y - maxRotationY, initialRotation.y + maxRotationY);

            // Clamp the rotation on the x-axis with different limits for up and down
            targetRotation.x = Mathf.Clamp(targetRotation.x, initialRotation.x - maxRotationDownX, initialRotation.x + maxRotationUpX);

            // Apply the target rotation to the camHolder
            camHolder.localRotation = Quaternion.Euler(targetRotation.x, targetRotation.y, targetRotation.z);
        }
    }

    void HandleCameraReset()
    {
        // Check if the right action button (e.g., "circle" on PlayStation) is pressed and not already resetting
        if (Input.GetButtonDown("Fire3") && !isResetting) // "Fire3" is typically mapped to the "B" or "Circle" button
        {
            StartCoroutine(SmoothReset());
        }
    }

    IEnumerator SmoothReset()
    {
        isResetting = true; // Set the flag to prevent input during reset

        // Smoothly interpolate from the current rotation to the initial rotation
        while (Vector3.Distance(camHolder.localEulerAngles, initialRotation) > 0.01f)
        {
            camHolder.localRotation = Quaternion.Lerp(camHolder.localRotation, Quaternion.Euler(initialRotation), smoothSnapSpeed * Time.deltaTime);
            targetRotation = camHolder.localEulerAngles; // Update targetRotation to prevent jumps
            yield return null;
        }

        // Ensure exact reset to avoid small discrepancies
        camHolder.localRotation = Quaternion.Euler(initialRotation);
        targetRotation = initialRotation;

        isResetting = false; // Reset the flag to allow input again
    }
}
