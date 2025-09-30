using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
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
    [Tooltip("Number of wall collisions allowed before game reset.")]
    public int maxCollisions = 2;

    private int currentCollisions = 0;
    private int targetLane = 0;
    private CharacterController controller;

    private float verticalVelocity = 0f;
    private float currentForwardSpeed;
    private float targetForwardSpeed;
    private float recoveryTimer = 0f;

    private Transform laneRoot; // Parent that rotates for turns

    void Start()
    {
        controller = GetComponent<CharacterController>();
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
        // Lane switching
        if (Input.GetKeyDown(KeyCode.A))
            ChangeLane(-1);
        else if (Input.GetKeyDown(KeyCode.D))
            ChangeLane(1);

        // Turning
        if (Input.GetKeyDown(KeyCode.Q))
            Turn(-90f);
        else if (Input.GetKeyDown(KeyCode.E))
            Turn(90f);

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

    void Turn(float angle)
    {
        // Rotate laneRoot
        laneRoot.Rotate(0f, angle, 0f);

        // Ensure player remains in current lane after turn
        Vector3 localPos = transform.localPosition;
        localPos.x = targetLane * laneOffset;
        transform.localPosition = localPos;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("Wall"))
        {
            currentCollisions++;
            UIManager.Instance.ReduceHealth(1); // 👈 update UI immediately

            if (currentCollisions >= maxCollisions)
            {
                // Reset scene on too many collisions
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                return;
            }

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

            // Slow down speed and start recovery
            currentForwardSpeed = 0f;
            recoveryTimer = recoveryTime;
        }
    }

}
