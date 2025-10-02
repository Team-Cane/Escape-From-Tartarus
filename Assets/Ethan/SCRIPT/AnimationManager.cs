using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
public class AnimationManager : MonoBehaviour
{
    private Animator animator;
    private CharacterController controller;

    [Header("Animation Parameters")]
    public string runParam = "isRunning";
    public string jumpParam = "isJumping";
    public string deathParam = "isDead";
    public string speedParam = "Speed";

    [Header("Movement Settings")]
    public float runThreshold = 0.1f; // Minimum movement magnitude to be considered "running"

    private bool isJumping;
    private bool isDead;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // Stop all movement animations if dead
        if (isDead) return;

        HandleRunning();
        HandleJumping();
    }

    private void HandleRunning()
    {
        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        float speed = horizontalVelocity.magnitude;

        animator.SetFloat(speedParam, speed);
        animator.SetBool(runParam, speed > runThreshold);
    }

    private void HandleJumping()
    {
        if (!controller.isGrounded && !isJumping)
        {
            isJumping = true;
            animator.SetBool(jumpParam, true);
        }
        else if (controller.isGrounded && isJumping)
        {
            isJumping = false;
            animator.SetBool(jumpParam, false);
        }
    }

    
    // Call this when the player dies.
    
    public void PlayDeathAnimation()
    {
        if (isDead) return;

        isDead = true;
        animator.SetBool(runParam, false);
        animator.SetBool(jumpParam, false);
        animator.SetBool(deathParam, true);
    }
}
