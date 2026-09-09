using UnityEngine;

public sealed class FishingHaptics : MonoBehaviour
{
    [SerializeField] private FishingLoopController loop;
    [SerializeField] private bool vibrationEnabled = true;
    private int lastAttemptId = -1;
    private int lastStrikeAttemptId = -1;

    public bool VibrationEnabled
    {
        get => vibrationEnabled;
        set => vibrationEnabled = value;
    }

    private void OnEnable()
    {
        if (loop == null) return;
        loop.AttemptFailed += HandleFailure;
        loop.StateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        if (loop == null) return;
        loop.AttemptFailed -= HandleFailure;
        loop.StateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(FishingLoopState state)
    {
        if (state != FishingLoopState.Striking || loop == null ||
            lastStrikeAttemptId == loop.CurrentAttemptId) return;
        // Record even muted entries so resuming or enabling vibration never replays a prompt.
        lastStrikeAttemptId = loop.CurrentAttemptId;
        VibrateIfAllowed();
    }

    private void HandleFailure(AttemptFailureResult result)
    {
        if (lastAttemptId == result.AttemptId) return;
        lastAttemptId = result.AttemptId;
        // The last hook still produces feedback; GameOver is already entered at this point.
        VibrateIfAllowed();
    }

    private void VibrateIfAllowed()
    {
        if (!isActiveAndEnabled || !vibrationEnabled || Time.timeScale <= 0f || !Application.isFocused) return;
#if UNITY_ANDROID && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }
}
