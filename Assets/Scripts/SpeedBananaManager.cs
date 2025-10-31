using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpeedBananaManager : MonoBehaviour
{
    [Header("Speed Banana Objects")]
    public GameObject[] speedBananaObjects;
    
    [Header("Settings")]
    public float firstActivationDelay = 3f; // Activate first one at 3 seconds
    public float minRandomDelay = 3f; // Minimum delay for random activations
    public float maxRandomDelay = 20f; // Maximum delay for random activations
    public int maxActivations = 4; // At most 4 should be activated

    List<GameObject> availableBananas = new List<GameObject>();
    List<GameObject> activeBananas = new List<GameObject>();
    List<GameObject> usedBananas = new List<GameObject>();
    int activationCount = 0;
    bool hasStarted = false;

    void Start()
    {
        // Initialize all speed banana objects as inactive
        if (speedBananaObjects != null)
        {
            foreach (var banana in speedBananaObjects)
            {
                if (banana != null)
                {
                    banana.SetActive(false);
                    availableBananas.Add(banana);
                }
            }
        }
    }

    public void BeginGame()
    {
        if (hasStarted) return;
        hasStarted = true;
        StartCoroutine(ActivateBananas());
    }

    IEnumerator ActivateBananas()
    {
        // Wait for first activation delay (3 seconds)
        yield return new WaitForSeconds(firstActivationDelay);

        // Activate the first banana
        if (availableBananas.Count > 0 && activationCount < maxActivations)
        {
            ActivateRandomBanana();
        }

        // Activate remaining bananas (3 more for a total of 4) at random intervals
        while (activationCount < maxActivations && availableBananas.Count > 0)
        {
            // Random delay between 3-20 seconds
            float randomDelay = Random.Range(minRandomDelay, maxRandomDelay);
            yield return new WaitForSeconds(randomDelay);

            if (availableBananas.Count > 0 && activationCount < maxActivations)
            {
                ActivateRandomBanana();
            }
        }
    }

    void ActivateRandomBanana()
    {
        if (availableBananas.Count == 0) return;

        // Pick a random available banana
        int randomIndex = Random.Range(0, availableBananas.Count);
        GameObject banana = availableBananas[randomIndex];
        
        if (banana != null)
        {
            availableBananas.RemoveAt(randomIndex);
            activeBananas.Add(banana);
            banana.SetActive(true);
            activationCount++;
        }
    }

    public void OnSpeedBananaCollected(GameObject banana)
    {
        // Remove from active list
        if (activeBananas.Contains(banana))
        {
            activeBananas.Remove(banana);
        }

        // Add to used list (mark as permanently used)
        if (!usedBananas.Contains(banana))
        {
            usedBananas.Add(banana);
        }

        // Destroy the object
        Destroy(banana);
    }

    public bool IsSpeedBananaActive(GameObject obj)
    {
        return activeBananas.Contains(obj);
    }
}

