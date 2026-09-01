using System;
using UnityEngine;

public enum CastDetectionState
{
    Unavailable,
    Ready,
    Sampling,
    CastDetected,
    Cooldown
}

public sealed class CastGestureDetector : MonoBehaviour
{
    private GyroscopeReader reader;
    [SerializeField] private CastTuningProfile tuningProfile;
    [SerializeField, Min(0f)] private float castDetectedDisplayDuration = 0.2f;

    private float stateStartTime;
    private bool isArmed = true;

    public event Action<float> CastDetected;
    public event Action GestureTriggered;
    
    public CastDetectionState State { get; private set; }
    public float DirectedVelocity { get; private set; }
    public float FilteredVelocity { get; private set; }
    public float RawPeak { get; private set; }
    public float FilteredPeak { get; private set; }
    public float CastPower { get; private set; }
    public float SamplingProgress { get; private set; }
    public CastSensorAxis SelectedAxis =>
        tuningProfile != null ? tuningProfile.CastAxis : CastSensorAxis.X;

    private void OnEnable()
    {
        ResetDetector();
    }

    public void Configure(GyroscopeReader sensorReader, CastTuningProfile profile)
    {
        reader = sensorReader;
        tuningProfile = profile;
    }
    public void ConfigureReader(GyroscopeReader sensorReader)
    {
        reader = sensorReader;
        ResetDetector();
    }
    private void Update()
    {
        if (reader == null || tuningProfile == null || !reader.IsEnabled)
        {
            State = CastDetectionState.Unavailable;
            DirectedVelocity = 0f;
            FilteredVelocity = 0f;
            return;
        }

        DirectedVelocity = ReadDirectedVelocity(reader.AngularVelocity);
        float blend = tuningProfile.FilterSharpness > 0f
            ? 1f - Mathf.Exp(-tuningProfile.FilterSharpness * Time.unscaledDeltaTime)
            : 1f;
        FilteredVelocity = Mathf.Lerp(FilteredVelocity, DirectedVelocity, blend);

        switch (State)
        {
            case CastDetectionState.Unavailable:
                EnterReadyState();
                break;

            case CastDetectionState.Ready:
                UpdateReadyState();
                break;

            case CastDetectionState.Sampling:
                UpdateSamplingState();
                break;

            case CastDetectionState.CastDetected:
                if (Time.unscaledTime - stateStartTime >= castDetectedDisplayDuration)
                {
                    State = CastDetectionState.Cooldown;
                    stateStartTime = Time.unscaledTime;
                }
                break;

            case CastDetectionState.Cooldown:
                if (Time.unscaledTime - stateStartTime >= tuningProfile.CooldownDuration)
                {
                    EnterReadyState();
                }
                break;
        }
    }

    public void ResetDetector()
    {
        State = reader != null && reader.IsEnabled
            ? CastDetectionState.Ready
            : CastDetectionState.Unavailable;
        DirectedVelocity = 0f;
        FilteredVelocity = 0f;
        RawPeak = 0f;
        FilteredPeak = 0f;
        CastPower = 0f;
        SamplingProgress = 0f;
        stateStartTime = Time.unscaledTime;
        isArmed = true;
    }

    private void UpdateReadyState()
    {
        if (!isArmed)
        {
            isArmed = Mathf.Abs(DirectedVelocity) <= tuningProfile.RearmThreshold;
            return;
        }

        if (DirectedVelocity < tuningProfile.TriggerThreshold)
        {
            return;
        }

        State = CastDetectionState.Sampling;
        stateStartTime = Time.unscaledTime;
        RawPeak = Mathf.Max(0f, DirectedVelocity);
        FilteredPeak = Mathf.Max(0f, FilteredVelocity);
        SamplingProgress = 0f;
        isArmed = false;
        
        // Notify binary gesture consumers immediately.
        // Cast power calculation continues through the Sampling state.
        GestureTriggered?.Invoke();
    }

    private void UpdateSamplingState()
    {
        RawPeak = Mathf.Max(RawPeak, DirectedVelocity);
        FilteredPeak = Mathf.Max(FilteredPeak, FilteredVelocity);

        float elapsed = Time.unscaledTime - stateStartTime;
        SamplingProgress = Mathf.Clamp01(elapsed / tuningProfile.SampleWindow);

        if (elapsed < tuningProfile.SampleWindow)
        {
            return;
        }

        CastPower = tuningProfile.EvaluatePower(FilteredPeak);
        State = CastDetectionState.CastDetected;
        stateStartTime = Time.unscaledTime;
        SamplingProgress = 1f;
        CastDetected?.Invoke(CastPower);
    }

    private void EnterReadyState()
    {
        State = CastDetectionState.Ready;
        stateStartTime = Time.unscaledTime;
        SamplingProgress = 0f;
        isArmed = Mathf.Abs(DirectedVelocity) <= tuningProfile.RearmThreshold;
    }

    private float ReadDirectedVelocity(Vector3 angularVelocity)
    {
        float value = tuningProfile.CastAxis switch
        {
            CastSensorAxis.X => angularVelocity.x,
            CastSensorAxis.Y => angularVelocity.y,
            CastSensorAxis.Z => angularVelocity.z,
            _ => 0f
        };

        return tuningProfile.InvertAxis ? -value : value;
    }
}
