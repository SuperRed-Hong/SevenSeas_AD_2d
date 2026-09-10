using UnityEngine;

public static class MotionTestServices
{
    public static GyroscopeReader Resolve(GyroscopeReader localReader)
    {
        if (AppRoot.Instance != null) return AppRoot.Instance.GyroscopeReader;
        if (localReader == null) localReader = Object.FindFirstObjectByType<GyroscopeReader>();
        if (localReader != null) localReader.enabled = true;
        return localReader;
    }

    public static AttitudeReader Resolve(AttitudeReader localReader)
    {
        if (AppRoot.Instance != null) return AppRoot.Instance.AttitudeReader;
        if (localReader == null) localReader = Object.FindFirstObjectByType<AttitudeReader>();
        if (localReader != null) localReader.enabled = true;
        return localReader;
    }

    public static AttitudeCalibrationService ResolveCalibration(AttitudeReader reader)
    {
        if (AppRoot.Instance != null) return AppRoot.Instance.AttitudeCalibration;
        if (reader == null) return null;
        // Direct Editor play uses the same calibration rules without creating AppRoot.
        return reader.GetComponent<AttitudeCalibrationService>() ??
            reader.gameObject.AddComponent<AttitudeCalibrationService>();
    }
}
