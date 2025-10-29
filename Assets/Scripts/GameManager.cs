using UnityEngine;

public class GameManager : MonoBehaviour
{
    public HUDController hud;
    public LivesDisplay livesDisplay;
    public string levelName = "Level 1";

    int lives = 3;
    bool playing;

    void Start()
    {
        if (hud) hud.AddScore(0);
        if (hud) hud.gameObject.SetActive(true);
        if (livesDisplay) livesDisplay.SetLives(lives);
        if (hud) hud.gameTimerText.text = "Time: 00:00:00";
        playing = true;
    }

    public void OnPelletEaten() { if (!playing) return; hud.AddScore(10); }
    public void OnCherryEaten() { if (!playing) return; hud.AddScore(100); }
    public void OnPowerPillEaten() { if (!playing) return; hud.AddScore(50); hud.StartScared(10f); }

    public void OnPlayerCaught()
    {
        if (!playing) return;
        lives--;
        if (livesDisplay) livesDisplay.SetLives(lives);
        if (lives <= 0) GameOver();
    }

    public void OnAllPelletsCleared()
    {
        if (playing) GameOver();
    }

    void GameOver()
    {
        playing = false;
        SaveBest();
    }

    void SaveBest()
    {
        int score = hud.GetScore();
        string time = hud.GetClock();

        int best = PlayerPrefs.GetInt("L1_HighScore", 0);
        string bestTime = PlayerPrefs.GetString("L1_BestTime", "99:59:99");

        bool better = score > best || (score == best && IsFaster(time, bestTime));
        if (better)
        {
            PlayerPrefs.SetInt("L1_HighScore", score);
            PlayerPrefs.SetString("L1_BestTime", time);
            PlayerPrefs.Save();
        }
    }

    bool IsFaster(string a, string b)
    {
        int am = int.Parse(a.Substring(0, 2));
        int as2 = int.Parse(a.Substring(3, 2));
        int ams = int.Parse(a.Substring(6, 2));
        int bm = int.Parse(b.Substring(0, 2));
        int bs2 = int.Parse(b.Substring(3, 2));
        int bms = int.Parse(b.Substring(6, 2));
        int aa = am * 6000 + as2 * 100 + ams;
        int bb = bm * 6000 + bs2 * 100 + bms;
        return aa < bb;
    }
}
