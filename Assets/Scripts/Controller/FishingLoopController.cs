using UnityEngine;
using UnityEngine.Serialization;


public enum FishingLoopState
{
    ReadyToCast,
    Casting,
    Baiting,
    Striking,
    Reeling,
    GameOver
}

[RequireComponent(typeof(CastGestureDetector))]
public sealed class FishingLoopController : MonoBehaviour
{
    #region SerializedReferences

    [Header("Gameplay Controllers")] [SerializeField]
    private ShoreLaneController shoreLaneController;

    [SerializeField] private FishBiteRaceController fishBiteRaceController;
    [SerializeField] private ReelingController reelingController;
    [SerializeField] private FishingHookController hookController;
    [SerializeField] private StrikeController strikeController;
    [SerializeField] private FishingCameraController cameraController;

    
    
    [Header("Input Sources")] 
    [SerializeField] private FishingInputSource inputSource;
    [SerializeField] private CastGestureDetector castGestureDetector;
    [SerializeField] private CastGestureDetector strikeGestureDetector;

    [Header("Session Tracking")] 
    [SerializeField] private ScoreTracker scoreTracker;
    [SerializeField] private HookTracker hookTracker;
    [SerializeField] private SessionTimer sessionTimer;
    [SerializeField] private CatchInventory catchInventory;


    [Header("Tuning Profiles")] [SerializeField] [Tooltip("Configuration for Score System")]
    private ScoreTuningProfile scoreTuningProfile;

    [SerializeField] [Tooltip("Shared configuration for the fishing session.")]
    private FishingLoopProfile loopProfile;

    [SerializeField] private CatchRewardProfile catchRewardProfile;
    [SerializeField] private FishEcologyProfile fishEcologyProfile;

    #endregion


    #region Runtime State and Public Properties

    //Runtime state belongs to the controller, not the shared profile.
    private float castCooldownRemaining;
    private int attemptId;
    private bool attemptSettled;
    private bool fishReplacedThisAttempt;
    public int CurrentAttemptId => attemptId;
    public event System.Action<CatchResult> CatchCompleted;
    public event System.Action<AttemptFailureResult> AttemptFailed;
    public event System.Action HookedFishReplaced;
    public event System.Action StrikeRejected;
    private readonly string leaderboardSessionId = System.Guid.NewGuid().ToString("N");
    public bool FinalScoreSaved { get; private set; }
    public bool IsCastCoolingDown => castCooldownRemaining > 0f;
    public Vector2 LandingPosition { get; private set; }
    public float SelectedLaneX { get; private set; }
    public float SelectedCastPower { get; private set; }
    public FishController HookedFish { get; private set; }
    public Vector3 HookPosition => fishBiteRaceController.transform.position;
    public bool CanProcessFishInteractions => isActiveAndEnabled && Time.timeScale > 0f && Time.deltaTime > 0f &&
        CurrentState == FishingLoopState.Reeling && !attemptSettled &&
        sessionTimer != null && sessionTimer.IsRunning && sessionTimer.TimeRemaining > 0f &&
        reelingController != null && reelingController.isActiveAndEnabled && reelingController.IsActive;

    public float CurrentDistanceMultiplier { get; private set; } = 1f;

    public event System.Action DistanceMultiplierLocked;

    public event System.Action<FishingLoopState> StateChanged;
    
    public float AcceleratedCatchDistance { get; private set; }

    public FishingLoopState CurrentState { get; private set; } = FishingLoopState.ReadyToCast;


    private float castDistance;
    public float CurrentCastDistance => reelingController.DistanceToShore;
    // Polled by audio every frame, including while no retrieval is active.
    public float ReelingTension01 => reelingController != null ? reelingController.Tension01 : 0f;
    public float CastDistance => castDistance;

    #endregion

    #region Initialization

    public void PrepareForEntry()
    {
        // Keep actors and ambient visuals active while entry owns the start gate.
        // Disabling this component postpones Start(), including the session timer.
        enabled = false;
        shoreLaneController.enabled = false;
        castGestureDetector.enabled = false;
        strikeGestureDetector.enabled = false;
        inputSource?.SetMoveEnabled(false);
        inputSource?.SetCastEnabled(false);
        inputSource?.SetStrikeEnabled(false);
        inputSource?.SetAccelerateEnabled(false);
    }

    private void Start()
    {
        if (loopProfile == null)
        {
            Debug.LogError("FishingLoopController requires a FishingLoopProfile", this);

            enabled = false;
            return;
        }

        if (scoreTuningProfile == null)
        {
            Debug.LogError("FishingLoopController requires a ScoreTuningProfile", this);

            enabled = false;
            return;
        }

        // Mobile entry provides AppRoot and the gyroscope reader.
        // Direct Editor testing can continue without motion services.
        if (AppRoot.Instance != null)
        {
            GyroscopeReader reader =
                AppRoot.Instance.GyroscopeReader;

            castGestureDetector.ConfigureReader(reader);
            strikeGestureDetector.ConfigureReader(reader);
        }

        sessionTimer.BeginSession();
        catchInventory?.Clear();
        EnterState(CurrentState);
    }

    #endregion

    #region State Transitions and Cast Input

    private void TransitionTo(FishingLoopState nextState)
    {
        if (CurrentState == nextState)
        {
            return;
        }

        //Capture the source state before CurrentState is overwritten.

        bool shouldStartCooldown =
            nextState == FishingLoopState.ReadyToCast &&
            (CurrentState == FishingLoopState.Casting ||
             CurrentState == FishingLoopState.Striking || CurrentState == FishingLoopState.Reeling);


        ExitState(CurrentState);

        castCooldownRemaining = shouldStartCooldown ? loopProfile.PostAttemptCooldown : 0f;
        CurrentState = nextState;
        EnterState(CurrentState);
        
        StateChanged?.Invoke(CurrentState);
    }


    private void SetCastingAvailable(bool available)
    {
        castGestureDetector.enabled = available;
        inputSource?.SetCastEnabled(available);
    }

    #endregion

    #region Frame Dispatch

    private void Update()
    {
        switch (CurrentState)
        {
            case FishingLoopState.ReadyToCast:
                UpdateReadyToCast();
                break;

            case FishingLoopState.Casting:
                UpdateCasting();
                break;

            case FishingLoopState.Baiting:
                UpdateBaiting();
                break;
            case FishingLoopState.Striking:
                UpdateStriking();
                break;
            case FishingLoopState.Reeling:
                UpdateReeling();
                break;
            case FishingLoopState.GameOver:
                UpdateGameOver();
                break;
        }
    }

    #endregion

    #region Event Subscriptions

    private void OnEnable()
    {
        if (catchInventory != null) CatchCompleted += catchInventory.Record;
        hookController.Landed += HandleHookLanded;

        fishBiteRaceController.FishHooked += HandleFishHooked;
        fishBiteRaceController.TimedOut += HandleBaitingTimedOut;


        strikeController.Succeeded += HandleStrikeSucceeded;
        strikeController.TimedOut += HandleStrikeTimedOut;
        strikeController.AttemptRejected += HandleStrikeAttemptRejected;
        reelingController.AttemptFailed +=
            HandleReelingAttemptFailed;
        reelingController.RetrievalCompleted +=
            HandleRetrievalCompleted;
        reelingController.AcceleratedDistanceMoved += HandleAcceleratedDistanceMoved;

        sessionTimer.Expired += HandleSessionExpired;

        if (inputSource != null)
        {
            inputSource.CastPerformed += HandleCastDetected;
        }
    }

    private void OnDisable()
    {
        if (catchInventory != null) CatchCompleted -= catchInventory.Record;
        hookController.Landed -= HandleHookLanded;

        fishBiteRaceController.FishHooked -= HandleFishHooked;
        fishBiteRaceController.TimedOut -= HandleBaitingTimedOut;
        strikeController.Succeeded -= HandleStrikeSucceeded;
        strikeController.TimedOut -= HandleStrikeTimedOut;
        strikeController.AttemptRejected -= HandleStrikeAttemptRejected;

        reelingController.AttemptFailed -=
            HandleReelingAttemptFailed;
        reelingController.RetrievalCompleted -=
            HandleRetrievalCompleted;
        reelingController.AcceleratedDistanceMoved -= HandleAcceleratedDistanceMoved;

        sessionTimer.Expired -= HandleSessionExpired;

        if (inputSource != null)
        {
            inputSource.CastPerformed -= HandleCastDetected;
        }
    }

    #endregion

    #region Casting Event Handlers

    // Interpret the hook's physical landing event as a gameplay transition.
    // The flight component does not need to know that Baiting exists.
    private void HandleHookLanded(Vector2 landingPosition)
    {
        if (CurrentState != FishingLoopState.Casting)
        {
            return;
        }

        // Resolve blocked landings before starting any baiting or bite race.
        Physics2D.SyncTransforms();
        if (ReelingObstacle.ContainsPoint(landingPosition))
        {
            LoseHookAndFinishAttempt("Hook landed on an obstacle.", AttemptFailureReason.ObstacleLanding);
            return;
        }

        castDistance = reelingController.DistanceToShore;
        // Lock the multiplier using the final landing distance.

        CurrentDistanceMultiplier = scoreTuningProfile.GetDistanceMultiplier(castDistance);

        // Preserve the landing point for Baiting and later retrieval logic.
        LandingPosition = landingPosition;

        // Notify presentation only after the final landing multiplier is available.
        DistanceMultiplierLocked?.Invoke();

        Debug.Log(
            $"Hook landed at: {LandingPosition.x:F2}, " +
            $"{LandingPosition.y:F2}");

        TransitionTo(FishingLoopState.Baiting);
    }


    private void HandleCastDetected(float power)
    {
        if (CurrentState != FishingLoopState.ReadyToCast || IsCastCoolingDown)
        {
            return;
        }

        SelectedLaneX = shoreLaneController.CurrentLaneX;
        SelectedCastPower = power;

        Debug.Log(
            $"Cast selected: lane X = {SelectedLaneX:F2}, " +
            $"power = {SelectedCastPower:F2}");

        TransitionTo(FishingLoopState.Casting);
    }

    #endregion

    #region Baiting Event Handlers

    private void HandleFishHooked(FishController fish)
    {
        if (CurrentState != FishingLoopState.Baiting)
        {
            return;
        }

        HookedFish = fish;
        if (fish.TryGetComponent<FishWaterVFX>(out var waterVFX))
        {
            waterVFX.Configure(reelingController);
        }

        Debug.Log(
            $"Fish hooked: {fish.name}, " +
            $"score value = {fish.ScoreValue}");

        TransitionTo(FishingLoopState.Striking);
    }

    private void HandleBaitingTimedOut()
    {
        if (CurrentState != FishingLoopState.Baiting)
        {
            return;
        }

        HookedFish = null;

        Debug.Log("Baiting timed out with no fish hooked.");

        // Continue retrieval with an empty hook.
        TransitionTo(FishingLoopState.Reeling);
    }

    #endregion

    #region Striking Event Handlers

    private void HandleStrikeSucceeded()
    {
        if (CurrentState != FishingLoopState.Striking)
        {
            return;
        }

        Debug.Log("Strike succeeded.");
        if (HookedFish != null &&
            HookedFish.TryGetComponent<FishAppearance>(out var appearance))
        {
            appearance.Reveal();
        }

        TransitionTo(FishingLoopState.Reeling);
    }

    private void HandleStrikeTimedOut()
    {
        if (CurrentState != FishingLoopState.Striking)
        {
            return;
        }

        LoseHookAndFinishAttempt("Strike timed out.", AttemptFailureReason.StrikeTimeout);
    }

    private void HandleStrikeAttemptRejected()
    {
        if (CurrentState != FishingLoopState.Striking)
        {
            return;
        }

        cameraController.PlayStrikeRejectedShake(
            strikeController.RejectedShakeAmplitude, strikeController.RejectedShakeDuration);
        StrikeRejected?.Invoke();
    }

    #endregion

    #region Reeling Events and Score Settlement

    // Only this coordinator can exchange the catch; this is not a second catch settlement.
    public bool TryReplaceHookedFish(FishController expectedPrey, FishController predator)
    {
        if (!CanProcessFishInteractions || fishReplacedThisAttempt ||
            expectedPrey == null || expectedPrey != HookedFish ||
            !expectedPrey.isActiveAndEnabled || expectedPrey.State != FishState.Hooked ||
            predator == null || predator == expectedPrey || !predator.CanNavigate ||
            !predator.IsFacingNavigationTarget ||
            !expectedPrey.TryGetComponent<FishAppearance>(out var preyAppearance) ||
            preyAppearance.Category != FishAppearanceCategory.Small ||
            !predator.TryGetComponent<FishAppearance>(out var predatorAppearance) ||
            fishEcologyProfile == null || !fishEcologyProfile.IsPredator(predatorAppearance.Category) ||
            !FishEcologyController.IsInBiteContact(predator, expectedPrey,
                fishEcologyProfile.MouthContactTolerance)) return false;

        // Commit identity before callbacks caused by disabling the consumed fish.
        fishReplacedThisAttempt = true;
        HookedFish = predator;
        predator.MarkHooked(fishBiteRaceController.transform);
        predatorAppearance.Reveal();
        if (predator.TryGetComponent<FishWaterVFX>(out var waterVFX))
            waterVFX.Configure(reelingController);
        reelingController.SetCatchTensionMultiplier(catchRewardProfile != null
            ? catchRewardProfile.GetAccelerationTensionMultiplier(predatorAppearance.Category) : 1f);
        expectedPrey.ResetToIdle();
        expectedPrey.gameObject.SetActive(false);

        // Preserve the cast multiplier, accrued tension and accelerated retrieval distance.
        Debug.Log($"Predation replaced {expectedPrey.name} with {predator.name}.", this);
        HookedFishReplaced?.Invoke();
        return true;
    }

    private void HandleReelingAttemptFailed()
    {
        if (CurrentState != FishingLoopState.Reeling)
        {
            return;
        }

        LoseHookAndFinishAttempt("Reeling attempt failed.", AttemptFailureReason.ReelingFailure);
    }

    private void HandleAcceleratedDistanceMoved(float distance)
    {
        if (CurrentState == FishingLoopState.Reeling && HookedFish != null)
        {
            AcceleratedCatchDistance += distance;
        }
    }

    private void HandleRetrievalCompleted()
    {
        if (CurrentState != FishingLoopState.Reeling || attemptSettled)
        {
            return;
        }

        attemptSettled = true;

        if (HookedFish != null)
        {
            float accelerationBonus = AcceleratedCatchDistance * scoreTuningProfile.AccelerationPointsPerUnit;
            // Round once after adding the bonus; the distance multiplier applies only to the fish.
            int caughtScore = Mathf.RoundToInt(
                HookedFish.ScoreValue * CurrentDistanceMultiplier + accelerationBonus);

            scoreTracker.AddScore(caughtScore);

            HookedFish.TryGetComponent<FishAppearance>(out var appearance);
            FishAppearanceCategory category = appearance != null ? appearance.Category : FishAppearanceCategory.Small;
            float addedSeconds = appearance != null && catchRewardProfile != null
                ? sessionTimer.AddTime(catchRewardProfile.GetTimeReward(category)) : 0f;
            var result = new CatchResult(attemptId, category,
                appearance != null ? appearance.RevealedSprite : null,
                HookedFish.ScoreValue, caughtScore, addedSeconds);

            Debug.Log(
                $"Fish caught: {HookedFish.name}, " +
                $"score added = {caughtScore}, " +
                $"acceleration bonus = {accelerationBonus:F2}, " +
                $"total score = {scoreTracker.Score}");

            HookedFish.gameObject.SetActive(false);
            HookedFish = null;
            CatchCompleted?.Invoke(result);
        }
        else
        {
            Debug.Log(
                $"Empty hook reached the shore. " +
                $"Total score = {scoreTracker.Score}");
        }

        TransitionTo(FishingLoopState.ReadyToCast);
    }

    #endregion

    #region Session End and Attempt Failure

    private void HandleSessionExpired()
    {
        if (CurrentState == FishingLoopState.GameOver)
        {
            return;
        }

        HookedFish?.ResetToIdle();
        HookedFish = null;

        Debug.Log("Session timer expired.");

        TransitionTo(FishingLoopState.GameOver);
    }

    private void LoseHookAndFinishAttempt(string reason, AttemptFailureReason failureReason)
    {
        if (attemptSettled || CurrentState == FishingLoopState.GameOver) return;
        attemptSettled = true;
        HookedFish?.ResetToIdle();
        HookedFish = null;

        hookTracker.LoseHook();

        Debug.LogWarning(
            $"{reason} Hooks remaining: " +
            $"{hookTracker.HooksRemaining}");

        FishingLoopState nextState =
            hookTracker.HasHooksRemaining
                ? FishingLoopState.ReadyToCast
                : FishingLoopState.GameOver;

        TransitionTo(nextState);
        AttemptFailed?.Invoke(new AttemptFailureResult(attemptId, failureReason));
    }

    #endregion

    #region State Updates

    private void UpdateGameOver()
    {
    }

    private void UpdateReeling()
    {
    }

    private void UpdateStriking()
    {
    }

    private void UpdateBaiting()
    {
    }

    private void UpdateCasting()
    {
        castDistance = reelingController.DistanceToShore;
        CurrentDistanceMultiplier =
            scoreTuningProfile.GetDistanceMultiplier(
                reelingController.DistanceToShore);
    }

    private void UpdateReadyToCast()
    {
        if (!IsCastCoolingDown)
        {
            return;
        }

        castCooldownRemaining = Mathf.Max(0f, castCooldownRemaining - Time.deltaTime);
        // Restore casting once when the cooldown ends.

        if (!IsCastCoolingDown)
        {
            SetCastingAvailable(true);
        }
    }

    #endregion

    #region State Entry and Exit Dispatch

    private void EnterState(FishingLoopState state)
    {
        Debug.Log($"Entered fishing state: {CurrentState}");
        switch (state)
        {
            case FishingLoopState.ReadyToCast:
                EnterReadyToCast();
                break;
            case FishingLoopState.Casting:
                EnterCasting();
                break;
            case FishingLoopState.Baiting:
                EnterBaiting();
                break;
            case FishingLoopState.Striking:
                EnterStriking();
                break;
            case FishingLoopState.Reeling:
                EnterReeling();
                break;
            case FishingLoopState.GameOver:
                EnterGameOver();
                break;
        }
    }

    private void ExitState(FishingLoopState state)
    {
        Debug.Log($"Exited fishing state: {CurrentState}");
        switch (state)
        {
            case FishingLoopState.ReadyToCast:
                ExitReadyToCast();
                break;
            case FishingLoopState.Casting:
                ExitCasting();
                break;
            case FishingLoopState.Baiting:
                ExitBaiting();
                break;
            case FishingLoopState.Striking:
                ExitStriking();
                break;
            case FishingLoopState.Reeling:
                ExitReeling();
                break;
            case FishingLoopState.GameOver:
                ExitGameOver();
                break;
        }
    }

    #endregion

    #region State Entry Actions

    private void EnterReadyToCast()
    {
        // Settlement has completed before entering the next attempt.
        CurrentDistanceMultiplier = 1f;
        shoreLaneController.enabled = true;
        cameraController.ShowOverview();

        inputSource?.SetMoveEnabled(true);
        // Return the hook to the player for the next attempt.
        hookController.Dock();
        SetCastingAvailable(!IsCastCoolingDown);
    }

    private void EnterCasting()
    {
        attemptId++;
        attemptSettled = false;
        fishReplacedThisAttempt = false;
        // Reset multiplier
        castDistance = 0f;
        CurrentDistanceMultiplier = 1f;
        // Follow the hook from launch through the remaining attempt states.
        cameraController.FollowHook();

        // Casting is event-driven. Landed will end this state.
        hookController.Launch(SelectedCastPower);
    }

    private void EnterBaiting()
    {
        // Start one bite race when the hook enters the water.
        fishBiteRaceController.BeginRace();
    }

    private void EnterStriking()
    {
        // Enabling the detector calls OnEnable(),
        // which resets it for this reaction opportunity.
        strikeGestureDetector.enabled = true;
        inputSource?.SetStrikeEnabled(true);

        strikeController.BeginCheck();
    }

    private void EnterReeling()
    {
        AcceleratedCatchDistance = 0f;
        inputSource?.SetMoveEnabled(true);
        inputSource?.SetAccelerateEnabled(true);

        float tensionMultiplier = 1f;
        if (catchRewardProfile != null && HookedFish != null &&
            HookedFish.TryGetComponent<FishAppearance>(out var appearance))
            tensionMultiplier = catchRewardProfile.GetAccelerationTensionMultiplier(appearance.Category);
        reelingController.SetCatchTensionMultiplier(tensionMultiplier);
        reelingController.BeginRetrieval();
    }

    private void EnterGameOver()
    {
        CurrentDistanceMultiplier = 1f;
        sessionTimer.StopTimer();
        hookController.CancelFlight();
        fishBiteRaceController.CancelRace();
        strikeController.CancelCheck();

        inputSource?.SetMoveEnabled(false);
        inputSource?.SetCastEnabled(false);
        inputSource?.SetStrikeEnabled(false);
        inputSource?.SetAccelerateEnabled(false);

        castGestureDetector.enabled = false;
        strikeGestureDetector.enabled = false;

        reelingController.CancelRetrieval();
        cameraController.ShowOverview();

        Debug.Log(
            $"Game Over. Final score: {scoreTracker.Score}");
        FinalScoreSaved = LocalLeaderboard.TryRecord(leaderboardSessionId, scoreTracker.Score);
    }

    #endregion

    #region State Exit Actions

    private void ExitReadyToCast()
    {
        inputSource?.SetMoveEnabled(false);
        SetCastingAvailable(false);
        shoreLaneController.enabled = false;
    }

    private void ExitCasting()
    {
    }

    private void ExitBaiting()
    {
    }

    private void ExitStriking()
    {
        strikeController.CancelCheck();

        strikeGestureDetector.enabled = false;
        inputSource?.SetStrikeEnabled(false);
    }

    private void ExitReeling()
    {
        // Successful retrieval settles before exit; every other exit discards the bonus.
        AcceleratedCatchDistance = 0f;
        reelingController.CancelRetrieval();

        inputSource?.SetMoveEnabled(false);
        inputSource?.SetAccelerateEnabled(false);
    }

    private void ExitGameOver()
    {
    }

    #endregion
}
