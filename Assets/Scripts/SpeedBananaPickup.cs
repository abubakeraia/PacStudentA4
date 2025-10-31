using UnityEngine;

public class SpeedBananaPickup : MonoBehaviour
{
    public AudioClip sfx;
    public float speedBoostMultiplier = 1.5f; // e.g., 1.5x normal speed (optional, controller has its own)
    public float speedBoostDuration = 5f; // Duration of the speed boost

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Check if this speed banana is managed by SpeedBananaManager
        var manager = FindObjectOfType<SpeedBananaManager>();
        if (manager != null && !manager.IsSpeedBananaActive(gameObject))
        {
            // Not active or already used, ignore
            return;
        }

        // Apply speed boost to player
        var player = other.GetComponent<PacStudentController>();
        if (player != null)
        {
            Debug.Log($"SpeedBanana collected! Applying boost with multiplier {speedBoostMultiplier} for {speedBoostDuration} seconds");
            player.ApplySpeedBoost(speedBoostDuration);
        }
        else
        {
            Debug.LogWarning("SpeedBananaPickup: Could not find PacStudentController on player!");
        }
        
        // Notify manager that this speed banana was collected
        if (manager != null)
        {
            manager.OnSpeedBananaCollected(gameObject);
        }
        else
        {
            // Fallback: just destroy if no manager
            Destroy(gameObject);
        }

        // Play sound effect if available
        if (sfx) AudioSource.PlayClipAtPoint(sfx, transform.position);
    }
}

