using UnityEngine;
using System.Collections.Generic;

public class BonusLifeManager : MonoBehaviour
{
    [Header("Bonus Life Objects")]
    public GameObject[] bonusLifeObjects;
    
    [Header("Settings")]
    public int maxActiveLives = 3;

    List<GameObject> availableBonusLives = new List<GameObject>();
    List<GameObject> activeBonusLives = new List<GameObject>();
    List<GameObject> usedBonusLives = new List<GameObject>();

    void Awake()
    {
        // Initialize all bonus life objects as inactive and available
        if (bonusLifeObjects != null)
        {
            foreach (var bonusLife in bonusLifeObjects)
            {
                if (bonusLife != null)
                {
                    bonusLife.SetActive(false);
                    availableBonusLives.Add(bonusLife);
                }
            }
        }
    }

    public void OnLifeLost()
    {
        // Only activate a bonus life if we haven't reached max active lives
        if (activeBonusLives.Count >= maxActiveLives)
            return;

        // Find an available bonus life object
        if (availableBonusLives.Count > 0)
        {
            GameObject bonusLife = availableBonusLives[0];
            availableBonusLives.RemoveAt(0);
            activeBonusLives.Add(bonusLife);
            bonusLife.SetActive(true);
        }
    }

    public void OnBonusLifeCollected(GameObject bonusLife)
    {
        // Remove from active list
        if (activeBonusLives.Contains(bonusLife))
        {
            activeBonusLives.Remove(bonusLife);
        }

        // Add to used list (mark as permanently used)
        if (!usedBonusLives.Contains(bonusLife))
        {
            usedBonusLives.Add(bonusLife);
        }

        // Destroy the object
        Destroy(bonusLife);
    }

    public bool IsBonusLifeActive(GameObject obj)
    {
        return activeBonusLives.Contains(obj);
    }
}

