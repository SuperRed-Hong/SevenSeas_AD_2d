using TMPro;
using UnityEngine;

public sealed class CastDebugHUD : MonoBehaviour
{
    [SerializeField] private GyroscopeReader reader;
    [SerializeField] private CastGestureDetector detector;
    [SerializeField] private CastBallController ball;
    [SerializeField] private CastTestController testController;
    [SerializeField] private TMP_Text sensorText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private TMP_Text axisText;
    [SerializeField] private TMP_Text rawVelocityText;
    [SerializeField] private TMP_Text filteredVelocityText;
    [SerializeField] private TMP_Text rawPeakText;
    [SerializeField] private TMP_Text filteredPeakText;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private TMP_Text velocityText;
    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private RectTransform xBar;
    [SerializeField] private RectTransform yBar;
    [SerializeField] private RectTransform zBar;
    [SerializeField, Min(0.1f)] private float barRange = 8f;
    [SerializeField, Min(1f)] private float refreshRate = 30f;

    private float nextRefreshTime;

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.unscaledTime + 1f / refreshRate;
        Refresh();
    }

    public void RestartTest()
    {
        testController?.RestartTest();
        Refresh();
    }

    private void Refresh()
    {
        Vector3 angularVelocity = reader != null
            ? reader.AngularVelocity
            : Vector3.zero;

        sensorText.text = reader != null && reader.IsEnabled
            ? $"GYRO ONLINE  |  {reader.SamplingFrequency:0} Hz"
            : "GYRO OFFLINE";
        stateText.text = $"STATE  {detector.State}";
        axisText.text = $"CAST AXIS  {detector.SelectedAxis}";
        rawVelocityText.text = $"RAW  {detector.DirectedVelocity:+0.00;-0.00;0.00} rad/s";
        filteredVelocityText.text =
            $"FILTERED  {detector.FilteredVelocity:+0.00;-0.00;0.00} rad/s";
        rawPeakText.text = $"RAW PEAK  {detector.RawPeak:0.00}";
        filteredPeakText.text = $"FILTERED PEAK  {detector.FilteredPeak:0.00}";
        powerText.text = $"CAST POWER  {detector.CastPower:P0}";
        velocityText.text =
            $"BALL VELOCITY  {ball.Velocity.x:0.00}, {ball.Velocity.y:0.00}";

        float distance = testController.HasResult
            ? testController.FinalDistance
            : ball.CurrentDistance;
        distanceText.text = testController.HasResult
            ? $"FINAL DISTANCE  {distance:0.00} m"
            : $"DISTANCE  {distance:0.00} m";

        SetCenteredBar(xBar, angularVelocity.x);
        SetCenteredBar(yBar, angularVelocity.y);
        SetCenteredBar(zBar, angularVelocity.z);
    }

    private void SetCenteredBar(RectTransform bar, float value)
    {
        if (bar == null)
        {
            return;
        }

        float normalized = Mathf.Clamp(value / barRange, -1f, 1f);
        float edge = 0.5f + normalized * 0.5f;
        bar.anchorMin = new Vector2(Mathf.Min(0.5f, edge), 0f);
        bar.anchorMax = new Vector2(Mathf.Max(0.5f, edge), 1f);
        bar.offsetMin = Vector2.zero;
        bar.offsetMax = Vector2.zero;
    }
}
