using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


// handles the fbx animation
// leaning
// and particle system of the character

[RequireComponent(typeof(CharacterController))]
public class Player_ProceduralPoseAnimation : MonoBehaviour
{
    public Animator animator;

    public Transform characterModel;
    CharacterController cC;

    public ParticleSystem pS_Run;
    public GameObject pS_Jump;
    Vector3 movementInput = new Vector3();

    float runTimer;

    public bool isJumping = false;


    // Leaning 

    public float leanAngleMax = 15.0f;    // Maximum lean angle in degrees
    public float leanSpeed = 5.0f;        // Speed at which the character leans
    public float leanResetSpeed = 10.0f;  // Speed at which the character returns to upright position

    public float forwardLeanAngleMax = 20.0f; // Maximum forward lean angle in degrees
    public float forwardLeanSpeed = 10.0f;    // Speed at which the character leans forward

    void Awake()
    {
        cC = GetComponent<CharacterController>();
    }


    void Update()
    {
        GetPlayerInput();
        Runtimer();

        if (cC.isGrounded)
        {
            PlayAnimation();
        }

        if (isJumping)
        {
            PlayJumpAnimation();
        }

        HandleCharacterLeaning();
    }

    void GetPlayerInput()
    {
        movementInput.z = Input.GetAxis("Vertical");
        movementInput.x = Input.GetAxis("Horizontal");
    }

    void Runtimer()
    {
        if (math.abs(movementInput.z) > 0.1f)
        {
            runTimer += Time.deltaTime;
        }
        else
        {
            runTimer = 0;
        }


    }


    void PlayAnimation()
    {

        // Debug.Log("stepByTime" + stepByTime);
        if (runTimer != 0)
        {
            if (movementInput.z > .5f)
            {
                SetRunParticlesActive();
                float stepByTime = Mathf.Sin(runTimer * 3);
                if (stepByTime > 0)
                {
                    animator.CrossFade("pose_run_lft", 1f);

                }
                else
                {
                    animator.CrossFade("pose_run_rght", 1f);

                }
            }
            else
            {
                SetRunParticlesInactive();
                float stepByTime = Mathf.Sin(runTimer * 12);
                if (stepByTime > 0)
                {
                    animator.CrossFade("pose_walk_lft", 1f);

                }
                else
                {
                    animator.CrossFade("pose_walk_rght", 1f);

                }
            }

        }
        else
        {
            animator.CrossFade("idle_01", .5f);
        }
    }

    void SetRunParticlesActive()
    {
        if (pS_Run != null)
        {
            if (!pS_Run.gameObject.activeSelf)
            {
                pS_Run.gameObject.SetActive(true);
            }
        }
    }

    void SetRunParticlesInactive()
    {
        if (pS_Run != null)
        {
            if (pS_Run.gameObject.activeSelf)
            {
                pS_Run.gameObject.SetActive(false);
            }
        }
    }

    void PlayJumpAnimation()
    {
        if (pS_Jump != null)
        {
            SpawnJumpParticles(transform.position + new Vector3(0, .4f, 0));
        }
        // Debug.Log("is jumping in animator");
        animator.CrossFade("pose_jump_01", 1f);


        // animator.Play("pose_jump_01");
        isJumping = false;
    }

    void SpawnJumpParticles(Vector3 pos)
    {
        GameObject tmpParticles = (GameObject)Instantiate(pS_Jump, pos, Quaternion.identity); //look up how to use Instantiate, you'll need it a lot
        Destroy(tmpParticles, 3f);
    }

    void HandleCharacterLeaning()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float speed = cC.velocity.magnitude; // Get the current speed of the character

        // Calculate the target side lean angle based on input
        float targetSideLeanAngle = -horizontalInput * leanAngleMax * (speed / 3f);

        // Calculate the target forward lean angle based on speed
        float targetForwardLeanAngle = Mathf.Clamp(speed / 10f * forwardLeanAngleMax, 0, forwardLeanAngleMax);

        // Smoothly interpolate the current side lean angle
        float currentSideLeanAngle = Mathf.LerpAngle(characterModel.localEulerAngles.z, targetSideLeanAngle, leanSpeed * Time.deltaTime);

        // Smoothly interpolate the current forward lean angle
        float currentForwardLeanAngle = Mathf.LerpAngle(characterModel.localEulerAngles.x, targetForwardLeanAngle, forwardLeanSpeed * Time.deltaTime);

        // Apply the lean rotation to the character model
        characterModel.localRotation = Quaternion.Euler(currentForwardLeanAngle, characterModel.localEulerAngles.y, currentSideLeanAngle);

        // If no input is detected, gradually return the character model to an upright position
        if (horizontalInput == 0)
        {
            // Smoothly interpolate back to 0 side lean angle (upright position)
            currentSideLeanAngle = Mathf.LerpAngle(characterModel.localEulerAngles.z, 0, leanResetSpeed * Time.deltaTime);
            characterModel.localRotation = Quaternion.Euler(currentForwardLeanAngle, characterModel.localEulerAngles.y, currentSideLeanAngle);
        }
    }
}
