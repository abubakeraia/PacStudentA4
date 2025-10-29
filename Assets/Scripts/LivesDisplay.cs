// LivesDisplay.cs
using UnityEngine;
using UnityEngine.UI;

public class LivesDisplay : MonoBehaviour
{
    public Image[] lifeIcons; 

    public void SetLives(int lives)
    {
        for (int i = 0; i < lifeIcons.Length; i++)
            lifeIcons[i].enabled = i < lives;
    }
}
