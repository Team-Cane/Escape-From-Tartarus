using UnityEngine;

///
/// Auto-run player camera controller for a lane-based runner game.
///


public class PlayerCameraController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Camera transform to control. If left empty, will use Camera.main.")]
    public Transform cameraTransform;

    [Header("Positioning")]
    [Tooltip("Offset from the player position.")]
    public Vector3 offset = new Vector3(0f, 4.0f, -6.0f);

    [Tooltip("How far ahead of the player the camera looks (in world units) - creates the Temp")]
    public float lookAheadDistance = 4f;

    [Tooltip("Speed at which the camera follows position (larger = snappier)")]
    public float positionSmoothTime = 0.12f;

    [Header("Rotation")]
    [Tooltip("Should the camera face the player's forward direction?")]
    public bool lockToPlayerForward = true;

    [Tooltip("How quickly the camera rotates")]
    public float rotationSpeed = 10f;

    [Header("Collision")]
    [Tooltip("Radius for cphere casting to detect camera collisions")]
    public float collisionRadius = 0.35f;

    [Tooltip("Layers considered obstacles for camera. Set to everything except player layer.")]
    public LayerMask collisionMask = ~0;

    [Tooltip("Minimum distance camera can be from the player")]
    public float minDistance = 0.8f;

    // internal state
    private Vector3 currentVelocity = Vector3.zero;
    private Camera cam;

    private void Reset()
    {
        // sensible defaults
        offset = new Vector3(0f, 4.0f, -6.0f);
        lookAheadDistance = 4f;
        positionSmoothTime = 0.12f;
        lockToPlayerForward = true;
        rotationSpeed = 10f;
        collisionRadius = 0.35f;
        minDistance = 0.8f;
    }

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }


        if (cameraTransform != null)
            cam = cameraTransform.GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 desiredLocalOffset = offset;

        // Apply look-ahead in player's forward direction
        Vector3 forward = transform.forward;
        Vector3 lookAhead = forward * lookAheadDistance;

        // Compute desired world position for camera
        Vector3 desiredWorldPos = transform.TransformPoint(desiredLocalOffset) + lookAhead;

        // Collision: sphere cast from player position toward desired position
        Vector3 playerPosition = transform.position + Vector3.up * 0.5f; // slightly above player position
        Vector3 dirToCam = (desiredWorldPos - playerPosition);
        float desiredDistance = dirToCam.magnitude;
        Vector3 dirNormalized = (desiredDistance > 0.0001f) ? dirToCam / desiredDistance : -transform.forward;

        RaycastHit hitInfo;
        float finalDistance = desiredDistance;

        if (Physics.SphereCast(playerPosition, collisionRadius, dirNormalized, out hitInfo, desiredDistance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            // Move camera to hit point minus offset so it doesn't clip into object
            finalDistance = Mathf.Max(minDistance, hitInfo.distance - 0.1f);
            desiredWorldPos = playerPosition + dirNormalized * finalDistance;
        }

        // Smooth camera position
        Vector3 smoothedPos = Vector3.SmoothDamp(cameraTransform.position, desiredWorldPos, ref currentVelocity, positionSmoothTime);
        cameraTransform.position = smoothedPos;

        // Rotation: either lock to player's forward direction (typical Temple Run feel) or smoothly face the player
        if (lockToPlayerForward)
        {
            // Camera should look at a point in front of the player (slightly above player center)
            Vector3 lookAtPoint = transform.position + Vector3.up * 1.5f + forward * (lookAheadDistance * 0.5f);
            Quaternion targetRot = Quaternion.LookRotation(lookAtPoint - cameraTransform.position, Vector3.up);
            cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, targetRot, Mathf.Clamp01(rotationSpeed * Time.deltaTime));
        }
        else
        {
            // Smoothly turn camera to look at the player center (or a slightly raised point)
            Vector3 lookAtPoint = transform.position + Vector3.up * 1.5f;
            Quaternion targetRot = Quaternion.LookRotation(lookAtPoint - cameraTransform.position, Vector3.up);
            cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, targetRot, Mathf.Clamp01(rotationSpeed * Time.deltaTime));
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw offset direction
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.TransformPoint(offset));
        Gizmos.DrawWireSphere(transform.TransformPoint(offset), 0.12f);


        if (cameraTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, cameraTransform.position);
            Gizmos.DrawWireSphere(cameraTransform.position, collisionRadius);
        }
    }
}
