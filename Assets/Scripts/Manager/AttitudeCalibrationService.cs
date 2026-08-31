using UnityEngine;
using System.Collections.Generic;
public enum AttitudeCalibrationState
{
    Uncalibrated,
    Calibrating,
    Calibrated
}

[RequireComponent(typeof(AttitudeReader))]
public sealed class AttitudeCalibrationService : MonoBehaviour
{
    [SerializeField, Min(0.1f)]
    private float calibrationDurationSeconds = 3f;
    [SerializeField, Min(1f)]
    private float samplingFrequency = 30f;

    [SerializeField, Min(0f)]
    private float movementToleranceDegrees = 8f;

    private AttitudeReader attitudeReader;
    private readonly List<Quaternion> samples = new List<Quaternion>();
    private bool hadCalibrationBeforeCurrentAttempt;
    private Quaternion referenceAttitude;
    private float stableElapsedSeconds;
    private float nextSampleTime;
    
    
    
    public AttitudeCalibrationState State{get; private set;} = AttitudeCalibrationState.Uncalibrated;
    
    public float Progress01 {get; private set;} 
    
    public Quaternion NeutralAttitude {get; private set;} =  Quaternion.identity;
    public bool IsSensorReady =>
        attitudeReader != null && attitudeReader.HasSample;
    public bool IsCalibrated => State == AttitudeCalibrationState.Calibrated;
    
    private void Awake()
    {
        attitudeReader = GetComponent<AttitudeReader>();
    }
    public bool BeginCalibration()
    {
        if (!IsSensorReady)
        {
            return false;
        }
        hadCalibrationBeforeCurrentAttempt = IsCalibrated;
        samples.Clear();

        referenceAttitude = attitudeReader.Attitude;
        stableElapsedSeconds = 0f;
        nextSampleTime = Time.unscaledTime;

        Progress01 = 0f;
        State = AttitudeCalibrationState.Calibrating;

        return true;
    }
    private void Update()
    {
        if (State != AttitudeCalibrationState.Calibrating ||
            !IsSensorReady)
        {
            return;
        }

        Quaternion currentAttitude = attitudeReader.Attitude;

        // Measure how far the phone has rotated from the start
        // of the current stable-hold window.
        float movementDegrees =
            Quaternion.Angle(referenceAttitude, currentAttitude);

        if (movementDegrees > movementToleranceDegrees)
        {
            ResetStabilityWindow(currentAttitude);
            return;
        }

        stableElapsedSeconds += Time.unscaledDeltaTime;

        Progress01 = Mathf.Clamp01(
            stableElapsedSeconds / calibrationDurationSeconds);

        if (Time.unscaledTime < nextSampleTime)
        {
            return;
        }

        samples.Add(currentAttitude);

        // Convert samples per second into seconds between samples.
        nextSampleTime =
            Time.unscaledTime + 1f / samplingFrequency;
        
        if (stableElapsedSeconds >= calibrationDurationSeconds)
        {
            CompleteCalibration();
        }
    }
    
    private void ResetStabilityWindow(Quaternion newReferenceAttitude)
    {
        samples.Clear();

        referenceAttitude = newReferenceAttitude;
        stableElapsedSeconds = 0f;
        nextSampleTime = Time.unscaledTime;

        Progress01 = 0f;
    }
    
    private void CompleteCalibration()
    {
        if (samples.Count == 0)
        {
            ResetStabilityWindow(attitudeReader.Attitude);
            return;
        }

        NeutralAttitude = CalculateAverageAttitude();

        Progress01 = 1f;
        State = AttitudeCalibrationState.Calibrated;
    }
    
    private Quaternion CalculateAverageAttitude()
    {
        Quaternion reference = samples[0];
        Vector4 componentSum = Vector4.zero;

        foreach (Quaternion sample in samples)
        {
            Quaternion alignedSample = sample;

            // q and -q represent the same rotation.
            // Align every sample to the same quaternion hemisphere
            // so equivalent rotations do not cancel each other out.
            if (Quaternion.Dot(reference, sample) < 0f)
            {
                alignedSample = new Quaternion(
                    -sample.x,
                    -sample.y,
                    -sample.z,
                    -sample.w);
            }

            componentSum += new Vector4(
                alignedSample.x,
                alignedSample.y,
                alignedSample.z,
                alignedSample.w);
        }

        Quaternion averageAttitude = new Quaternion(
            componentSum.x,
            componentSum.y,
            componentSum.z,
            componentSum.w);

        // Restore unit length because Unity expects rotation
        // quaternions to be normalized.
        return Quaternion.Normalize(averageAttitude);
    }
    
    public void CancelCalibration()
    {
        if (State != AttitudeCalibrationState.Calibrating)
        {
            return;
        }

        samples.Clear();
        stableElapsedSeconds = 0f;
        Progress01 = 0f;

        State = hadCalibrationBeforeCurrentAttempt
            ? AttitudeCalibrationState.Calibrated
            : AttitudeCalibrationState.Uncalibrated;
    }
    
}