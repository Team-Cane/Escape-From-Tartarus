using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AnimationManager))]
public partial class AutoRunPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float forwardSpeed = 12f;
    public float laneChangeSpeed = 10f;
    public float laneOffset = 4.55f;
    public int totalLanes = 3;

    [Header("Jump Settings")]
    public float jumpHeight = 2f;
    public float gravity = -20f;

    [Header("Collision Slowdown Settings")]
    [Tooltip("Duration to recover full speed after hitting a wall.")]
    public float recoveryTime = 1.5f;

    [Header("Collision Settings")]
    [Tooltip("Number of wall collisions allowed before death/scene reload.")]
    public int maxCollisions = 2;

    [Header("Death / Reload")]
    [Tooltip("Name of the Animator state that plays the death clip (default: \"Death\").")]
    public string deathStateName = "Death";
    [Tooltip("Failsafe timeout (seconds) to reload if animation state can't be detected.")]
    public float deathReloadTimeout = 10f;

    private int currentCollisions = 0;
    private int targetLane = 0;
    private CharacterController controller;
    private AnimationManager animManager;
    private Animator animator;

    private float verticalVelocity = 0f;
    private float currentForwardSpeed;
    private float targetForwardSpeed;
    private float recoveryTimer = 0f;

    private Transform laneRoot; // Parent that rotates for turns

    // death handling
    private bool isDying = false;
    private Coroutine deathCoroutine;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animManager = GetComponent<AnimationManager>();
        animator = GetComponent<Animator>();

        currentForwardSpeed = forwardSpeed;
        targetForwardSpeed = forwardSpeed;

        // Create a "lane root" so the player moves relative to it
        laneRoot = new GameObject("LaneRoot").transform;
        laneRoot.position = transform.position;
        transform.SetParent(laneRoot);

        // Initialize UI with current values
        UIManager.Instance.ReduceHealth(0); // refresh health display
    }

    void Update()
    {
        // If we're dying, freeze player control & wait for animation (coroutine handles reload)
        if (isDying)
        {
            return;
        }

        // Lane switching
        if (Input.GetKeyDown(KeyCode.A))
            ChangeLane(-1);
        else if (Input.GetKeyDown(KeyCode.D))
            ChangeLane(1);

        // Jump input + gravity
        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f) verticalVelocity = -1f; // keep grounded

            if (Input.GetKeyDown(KeyCode.Space))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        // Handle forward speed recovery after collision
        if (recoveryTimer > 0f)
        {
            recoveryTimer -= Time.deltaTime;
            float t = 1f - (recoveryTimer / recoveryTime);
            currentForwardSpeed = Mathf.Lerp(0f, targetForwardSpeed, t);
        }
        else
        {
            currentForwardSpeed = targetForwardSpeed;
        }

        // Move laneRoot forward
        laneRoot.position += laneRoot.forward * currentForwardSpeed * Time.deltaTime;

        // Lane position (relative to right direction of laneRoot)
        Vector3 laneOffsetVector = laneRoot.right * (targetLane * laneOffset);
        Vector3 desiredPos = laneRoot.position + laneOffsetVector;

        // Smooth lane movement
        Vector3 currentXZ = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 desiredXZ = new Vector3(desiredPos.x, 0, desiredPos.z);
        Vector3 smoothedXZ = Vector3.Lerp(currentXZ, desiredXZ, laneChangeSpeed * Time.deltaTime);

        // Movement vector (vertical only, since laneRoot already moves forward)
        Vector3 move = new Vector3(smoothedXZ.x - transform.position.x, verticalVelocity * Time.deltaTime, smoothedXZ.z - transform.position.z);

        controller.Move(move);
    }

    void ChangeLane(int direction)
    {
        int newLane = targetLane + direction;
        int half = totalLanes / 2;
        targetLane = Mathf.Clamp(newLane, -half, half);
    }

    // Restored: pushback, random lane change, and slowdown happen here (then we call TakeDamage to update health/death)
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (isDying) return;

        if (hit.collider.CompareTag("Wall"))
        {
            // Push player away from wall so they don't keep colliding
            Vector3 pushBack = hit.normal * 2f; // 2 units away from the wall
            laneRoot.position += new Vector3(pushBack.x, 0, pushBack.z);

            // Choose a random lane different from current
            int half = totalLanes / 2;
            int randomLane;
            do
            {
                randomLane = Random.Range(-half, half + 1);
            }
            while (randomLane == targetLane);

            targetLane = randomLane;

            // Slow down speed and start recovery (we apply slowdown here)
            currentForwardSpeed = 0f;
            recoveryTimer = recoveryTime;

            // Apply damage (we pass false so TakeDamage doesn't re-apply slowdown)
            TakeDamage(1, false);
        }
    }

    /// <summary>
    /// Applies damage. If collisions reach max -> triggers death animation (waits for full animation before reload).
    /// </summary>
    public void TakeDamage(int amount, bool applySlowdown = false)
    {
        if (isDying) return; // ignore damage while dying

        // Update UI & collisions
        UIManager.Instance.ReduceHealth(amount);
        currentCollisions += amount;

        if (currentCollisions >= maxCollisions)
        {
            // Start death flow (play animation and wait for it to finish)
            StartDeathSequence();
            return;
        }

        if (applySlowdown)
        {
            currentForwardSpeed = 0f;
            recoveryTimer = recoveryTime;
        }
    }

    private void StartDeathSequence()
    {
        if (isDying) return;
        isDying = true;

        // stop movement immediately
        currentForwardSpeed = 0f;
        recoveryTimer = 0f;

        // Trigger death on animator via AnimationManager
        if (animManager != null)
        {
            animManager.PlayDeathAnimation();
        }

        // Start coroutine to wait until animation completes (or fallback timeout)
        if (deathCoroutine != null) StopCoroutine(deathCoroutine);
        deathCoroutine = StartCoroutine(WaitForDeathAndReload());
    }

    private IEnumerator WaitForDeathAndReload()
    {
        float timer = 0f;

        // Wait until animator reaches the named death state and finishes playback,
        // or until deathReloadTimeout is exceeded (failsafe)
        while (timer < deathReloadTimeout)
        {
            if (animator != null)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName(deathStateName) && stateInfo.normalizedTime >= 1f)
                {
                    break; // finished the death clip
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }

        ReloadScene();
    }

    private void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
