using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Tilemaps;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Core")]
    public HUDController hud;
    public LivesDisplay livesDisplay;
    public string levelName = "Level 1";

    [Header("Actors")]
    public PacStudentController pac;
    public GhostStateController[] ghosts;
    public BgmPlayer bgm;

    [Header("Pellets")]
    public Tilemap[] pelletTilemaps;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    public float gameOverHoldSeconds = 3f;
    public string startSceneName = "StartScene";

    int lives = 3;
    bool playing;
    int pelletsRemaining;

    bool hasStartedRound = false;
    bool hasEatenAnyPellet = false;
    bool pelletCountInitialized = false;

    public bool HasStartedRound() => hasStartedRound;

    void Start()
    {
        if (hud) hud.AddScore(0);
        if (hud) hud.gameObject.SetActive(true);
        if (livesDisplay) livesDisplay.SetLives(lives);
        if (hud) hud.gameTimerText.text = "Time: 00:00:00";
        playing = false;

        pelletsRemaining = CountAllPellets();
        if (gameOverPanel) gameOverPanel.SetActive(false);

        Freeze(true);
    }

    public void BeginRound()
    {
        pelletsRemaining = CountAllPellets();
        hasEatenAnyPellet = false;
        hasStartedRound = true;
        pelletCountInitialized = false;

        if (hud) { hud.ResetTimer(); hud.StartTimer(); }
        if (bgm) bgm.PlayNormalLoop();
        Freeze(false);
        playing = true;

        if (gameOverPanel) gameOverPanel.SetActive(false);
        
        // Start SpeedBanana activation sequence
        var speedBananaManager = FindObjectOfType<SpeedBananaManager>();
        speedBananaManager?.BeginGame();
    }

    int CountAllPellets()
    {
        if (pelletTilemaps == null) return 0;
        int total = 0;
        foreach (var tm in pelletTilemaps)
        {
            if (!tm) continue;
            var b = tm.cellBounds;
            foreach (var p in b.allPositionsWithin)
                if (tm.HasTile(p)) total++;
        }
        return total;
    }

    public void OnPelletEaten()
    {
        if (!playing || !hasStartedRound) return;

        if (!pelletCountInitialized)
        {
            pelletsRemaining = Mathf.Max(0, CountAllPellets() - 1);
            pelletCountInitialized = true;
            hasEatenAnyPellet = true;
            hud.AddScore(10);
            if (pelletsRemaining == 0) OnAllPelletsCleared();
            return;
        }

        hasEatenAnyPellet = true;
        hud.AddScore(10);
        pelletsRemaining = Mathf.Max(0, pelletsRemaining - 1);
        if (pelletsRemaining == 0 && hasEatenAnyPellet) OnAllPelletsCleared();
    }

    public void OnCherryEaten()
    {
        if (!playing || !hasStartedRound) return;
        hud.AddScore(100);
    }

    public void OnPowerPillEaten()
    {
        if (!playing || !hasStartedRound) return;
        hud.AddScore(50);
        hud.StartScared(10f);
    }

    public void OnPlayerCaught()
    {
        if (!playing || !hasStartedRound) return;
        lives--;
        if (livesDisplay) livesDisplay.SetLives(lives);
        
        // Notify BonusLifeManager to activate a bonus life when player loses a life
        var bonusLifeManager = FindObjectOfType<BonusLifeManager>();
        bonusLifeManager?.OnLifeLost();
        
        if (lives <= 0) GameOver();
    }

    public void AddLife()
    {
        lives++;
        if (livesDisplay) livesDisplay.SetLives(lives);
    }

    public void OnAllPelletsCleared()
    {
        if (playing && hasStartedRound) GameOver();
    }

    void GameOver()
    {
        if (!playing) return;
        playing = false;
        StartCoroutine(GameOverFlow());
    }

    IEnumerator GameOverFlow()
    {
        if (hud) hud.StopTimer();

        Freeze(true);
        if (bgm) bgm.StopAll();

        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (gameOverText) gameOverText.text = "GAME OVER";

        SaveBest();

        yield return new WaitForSecondsRealtime(gameOverHoldSeconds);
        SceneManager.LoadScene(startSceneName);
    }

    void Freeze(bool on)
    {
        if (pac)
        {
            var rb = pac.GetComponent<Rigidbody2D>();
            pac.enabled = !on;
            if (rb) rb.simulated = !on;
        }
        if (ghosts != null)
        {
            foreach (var g in ghosts)
            {
                if (!g) continue;
                if (g.animator) g.animator.speed = on ? 0f : 1f;
                var rb = g.GetComponent<Rigidbody2D>();
                if (rb && on) { rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f; }
            }
        }
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
