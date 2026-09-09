using UnityEngine;

public readonly struct CatchResult
{
    public int AttemptId { get; }
    public FishAppearanceCategory Category { get; }
    public Sprite RevealedSprite { get; }
    public int BaseScore { get; }
    public int ActualScore { get; }
    public float AddedSeconds { get; }

    public CatchResult(int attemptId, FishAppearanceCategory category, Sprite revealedSprite,
        int baseScore, int actualScore, float addedSeconds)
    {
        AttemptId = attemptId;
        Category = category;
        RevealedSprite = revealedSprite;
        BaseScore = baseScore;
        ActualScore = actualScore;
        AddedSeconds = addedSeconds;
    }
}

public enum AttemptFailureReason { ObstacleLanding, StrikeTimeout, ReelingFailure }

public readonly struct AttemptFailureResult
{
    public int AttemptId { get; }
    public AttemptFailureReason Reason { get; }

    public AttemptFailureResult(int attemptId, AttemptFailureReason reason)
    {
        AttemptId = attemptId;
        Reason = reason;
    }
}
