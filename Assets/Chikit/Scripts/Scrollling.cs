using UnityEngine;
using TMPro;

public class CreditsScroll : MonoBehaviour
{
    [Header("Scroll Settings")]
    public float scrollSpeed = 50f;  // Pixels per second
    public float resetDelay = 2f;    // Delay before restarting

    private RectTransform rectTransform;
    private Vector2 startPos;
    private float contentHeight;
    private float viewportHeight;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        startPos = rectTransform.anchoredPosition;

        // Get heights
        contentHeight = rectTransform.rect.height;
        viewportHeight = rectTransform.parent.GetComponent<RectTransform>().rect.height;
    }

    void Update()
    {
        rectTransform.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;

        if (rectTransform.anchoredPosition.y >= contentHeight - viewportHeight)
        {
            StartCoroutine(RestartCredits());
        }
    }

    private System.Collections.IEnumerator RestartCredits()
    {
        yield return new WaitForSeconds(resetDelay);
        rectTransform.anchoredPosition = startPos;
    }
}
