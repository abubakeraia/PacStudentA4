using UnityEngine;
using UnityEngine.SceneManagement;

public class PowerPillPickup : MonoBehaviour
{
    public int scoreValue = 50;
    public AudioClip sfx;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        FindObjectOfType<HUDController>()?.AddScore(scoreValue);
        FindObjectOfType<ScareManager>()?.TriggerScared();

        if (sfx) AudioSource.PlayClipAtPoint(sfx, transform.position);
        Destroy(gameObject);
    }
}
