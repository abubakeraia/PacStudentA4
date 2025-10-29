using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    public TextMeshProUGUI scoreText, gameTimerText, scaredTimerText;

    int score = 0; float t = 0f; float scaredLeft = 0f;

    void Update()
    {
        t += Time.deltaTime;
        gameTimerText.text = $"Time: {ToClock(t)}";

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

    public void AddScore(int d) { score = Mathf.Max(0, score + d); scoreText.text = $"Score: {score:000000}"; }
    public void StartScared(float seconds) { scaredLeft = seconds; scaredTimerText.gameObject.SetActive(true); }

    public int GetScore() { return score; }
    public string GetClock() { return ToClock(t); }

    string ToClock(float s) { int ms = Mathf.FloorToInt((s % 1f) * 100f), sec = Mathf.FloorToInt(s) % 60, min = Mathf.FloorToInt(s / 60f); return $"{min:00}:{sec:00}:{ms:00}"; }
}
