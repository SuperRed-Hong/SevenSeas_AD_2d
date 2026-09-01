using System;
using UnityEngine;

public sealed class ScoreTracker : MonoBehaviour
{
    public int Score { get; private set; }

    public event Action<int> ScoreChanged;

    public void AddScore(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Score += amount;
        ScoreChanged?.Invoke(Score);
    }

    public void ResetScore()
    {
        Score = 0;
        ScoreChanged?.Invoke(Score);
    }
}