using UnityEngine;

public static class HighScoreStore
{
    const string ScoreKey = "HS_Score";
    const string TimeSecKey = "HS_TimeSec";
    const string TimeStrKey = "HS_TimeStr"; 

    public static bool HasAny() => PlayerPrefs.HasKey(ScoreKey);

    public static (int score, float timeSec, string timeStr) Load()
    {
        if (!HasAny()) return (0, 0f, "00:00:00");
        int s = PlayerPrefs.GetInt(ScoreKey, 0);
        float t = PlayerPrefs.GetFloat(TimeSecKey, 0f);
        string ts = PlayerPrefs.GetString(TimeStrKey, "00:00:00");
        return (s, t, ts);
    }

    public static void SaveIfBest(int score, float timeSec, string timeStr)
    {
        if (!HasAny())
        {
            Commit(score, timeSec, timeStr);
            return;
        }

        var (bestScore, bestTimeSec, _) = Load();
        bool isBetter = score > bestScore || (score == bestScore && timeSec < bestTimeSec);
        if (isBetter) Commit(score, timeSec, timeStr);
    }

    static void Commit(int score, float timeSec, string timeStr)
    {
        PlayerPrefs.SetInt(ScoreKey, score);
        PlayerPrefs.SetFloat(TimeSecKey, timeSec);
        PlayerPrefs.SetString(TimeStrKey, timeStr);
        PlayerPrefs.Save();
    }

    public static void Reset()
    {
        PlayerPrefs.DeleteKey(ScoreKey);
        PlayerPrefs.DeleteKey(TimeSecKey);
        PlayerPrefs.DeleteKey(TimeStrKey);
    }
}
