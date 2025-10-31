using UnityEngine;

public class BonusWatermelonPickup : MonoBehaviour
{
    public int scoreValue = 100;
    public AudioClip sfx;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Add score - just add directly, GameManager.OnCherryEaten also adds score so we only use one
        var hud = FindObjectOfType<HUDController>();
        hud?.AddScore(scoreValue);

        // Play sound effect if available
        if (sfx) AudioSource.PlayClipAtPoint(sfx, transform.position);
        
        // Destroy the watermelon
        Destroy(gameObject);
    }
}

