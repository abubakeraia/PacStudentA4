using UnityEngine;
using TMPro;

public class StartHighScoreUI : MonoBehaviour
{
    public TextMeshProUGUI bestScoreText;
    public TextMeshProUGUI bestTimeText;
    public GameObject container;

    void Awake() { Refresh(); }

    public void Refresh()
    {
        bool ok = TryRead("L1_HighScore", "L1_BestTime", out int score, out string time);

        if (!ok) ok = TryRead("HS_Score", "HS_TimeStr", out score, out time);
        if (!ok) ok = TryRead("HighScore", "BestTime", out score, out time);

        if (ok)
        {
            if (container) container.SetActive(true);
            if (bestScoreText) bestScoreText.text = $"Best: {score:000000}";
            if (bestTimeText) bestTimeText.text = $"Time: {time}";
        }
        else
        {
            if (container) container.SetActive(true);              
            if (bestScoreText) bestScoreText.text = "Best: 000000";
            if (bestTimeText) bestTimeText.text = "Time: 00:00:00";
        }

        Debug.Log($"[StartHighScoreUI] Keys found: {ok}, Score={score}, Time='{time}'");
    }

    bool TryRead(string scoreKey, string timeKey, out int score, out string time)
    {
        score = PlayerPrefs.HasKey(scoreKey) ? PlayerPrefs.GetInt(scoreKey, 0) : -1;
        time = PlayerPrefs.HasKey(timeKey) ? PlayerPrefs.GetString(timeKey, "") : "";
        return score >= 0 && !string.IsNullOrEmpty(time);
    }
}
