using UnityEngine;

public class ObstacleMeta : MonoBehaviour
{
    [Tooltip("1 = sits in one lane; 2 = spans two lanes")]
    public int laneWidth = 1; // 1 or 2

    // Allowed placements (tick boxes you want to allow).
    // For 1-lane obstacles use the first 3.
    public bool allowLeft = true;
    public bool allowMiddle = true;
    public bool allowRight = true;

    // For 2-lane obstacles, only these make sense:
    public bool allowLeftMiddle = true;   // spans Left + Middle
    public bool allowMiddleRight = true;  // spans Middle + Right
}
