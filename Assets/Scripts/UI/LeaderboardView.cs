using System;
using TMPro;
using UnityEngine;

public sealed class LeaderboardView : MonoBehaviour
{
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text[] scoreRows;
    [SerializeField] private TMP_Text[] dateRows;
    [SerializeField] private TMP_Text[] rankRows;
    [SerializeField] private Color rowColor = new Color(0.23f, 0.16f, 0.1f);
    [SerializeField] private Color currentRunColor = new Color(0.65f, 0.22f, 0.07f);

    private void OnEnable()
    {
        bool loaded = LocalLeaderboard.TryLoad(out LeaderboardData data);
        int currentRank = loaded ? data.entries.FindIndex(entry =>
            entry.sessionId == LocalLeaderboard.LastSessionId) : -1;
        statusText.text = !loaded ? "RECORDS UNAVAILABLE"
            : string.IsNullOrEmpty(LocalLeaderboard.LastSessionId)
                ? (data.entries.Count == 0 ? "NO RECORDS YET" : "LOCAL TOP 10")
                : $"THIS RUN  {LocalLeaderboard.LastScore:N0}\n" +
                  (!LocalLeaderboard.LastSaveSucceeded ? "SCORE NOT SAVED"
                  : currentRank >= 0 ? $"RANK {currentRank + 1:00}" : "OUTSIDE TOP 10");

        for (int i = 0; i < scoreRows.Length; i++)
        {
            bool hasEntry = loaded && i < data.entries.Count;
            Color color = hasEntry && i == currentRank ? currentRunColor : rowColor;
            scoreRows[i].color = color;
            if (rankRows != null && i < rankRows.Length) rankRows[i].color = color;
            scoreRows[i].text = hasEntry ? data.entries[i].score.ToString("N0") : "--";
            if (i < dateRows.Length)
            {
                dateRows[i].color = color;
                dateRows[i].text = hasEntry
                    ? new DateTime(data.entries[i].completedUtcTicks, DateTimeKind.Utc)
                        .ToLocalTime().ToString("MM-dd HH:mm")
                    : "--";
            }
        }
    }
}
