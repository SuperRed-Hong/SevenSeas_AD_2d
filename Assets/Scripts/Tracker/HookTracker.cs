using System;
using UnityEngine;

public sealed class HookTracker : MonoBehaviour
{
    [SerializeField, Min(1)]
    [Tooltip("Number of hooks available when a session starts.")]
    private int startingHooks = 5;

    public int HooksRemaining { get; private set; }

    public bool HasHooksRemaining => HooksRemaining > 0;

    public event Action<int> HooksChanged;

    private void Awake()
    {
        ResetHooks();
    }

    public void LoseHook()
    {
        if (HooksRemaining <= 0)
        {
            return;
        }

        HooksRemaining--;
        HooksChanged?.Invoke(HooksRemaining);
    }

    public void ResetHooks()
    {
        HooksRemaining = startingHooks;
        HooksChanged?.Invoke(HooksRemaining);
    }
}