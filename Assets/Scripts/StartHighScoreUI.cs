using UnityEngine;
using TMPro;

public class StartHighScoreUI : MonoBehaviour
{
    public TextMeshProUGUI bestScoreText;
    public TextMeshProUGUI bestTimeText;
    public GameObject container; 

    void OnEnable() => Refresh();

    public void Refresh()
    {
        if (!HighScoreStore.HasAny())
        {
            if (container) container.SetActive(false);
            else
            {
                if (bestScoreText) bestScoreText.text = "Best: 000000";
                if (bestTimeText) bestTimeText.text = "Time: 00:00:00";
            }
            return;
        }

        var (s, _, ts) = HighScoreStore.Load();
        if (container) container.SetActive(true);
        if (bestScoreText) bestScoreText.text = $"Best: {s:000000}";
        if (bestTimeText) bestTimeText.text = $"Time: {ts}";
    }
}
