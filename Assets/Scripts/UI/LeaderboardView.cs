using System;
using TMPro;
using UnityEngine;

public sealed class LeaderboardView : MonoBehaviour
{
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text[] scoreRows;
    [SerializeField] private TMP_Text[] dateRows;

    private void OnEnable()
    {
        bool loaded = LocalLeaderboard.TryLoad(out LeaderboardData data);
        statusText.text = !loaded ? "Local records could not be loaded."
            : data.entries.Count == 0 ? "No scores recorded yet. Finish a run to set your first score."
            : "Your best 10 completed runs";

        for (int i = 0; i < scoreRows.Length; i++)
        {
            bool hasEntry = loaded && i < data.entries.Count;
            scoreRows[i].text = hasEntry ? data.entries[i].score.ToString("N0") : "--";
            if (i < dateRows.Length)
            {
                dateRows[i].text = hasEntry
                    ? new DateTime(data.entries[i].completedUtcTicks, DateTimeKind.Utc)
                        .ToLocalTime().ToString("yyyy-MM-dd  HH:mm")
                    : "--";
            }
        }
    }
}
