using UnityEngine;
using UnityEngine.SceneManagement;

public class Fireball : MonoBehaviour
{
    public float speed = 10f;
    public float lifetime = 99f;

    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = transform.forward * speed;

        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            AutoRunPlayerController player = collision.gameObject.GetComponent<AutoRunPlayerController>();
            if (player != null && !player.IsInvulnerable)
            {
                // Kill player & reset level if NOT invulnerable
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        // Destroy fireball on any collision
        Destroy(gameObject);
    }
}
