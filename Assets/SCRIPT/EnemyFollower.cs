using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class EnemyFollower : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public CharacterController controller;

    [Header("Movement Settings")]
    public float forwardSpeed = 12f;
    public float laneChangeSpeed = 10f;
    public float laneOffset = 4.55f; // distance between lanes
    public int totalLanes = 3;

    private int currentLane = 1; // middle lane by default
    private Vector3 targetPosition;

    [Header("Turn Settings")]
    public float turnSpeed = 5f;

    private void Start()
    {
        if (!controller) controller = GetComponent<CharacterController>();
        UpdateLanePosition();
    }

    private void Update()
    {
        if (!player) return;

        // Follow player forward
        Vector3 forwardMove = transform.forward * forwardSpeed * Time.deltaTime;

        // Match lane with player
        int playerLane = GetPlayerLane();
        if (currentLane != playerLane)
        {
            currentLane = playerLane;
            UpdateLanePosition();
        }

        // Move toward target lane smoothly
        Vector3 moveDirection = Vector3.MoveTowards(transform.position, targetPosition, laneChangeSpeed * Time.deltaTime);
        Vector3 horizontalMove = moveDirection - transform.position;

        // Apply movement
        controller.Move(forwardMove + horizontalMove);

        // Match player rotation
        Quaternion targetRotation = Quaternion.LookRotation(player.forward, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private int GetPlayerLane()
    {
        // Assuming lanes are centered at x = 0 (middle), left = -laneOffset, right = laneOffset
        float playerX = player.position.x;
        int lane = Mathf.RoundToInt(playerX / laneOffset) + 1;
        lane = Mathf.Clamp(lane, 0, totalLanes - 1);
        return lane;
    }

    private void UpdateLanePosition()
    {
        float xPos = (currentLane - 1) * laneOffset; // middle lane = 1
        targetPosition = new Vector3(xPos, transform.position.y, transform.position.z);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("Player"))
        {
            // Reset the game
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
