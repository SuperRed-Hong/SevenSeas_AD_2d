
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
    private FishingInputSource inputSource;
    
    [FormerlySerializedAs("hookFlightController")] [SerializeField]
    private FishingHookController hookController;

    public Vector2 LandingPosition { get; private set; }
    public float SelectedLaneX { get; private set; }
    public float SelectedCastPower { get; private set; }
    public FishController HookedFish { get; private set; }
    
    public FishingLoopState CurrentState { get; private set; } =  FishingLoopState.ReadyToCast;
    private CastGestureDetector castGestureDetector;


    private void Awake()
    {
        castGestureDetector = GetComponent<CastGestureDetector>();
    }
    private void Start()
    {
        // Mobile entry provides AppRoot and the gyroscope reader.
        // Direct Editor testing can continue without motion services.
        if (AppRoot.Instance != null)
        {
            castGestureDetector.ConfigureReader(
                AppRoot.Instance.GyroscopeReader);
        }
        
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
        inputSource?.SetMoveEnabled(true);
        // Return the hook to the player for the next attempt.
        hookController.Dock();
        castGestureDetector.enabled = true;
        
        inputSource?.SetCastEnabled(true);
    }
    private void EnterCasting() 
    { 
        // Casting is event-driven. Landed will end this state.
        hookController.Launch(SelectedCastPower);
    }

    private void EnterBaiting()
    {
        // Start one bite race when the hook enters the water.
        fishBiteRaceController.BeginRace();
    }
    private void EnterStriking() { }
    private void EnterReeling() { }
    private void EnterGameOver() { }

    private void ExitReadyToCast()
    {
        inputSource?.SetMoveEnabled(false);
        castGestureDetector.enabled = false; 
        inputSource?.SetCastEnabled(false);
    }
    private void ExitCasting() { }
    private void ExitBaiting() { }
    private void ExitStriking() { }
    private void ExitReeling() { }
    private void ExitGameOver() { }
}
        
