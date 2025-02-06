using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Player_Animation_Blendtree : MonoBehaviour
{
    CharacterController cC;

    public Transform characterModel;
    public Animator animator;

    public ParticleSystem particles_Jump;
    public ParticleSystem particles_Run;
    public float currentSpeed = 0f; // Set via PlayerMovement script Line 90 (Todo: make this more elegant!)
    
    public float rotationSpeed = 700.0f;

    private float horizontalInput;
    public bool isJumping = false;

    // Leaning 

    public float sideLeanAngleMax = 5.0f;    // Maximum lean angle in degrees
    public float sideLeanSpeed = 5.0f;        // Speed at which the character leans
    public float sideLeanResetSpeed = 10.0f;  // Speed at which the character returns to upright position

    public float forwardLeanAngleMax = 20.0f; // Maximum forward lean angle in degrees
    public float forwardLeanSpeed = 10.0f;    // Speed at which the character leans forward

    void Awake()
    {
        cC = GetComponent<CharacterController>();
    }

    public void UpdatePlayerInput(Vector3 playerInput)
    {
        animator.SetFloat("Vertical", playerInput.z);
        animator.SetFloat("Horizontal", playerInput.x);
        
        horizontalInput = playerInput.x; //used to calculate leaning

        PlayAnimation();
    }

    void PlayAnimation()
    {
        // Handle Jumping
        if (isJumping)
        {
            PlayJumpAnimation();
        }

        // Check if the character has landed
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName("pose_jump_01") && cC.isGrounded)
        {
            animator.CrossFade("Movement Blend Tree", 0.1f);  // Fade back to the Blend Tree
        }
        HandleCharacterLeaning();

        // play Run particles
        var emissionModule = particles_Run.emission;
        if (!cC.isGrounded)
        {
            emissionModule.rateOverTime = 0; //None while jumping
        }
        else
        {
            emissionModule.rateOverTime = currentSpeed * 3f;
        }

    }


    void HandleCharacterLeaning()
    {
        float speed = cC.velocity.magnitude; // Get the current speed of the character

        // Calculate the target side lean angle based on input
        float targetSideLeanAngle = -horizontalInput * sideLeanAngleMax * (speed / 3f);

        // Calculate the target forward lean angle based on speed
        float targetForwardLeanAngle = Mathf.Clamp(speed / 10f * forwardLeanAngleMax, 0, forwardLeanAngleMax);

        // Smoothly interpolate the current side lean angle
        float currentSideLeanAngle = Mathf.LerpAngle(characterModel.localEulerAngles.z, targetSideLeanAngle, sideLeanSpeed * Time.deltaTime);

        // Smoothly interpolate the current forward lean angle
        float currentForwardLeanAngle = Mathf.LerpAngle(characterModel.localEulerAngles.x, targetForwardLeanAngle, forwardLeanSpeed * Time.deltaTime);

        // Apply the lean rotation to the character model
        characterModel.localRotation = Quaternion.Euler(currentForwardLeanAngle, characterModel.localEulerAngles.y, currentSideLeanAngle);

        // If no input is detected, gradually return the character model to an upright position
        if (horizontalInput == 0)
        {
            // Smoothly interpolate back to 0 side lean angle (upright position)
            currentSideLeanAngle = Mathf.LerpAngle(characterModel.localEulerAngles.z, 0, sideLeanResetSpeed * Time.deltaTime);
            characterModel.localRotation = Quaternion.Euler(currentForwardLeanAngle, characterModel.localEulerAngles.y, currentSideLeanAngle);
        }
    }

    void PlayJumpAnimation()
    {
        if (particles_Jump != null)
        {
            PlayJumpParticles();
        }
        animator.CrossFade("pose_jump_01", .2f);

        isJumping = false;
    }

    void PlayJumpParticles()
    {
        particles_Jump.Emit(10);
    }
}
