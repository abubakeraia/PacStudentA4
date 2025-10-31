using UnityEngine;

public class BonusLifePickup : MonoBehaviour
{
    public AudioClip sfx;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Check if this bonus life is managed by BonusLifeManager
        var manager = FindObjectOfType<BonusLifeManager>();
        if (manager != null && !manager.IsBonusLifeActive(gameObject))
        {
            // Not active or already used, ignore
            return;
        }

        // Add a life to the player
        var gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.AddLife();
        }

        // Notify manager that this bonus life was collected
        if (manager != null)
        {
            manager.OnBonusLifeCollected(gameObject);
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

