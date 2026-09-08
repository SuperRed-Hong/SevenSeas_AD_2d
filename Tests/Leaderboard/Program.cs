using System;
using System.Collections.Generic;

internal static class Program
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Main()
    {
        var start = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
        var board = new LeaderboardData();
        for (int i = 0; i < 15; i++) board.Add("run-" + i, i * 10, start.AddMinutes(i));
        Check(board.entries.Count == 10, "Only the best ten runs should remain.");
        Check(board.entries[0].score == 140 && board.entries[9].score == 50,
            "Scores must sort descending and trim the lowest results.");
        board.Add("run-14", 999, start.AddDays(1));
        Check(board.entries.Count == 10 && board.entries[0].score == 140,
            "Repeating the same session must not replace or duplicate its score.");

        var ties = new LeaderboardData();
        ties.Add("later", 100, start.AddMinutes(1));
        ties.Add("earlier", 100, start);
        Check(ties.entries[0].sessionId == "earlier", "Earlier runs should win score ties.");
        ties.Add("negative", -1, start);
        ties.Add("zero", 0, start);
        Check(ties.entries.Count == 3 && ties.entries[2].score == 0,
            "Zero is a valid completed score; negative scores are invalid.");

        ties.entries.Add(null);
        ties.entries.Add(new LeaderboardEntry { sessionId = "bad-date", score = 200, completedUtcTicks = long.MaxValue });
        ties.entries.Add(new LeaderboardEntry { sessionId = "", score = 200, completedUtcTicks = start.Ticks });
        ties.entries.Add(ties.entries[0]);
        ties.Normalize();
        Check(ties.entries.Count == 3, "Malformed entries and duplicate sessions must be removed.");
        ties.entries = null;
        ties.Normalize();
        Check(ties.entries.Count == 0, "Missing lists should become an empty board.");
        Console.WriteLine("PASS: top ten, descending scores, duplicate sessions, ties, zero/negative scores, invalid dates and empty data.");
    }
}
