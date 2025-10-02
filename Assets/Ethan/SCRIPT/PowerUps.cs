using UnityEngine;
using System.Collections;

/// 
/// Handles collectible pickups in an auto-runner game.
/// - Coin: adds 100 points.
/// - Invulnerability: grants invincibility and increases speed for 20 seconds.
/// - Magnet: pulls nearby coins towards the player for 20 seconds.
/// 
public class PickupItems : MonoBehaviour
{
    public enum PickupType { Coin, Invulnerability, Magnet }
    public PickupType pickupType;

    void OnTriggerEnter(Collider other)
    {
        AutoRunPlayerController player = other.GetComponent<AutoRunPlayerController>();
        if (player != null)
        {
            switch (pickupType)
            {
                case PickupType.Coin:
                    player.AddScore(100);
                    UIManager.Instance.AddScore(100); // ?? update UI
                    break;
                case PickupType.Invulnerability:
                    player.StartCoroutine(player.ActivateInvulnerability(20f));
                    break;
                case PickupType.Magnet:
                    player.StartCoroutine(player.ActivateMagnet(20f));
                    break;
            }

            Destroy(gameObject);
        }
    }
}


// Extend AutoRunPlayerController with score, invulnerability, and magnet functionality.

public partial class AutoRunPlayerController : MonoBehaviour
{
    [Header("Power-up Settings")]
    public float invulnerableSpeedMultiplier = 1.5f;
    public float magnetRadius = 10f;

    private bool isInvulnerable = false;
    private bool isMagnetActive = false;
    private int score = 0;

    // Public property for other scripts (like Fireball)
    public bool IsInvulnerable => isInvulnerable;

    public void AddScore(int amount)
    {
        score += amount;
        Debug.Log("Score: " + score);
    }

    public IEnumerator ActivateInvulnerability(float duration)
    {
        if (isInvulnerable) yield break;
        isInvulnerable = true;

        float originalSpeed = forwardSpeed;
        forwardSpeed *= invulnerableSpeedMultiplier;

        Debug.Log("Invulnerability Active!");
        yield return new WaitForSeconds(duration);

        forwardSpeed = originalSpeed;
        isInvulnerable = false;
        Debug.Log("Invulnerability Ended");
    }

    public IEnumerator ActivateMagnet(float duration)
    {
        if (isMagnetActive) yield break;
        isMagnetActive = true;

        Debug.Log("Magnet Active!");

        float timer = duration;
        while (timer > 0)
        {
            timer -= Time.deltaTime;

            // Pull all coins toward the player
            Collider[] hits = Physics.OverlapSphere(transform.position, magnetRadius);
            foreach (Collider hit in hits)
            {
                PickupItems coin = hit.GetComponent<PickupItems>();
                if (coin != null && coin.pickupType == PickupItems.PickupType.Coin)
                {
                    hit.transform.position = Vector3.MoveTowards(
                        hit.transform.position,
                        transform.position,
                        15f * Time.deltaTime
                    );
                }
            }

            yield return null;
        }

        isMagnetActive = false;
        Debug.Log("Magnet Ended");
    }
}

