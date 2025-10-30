using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    public TextMeshProUGUI scoreText, gameTimerText, scaredTimerText;

    int score = 0;
    float t = 0f;
    float scaredLeft = 0f;
    bool timerRunning = false;

    void Update()
    {
        if (timerRunning)
        {
            t += Time.deltaTime;
            gameTimerText.text = $"Time: {ToClock(t)}";
        }

        if (scaredLeft > 0f)
        {
            scaredLeft -= Time.deltaTime;
            int s = Mathf.Max(0, Mathf.CeilToInt(scaredLeft));
            if (!scaredTimerText.gameObject.activeSelf) scaredTimerText.gameObject.SetActive(true);
            scaredTimerText.text = $"Scared: {s}";
        }
        else if (scaredTimerText.gameObject.activeSelf)
        {
            scaredTimerText.gameObject.SetActive(false);
        }
    }

    public void AddScore(int d)
    {
        score = Mathf.Max(0, score + d);
        if (scoreText) scoreText.text = $"Score: {score:000000}";
    }

    public void StartScared(float seconds)
    {
        scaredLeft = seconds;
        if (scaredTimerText) scaredTimerText.gameObject.SetActive(true);
    }

    public void ResetTimer()
    {
        t = 0f;
        if (gameTimerText) gameTimerText.text = "Time: 00:00:00";
    }

    public void StartTimer() { timerRunning = true; }
    public void StopTimer() { timerRunning = false; }

    public int GetScore() { return score; }
    public string GetClock() { return ToClock(t); }

    string ToClock(float s)
    {
        int ms = Mathf.FloorToInt((s % 1f) * 100f);
        int sec = Mathf.FloorToInt(s) % 60;
        int min = Mathf.FloorToInt(s / 60f);
        return $"{min:00}:{sec:00}:{ms:00}";
    }
}
