using UnityEngine;

public sealed class CastTestController : MonoBehaviour
{
    [SerializeField] private CastGestureDetector detector;
    [SerializeField] private CastBallController ball;
    [SerializeField] private CastCameraFollow cameraFollow;

    public bool TrialStarted { get; private set; }
    public bool HasResult { get; private set; }
    public float FinalDistance { get; private set; }

    private void OnEnable()
    {
        if (detector != null)
        {
            detector.CastDetected += HandleCastDetected;
        }

        if (ball != null)
        {
            ball.Landed += HandleBallLanded;
        }
    }

    private void Start()
    {
        RestartTest();
    }

    private void OnDisable()
    {
        if (detector != null)
        {
            detector.CastDetected -= HandleCastDetected;
        }

        if (ball != null)
        {
            ball.Landed -= HandleBallLanded;
        }
    }

    public void RestartTest()
    {
        TrialStarted = false;
        HasResult = false;
        FinalDistance = 0f;
        detector?.ResetDetector();
        ball?.ResetBall();
        cameraFollow?.ResetCamera();
    }

    private void HandleCastDetected(float power)
    {
        if (TrialStarted)
        {
            return;
        }

        TrialStarted = true;
        ball?.Launch(power);
    }

    private void HandleBallLanded(float distance)
    {
        FinalDistance = distance;
        HasResult = true;
    }
}
