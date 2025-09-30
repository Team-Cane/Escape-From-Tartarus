using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI References")]
    public TMP_Text scoreText;
    public TMP_Text healthText;

    private int score = 0;
    private int health = 2;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        UpdateScoreUI();
        UpdateHealthUI();
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreUI();
    }

    public void ReduceHealth(int amount)
    {
        health -= amount;
        UpdateHealthUI();

        if (health <= 0)
        {
            // Handle game over logic here
            Debug.Log("Game Over!");
        }
    }

    private void UpdateScoreUI()
    {
       if (scoreText) scoreText.text = "Score: " + score;
    }

    private void UpdateHealthUI()
    {
        if (healthText) healthText.text = "Health: " + health;
    }
}