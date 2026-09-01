
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
    [SerializeField] private ShoreLaneController shoreLaneController;
    [SerializeField]
    private FishBiteRaceController fishBiteRaceController;
    [SerializeField]
    private ReelingController reelingController;
    [SerializeField]
    private FishingInputSource inputSource;
    
    [FormerlySerializedAs("hookFlightController")] [SerializeField]
    private FishingHookController hookController;
    [SerializeField]
    private StrikeController strikeController;
    
    [SerializeField]
    private FishingCameraController cameraController;
    
    
    [SerializeField]
    private ScoreTracker scoreTracker;
    [SerializeField]
    private HookTracker hookTracker;
    [SerializeField]
    private SessionTimer sessionTimer;
    
    public Vector2 LandingPosition { get; private set; }
    public float SelectedLaneX { get; private set; }
    public float SelectedCastPower { get; private set; }
    public FishController HookedFish { get; private set; }
    
    public FishingLoopState CurrentState { get; private set; } =  FishingLoopState.ReadyToCast;
    [SerializeField]
    private CastGestureDetector castGestureDetector;
    [SerializeField]
    private CastGestureDetector strikeGestureDetector;



    private void Start()
    {
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
        EnterState(CurrentState);
      

    }

    private void TransitionTo(FishingLoopState nextState)
    {
        if (CurrentState == nextState)
        {
            return;
        }

        ExitState(CurrentState);
        CurrentState = nextState;
        EnterState(CurrentState);
    }


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
    private void OnEnable()
    {

        castGestureDetector.CastDetected += HandleCastDetected;
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
        
        sessionTimer.Expired += HandleSessionExpired;
        
        if (inputSource != null)
        {
            inputSource.CastPerformed += HandleCastDetected;

        }
        

    }

    private void OnDisable()
    {
        castGestureDetector.CastDetected -= HandleCastDetected;
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
        
        sessionTimer.Expired -= HandleSessionExpired;
        
        if (inputSource != null)
        {
            inputSource.CastPerformed -= HandleCastDetected;

        }
        
    }
    // Interpret the hook's physical landing event as a gameplay transition.
    // The flight component does not need to know that Baiting exists.
    private void HandleHookLanded(Vector2 landingPosition)
    {
        if (CurrentState != FishingLoopState.Casting)
        {
            return;
        }

        // Preserve the landing point for Baiting and later retrieval logic.
        LandingPosition = landingPosition;

        Debug.Log(
            $"Hook landed at: {LandingPosition.x:F2}, " +
            $"{LandingPosition.y:F2}");

        TransitionTo(FishingLoopState.Baiting);
    }
    
    

    
    
    private void HandleCastDetected(float power)
    {
        if (CurrentState != FishingLoopState.ReadyToCast)
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
   
    
    private void HandleFishHooked(FishController fish)
    {
        if (CurrentState != FishingLoopState.Baiting)
        {
            return;
        }

        HookedFish = fish;

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
    private void HandleStrikeSucceeded()
    {
        if (CurrentState != FishingLoopState.Striking)
        {
            return;
        }

        Debug.Log("Strike succeeded.");
        TransitionTo(FishingLoopState.Reeling);
    }

    private void HandleStrikeTimedOut()
    {
        if (CurrentState != FishingLoopState.Striking)
        {
            return;
        }

        LoseHookAndFinishAttempt("Strike timed out.");
    }
    
    private void HandleStrikeAttemptRejected()
    {
        if (CurrentState != FishingLoopState.Striking)
        {
            return;
        }

        cameraController.PlayStrikeRejectedShake();
    }
    
    private void HandleReelingAttemptFailed()
    {
        if (CurrentState != FishingLoopState.Reeling)
        {
            return;
        }

        LoseHookAndFinishAttempt("Reeling attempt failed.");
    }
    private void HandleRetrievalCompleted()
    {
        if (CurrentState != FishingLoopState.Reeling)
        {
            return;
        }

        if (HookedFish != null)
        {
            int caughtScore = HookedFish.ScoreValue;

            scoreTracker.AddScore(caughtScore);

            Debug.Log(
                $"Fish caught: {HookedFish.name}, " +
                $"score added = {caughtScore}, " +
                $"total score = {scoreTracker.Score}");

            HookedFish.gameObject.SetActive(false);
            HookedFish = null;
        }
        else
        {
            Debug.Log(
                $"Empty hook reached the shore. " +
                $"Total score = {scoreTracker.Score}");
        }

        TransitionTo(FishingLoopState.ReadyToCast);
    }
    
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
    
    private void LoseHookAndFinishAttempt(string reason)
    {
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
    }
    
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
    }

    private void UpdateReadyToCast()
    {
    }

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

    private void EnterReadyToCast()
    {
        
        shoreLaneController.enabled = true;
        cameraController.ShowOverview();
        
        inputSource?.SetMoveEnabled(true);
        // Return the hook to the player for the next attempt.
        hookController.Dock();
        castGestureDetector.enabled = true;
        
        inputSource?.SetCastEnabled(true);
    }
    private void EnterCasting() 
    { 
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
        inputSource?.SetMoveEnabled(true);
        inputSource?.SetAccelerateEnabled(true);

        reelingController.BeginRetrieval();
    }
    private void EnterGameOver()
    {
        sessionTimer.StopTimer();

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
    }

    private void ExitReadyToCast()
    {
        inputSource?.SetMoveEnabled(false);
        castGestureDetector.enabled = false; 
        inputSource?.SetCastEnabled(false);
        shoreLaneController.enabled = false;
    }
    private void ExitCasting() { }
    private void ExitBaiting() { }
    private void ExitStriking()
    {
        strikeController.CancelCheck();

        strikeGestureDetector.enabled = false;
        inputSource?.SetStrikeEnabled(false);
    }
    private void ExitReeling()
    {
        reelingController.CancelRetrieval();

        inputSource?.SetMoveEnabled(false);
        inputSource?.SetAccelerateEnabled(false);
    }
    private void ExitGameOver() { }
}
        
