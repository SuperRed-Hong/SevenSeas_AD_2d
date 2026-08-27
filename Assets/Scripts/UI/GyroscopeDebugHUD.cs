using TMPro;
using UnityEngine;

public sealed class GyroscopeDebugHUD : MonoBehaviour
{
    [SerializeField] private GyroscopeReader reader;
    [SerializeField] private GyroscopeHistoryGraphic historyGraphic;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text xValueText;
    [SerializeField] private TMP_Text yValueText;
    [SerializeField] private TMP_Text zValueText;
    [SerializeField] private TMP_Text magnitudeText;
    [SerializeField] private TMP_Text peakText;
    [SerializeField] private RectTransform xBar;
    [SerializeField] private RectTransform yBar;
    [SerializeField] private RectTransform zBar;
    [SerializeField, Min(0.1f)] private float displayRange = 6f;
    [SerializeField, Min(1f)] private float refreshRate = 20f;

    private float peak;
    private float nextRefreshTime;

    private void Awake()
    {
        if (reader == null)
        {
            reader = FindFirstObjectByType<GyroscopeReader>();
        }

        if (historyGraphic != null)
        {
            historyGraphic.DisplayRange = displayRange;
        }
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.unscaledTime + 1f / refreshRate;
        Refresh();
    }

    private void Refresh()
    {
        if (reader == null || !reader.IsAvailable)
        {
            SetOfflineState();
            return;
        }

        Vector3 value = reader.AngularVelocity;
        peak = Mathf.Max(peak, reader.AngularSpeed);

        statusText.text =
            $"GYRO ONLINE  |  {reader.SamplingFrequency:0} Hz  |  SCALE +/-{displayRange:0.#} rad/s";
        xValueText.text = $"X  {value.x:+0.00;-0.00; 0.00}";
        yValueText.text = $"Y  {value.y:+0.00;-0.00; 0.00}";
        zValueText.text = $"Z  {value.z:+0.00;-0.00; 0.00}";
        magnitudeText.text = $"MAG  {reader.AngularSpeed:0.00}";
        peakText.text = $"PEAK  {peak:0.00}";

        SetCenteredBar(xBar, value.x);
        SetCenteredBar(yBar, value.y);
        SetCenteredBar(zBar, value.z);
    }

    private void SetOfflineState()
    {
        statusText.text = "GYRO OFFLINE";
        xValueText.text = "X   0.00";
        yValueText.text = "Y   0.00";
        zValueText.text = "Z   0.00";
        magnitudeText.text = "MAG  0.00";
        peakText.text = "PEAK  0.00";
        SetCenteredBar(xBar, 0f);
        SetCenteredBar(yBar, 0f);
        SetCenteredBar(zBar, 0f);
    }

    private void SetCenteredBar(RectTransform bar, float value)
    {
        if (bar == null)
        {
            return;
        }

        float normalized = Mathf.Clamp(value / displayRange, -1f, 1f);
        float edge = 0.5f + normalized * 0.5f;

        bar.anchorMin = new Vector2(Mathf.Min(0.5f, edge), 0f);
        bar.anchorMax = new Vector2(Mathf.Max(0.5f, edge), 1f);
        bar.offsetMin = Vector2.zero;
        bar.offsetMax = Vector2.zero;
    }
}
