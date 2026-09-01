using System;
using UnityEngine;

public sealed class SessionTimer : MonoBehaviour
{
    [SerializeField, Min(1f)]
    [Tooltip("Total session duration in seconds.")]
    private float sessionDuration = 90f;

    public float TimeRemaining { get; private set; }

    public bool IsRunning { get; private set; }

    public event Action Expired;

    private void Awake()
    {
        ResetTimer();
    }

    private void Update()
    {
        if (!IsRunning)
        {
            return;
        }

        TimeRemaining = Mathf.Max(
            0f,
            TimeRemaining - Time.deltaTime);

        if (TimeRemaining > 0f)
        {
            return;
        }

        IsRunning = false;
        Expired?.Invoke();
    }

    public void BeginSession()
    {
        TimeRemaining = sessionDuration;
        IsRunning = true;
    }

    public void StopTimer()
    {
        IsRunning = false;
    }

    public void ResetTimer()
    {
        TimeRemaining = sessionDuration;
        IsRunning = false;
    }
}