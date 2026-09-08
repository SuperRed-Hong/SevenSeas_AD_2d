using System;
using UnityEngine;

public static class LocalLeaderboard
{
    private const string StorageKey = "SevenSeas.LocalLeaderboard.v1";

    public static bool TryLoad(out LeaderboardData data)
    {
        data = new LeaderboardData();
        try
        {
            if (!PlayerPrefs.HasKey(StorageKey)) return true;
            string json = PlayerPrefs.GetString(StorageKey);
            LeaderboardData saved = JsonUtility.FromJson<LeaderboardData>(json);
            if (saved == null || saved.version != 1 || saved.entries == null) return false;
            saved.Normalize();
            data = saved;
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not load the local leaderboard: {exception.Message}");
            return false;
        }
    }

    public static bool TryRecord(string sessionId, int score)
    {
        // Preserve unreadable data instead of silently replacing the player's history.
        if (!TryLoad(out LeaderboardData data)) return false;
        data.Add(sessionId, score, DateTime.UtcNow);
        try
        {
            PlayerPrefs.SetString(StorageKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not save the local leaderboard: {exception.Message}");
            return false;
        }
    }
}
