using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(50)]
public sealed class FishEcologyController : MonoBehaviour
{
    [SerializeField] private FishingLoopController loop;
    [SerializeField] private FishEcologyProfile ecologyProfile;

    private FishController[] fish = Array.Empty<FishController>();
    private readonly HashSet<FishController> fleeing = new();
    private readonly HashSet<FishController> chasing = new();
    private readonly HashSet<FishController> rolledPredators = new();
    private bool reeling;
    private bool predationCompleted;
    private bool hasAttempt;
    private int attemptId;

    private void OnEnable()
    {
        if (loop == null) return;
        loop.StateChanged += HandleStateChanged;
        HandleStateChanged(loop.CurrentState);
    }

    private void OnDisable()
    {
        if (loop != null) loop.StateChanged -= HandleStateChanged;
        EndReeling();
    }

    private void HandleStateChanged(FishingLoopState state)
    {
        if (state != FishingLoopState.Reeling)
        {
            EndReeling();
            return;
        }
        if (reeling) return;
        reeling = true;
        if (!hasAttempt || attemptId != loop.CurrentAttemptId)
        {
            attemptId = loop.CurrentAttemptId;
            hasAttempt = true;
            predationCompleted = false;
            rolledPredators.Clear();
        }
        // This prototype spawns its population before a retrieval starts.
        fish = FindObjectsByType<FishController>(FindObjectsSortMode.InstanceID);
    }

    private void EndReeling()
    {
        StopNavigation(fleeing);
        StopNavigation(chasing);
        reeling = false;
        fish = Array.Empty<FishController>();
    }

    private static void StopNavigation(HashSet<FishController> owned)
    {
        foreach (FishController item in owned)
            if (item != null) item.EndExternalNavigation();
        owned.Clear();
    }

    private void LateUpdate()
    {
        if (!reeling || loop == null || ecologyProfile == null ||
            !loop.CanProcessFishInteractions || Time.deltaTime <= 0f) return;

        FishController prey = loop.HookedFish;
        bool canPredate = !predationCompleted && prey != null &&
            prey.isActiveAndEnabled && prey.State == FishState.Hooked &&
            HasCategory(prey, FishAppearanceCategory.Small);
        if (!canPredate) StopNavigation(chasing);

        foreach (FishController item in fish)
        {
            if (item == null) continue;
            if (!item.CanNavigate || item == prey || item.State == FishState.Hooked)
            {
                EndOwnedNavigation(item);
                continue;
            }
            if (HasCategory(item, FishAppearanceCategory.Small)) UpdateFlee(item);
            if (canPredate && !predationCompleted && IsPredator(item)) UpdatePredation(item, prey);
        }
    }

    private void EndOwnedNavigation(FishController item)
    {
        bool owned = fleeing.Remove(item);
        owned |= chasing.Remove(item);
        if (owned) item.EndExternalNavigation();
    }

    private void UpdateFlee(FishController item)
    {
        Vector3 away = item.transform.position - loop.HookPosition;
        away.z = 0f;
        float distance = away.magnitude;
        bool alreadyFleeing = fleeing.Contains(item);
        if (alreadyFleeing && distance >= ecologyProfile.FleeExitRadius)
        {
            fleeing.Remove(item);
            item.EndExternalNavigation();
            return;
        }
        if (!alreadyFleeing && distance > ecologyProfile.FleeEnterRadius) return;
        if (!alreadyFleeing && item.IsExternallyNavigating) return;

        // A coincident center still needs a deterministic escape direction.
        Vector3 direction = distance > 0.0001f ? away / distance : Vector3.right;
        Vector3 target = loop.HookPosition + direction * (ecologyProfile.FleeExitRadius + 0.25f);
        target.z = item.transform.position.z;
        if (item.BeginExternalNavigation(target, 0f, ecologyProfile.FleeSpeedMultiplier))
            fleeing.Add(item);
        else if (alreadyFleeing)
            EndOwnedNavigation(item);
    }

    private bool IsPredator(FishController item)
    {
        return item.TryGetComponent<FishAppearance>(out var appearance) &&
            ecologyProfile.IsPredator(appearance.Category);
    }

    private static bool HasCategory(FishController item, FishAppearanceCategory category)
    {
        return item.TryGetComponent<FishAppearance>(out var appearance) && appearance.Category == category;
    }

    private void UpdatePredation(FishController predator, FishController prey)
    {
        if (!chasing.Contains(predator))
        {
            if (predator.IsExternallyNavigating || rolledPredators.Contains(predator) ||
                Vector2.Distance(predator.transform.position, prey.transform.position) >
                    ecologyProfile.PredatorDetectionRadius) return;
            // Record both successful and failed rolls for the entire retrieval.
            rolledPredators.Add(predator);
            if (UnityEngine.Random.value >= ecologyProfile.PredationProbability) return;
            if (!predator.BeginExternalNavigation(prey.transform, 0f, ecologyProfile.PredatorSpeedMultiplier)) return;
            chasing.Add(predator);
        }
        else if (!predator.IsExternallyNavigating)
        {
            // Navigation may terminate if its target disappears or can no longer be pursued.
            chasing.Remove(predator);
            return;
        }

        if (!predator.IsFacingNavigationTarget ||
            !IsInBiteContact(predator, prey, ecologyProfile.MouthContactTolerance)) return;
        if (loop.TryReplaceHookedFish(prey, predator))
        {
            predationCompleted = true;
            StopNavigation(chasing);
        }
        else
        {
            chasing.Remove(predator);
            predator.EndExternalNavigation();
        }
    }

    public static bool IsInBiteContact(FishController predator, FishController prey, float tolerance)
    {
        if (predator == null || prey == null || !predator.isActiveAndEnabled || !prey.isActiveAndEnabled)
            return false;
        // The hooked fish follows in LateUpdate, after the most recent physics step.
        Physics2D.SyncTransforms();
        Vector2 mouth = predator.BitePosition;
        foreach (Collider2D body in prey.GetComponentsInChildren<Collider2D>())
        {
            if (!body.enabled || !body.gameObject.activeInHierarchy) continue;
            Vector2 contact = body.ClosestPoint(mouth);
            if (Vector2.Distance(mouth, contact) <= Mathf.Max(0f, tolerance) &&
                !ReelingObstacle.BlocksSegment(mouth, contact)) return true;
        }
        return false;
    }
}
