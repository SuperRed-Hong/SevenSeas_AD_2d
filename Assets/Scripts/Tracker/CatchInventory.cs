using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public sealed class CatchInventory : MonoBehaviour
{
    private readonly List<CatchResult> records = new();
    private readonly HashSet<int> recordedAttempts = new();
    private ReadOnlyCollection<CatchResult> readOnlyRecords;

    public IReadOnlyList<CatchResult> Records => readOnlyRecords ??= records.AsReadOnly();
    public int Count => records.Count;
    public int TotalScore { get; private set; }
    public event Action Changed;

    public void Record(CatchResult result) => TryRecord(result);

    public bool TryRecord(CatchResult result)
    {
        // The coordinator supplies the settled result; never calculate another score here.
        if (!recordedAttempts.Add(result.AttemptId)) return false;
        records.Add(result);
        TotalScore += result.ActualScore;
        Changed?.Invoke();
        return true;
    }

    public void Clear()
    {
        records.Clear();
        recordedAttempts.Clear();
        TotalScore = 0;
        Changed?.Invoke();
    }
}
