
using UnityEngine;



public enum FishingLoopState
{
    ReadyToCast,
    Casting,
    Baiting,
    Striking,
    Reeling,
    GameOver
}
public sealed class FishingLoopController : MonoBehaviour
{
    public FishingLoopState CurrentState { get; private set; } =  FishingLoopState.ReadyToCast;



    
    private void Start()
    {
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
    private void EnterReadyToCast() { }
    private void EnterCasting() { }
    private void EnterBaiting() { }
    private void EnterStriking() { }
    private void EnterReeling() { }
    private void EnterGameOver() { }
    private void ExitReadyToCast() { }
    private void ExitCasting() { }
    private void ExitBaiting() { }
    private void ExitStriking() { }
    private void ExitReeling() { }
    private void ExitGameOver() { }
}
        
