using System;
using System.Collections.Generic;

[Serializable]
public sealed class LeaderboardEntry
{
    public string sessionId;
    public int score;
    public long completedUtcTicks;
}

[Serializable]
public sealed class LeaderboardData
{
    public int version = 1;
    public List<LeaderboardEntry> entries = new();
    public const int Capacity = 10;

    public void Normalize()
    {
        if (entries == null) entries = new List<LeaderboardEntry>();
        entries.RemoveAll(entry => entry == null || entry.score < 0 ||
            string.IsNullOrEmpty(entry.sessionId) || entry.completedUtcTicks <= 0 ||
            entry.completedUtcTicks > DateTime.MaxValue.Ticks);
        entries.Sort((left, right) =>
        {
            int scoreOrder = right.score.CompareTo(left.score);
            if (scoreOrder != 0) return scoreOrder;
            int dateOrder = left.completedUtcTicks.CompareTo(right.completedUtcTicks);
            return dateOrder != 0 ? dateOrder : string.CompareOrdinal(left.sessionId, right.sessionId);
        });

        var sessions = new HashSet<string>();
        entries.RemoveAll(entry => !sessions.Add(entry.sessionId));
        if (entries.Count > Capacity) entries.RemoveRange(Capacity, entries.Count - Capacity);
    }

    public void Add(string sessionId, int score, DateTime completedUtc)
    {
        Normalize();
        if (score < 0 || string.IsNullOrEmpty(sessionId) ||
            entries.Exists(entry => entry.sessionId == sessionId)) return;

        entries.Add(new LeaderboardEntry
        {
            sessionId = sessionId,
            score = score,
            completedUtcTicks = completedUtc.ToUniversalTime().Ticks
        });
        Normalize();
    }
}
