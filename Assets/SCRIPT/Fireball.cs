using UnityEngine;
using UnityEngine.SceneManagement;

public class Fireball : MonoBehaviour
{
    public float speed = 10f;
    public float lifetime = 99f;

    private Rigidbody rb;
    private bool hasHit = false; // prevents double damage

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = transform.forward * speed;

        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return; // already hit something, ignore further collisions
        hasHit = true;

        if (collision.gameObject.CompareTag("Player"))
        {
            AutoRunPlayerController player = collision.gameObject.GetComponent<AutoRunPlayerController>();
            if (player != null && !player.IsInvulnerable)
            {
                // Reduce health and apply slowdown
                player.TakeDamage(1, applySlowdown: true);
            }
        }

        // Destroy fireball immediately to prevent further collisions
        Destroy(gameObject);
    }
}
