using UnityEngine;

public class Enemy : MonoBehaviour
{
    public GameObject fireballPrefab;   // Assign fireball prefab in inspector
    public Transform firePoint;         // Fireball spawn point
    public float fireRate = 1.5f;       // Time between shots

    private void Start()
    {
        InvokeRepeating(nameof(ShootFireball), 0f, fireRate);
    }

    void ShootFireball()
    {
        if (fireballPrefab != null && firePoint != null)
        {
            Instantiate(fireballPrefab, firePoint.position, firePoint.rotation);
        }
    }
}
