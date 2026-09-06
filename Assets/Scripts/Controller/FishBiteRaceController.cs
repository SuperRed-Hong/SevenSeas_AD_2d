using System;
using UnityEngine;
using System.Collections.Generic;
public sealed class FishBiteRaceController : MonoBehaviour
{
    [SerializeField, Min(0f)]
    [Tooltip("Fish inside this radius can detect and approach the bait.")]
    private float attractionRadius = 4f;

    [SerializeField]
    [Tooltip("Only colliders on these layers can participate in the bite race.")]
    private LayerMask fishLayer;

    private readonly List<FishController> candidates = new();

    [SerializeField, Min(0f)]
    [Tooltip("The distance at which a fish can claim the bait.")]
    private float hookRadius = 0.25f;

    [SerializeField, Min(0.1f)]
    [Tooltip("Maximum time allowed for a fish to reach the bait.")]
    private float baitingDuration = 8f;

    private float elapsedTime;
    private bool isRaceActive;
    private FishController hookedFish;

    public event Action<FishController> FishHooked;
    public event Action TimedOut;
    
    public void BeginRace()
    {
   

        elapsedTime = 0f;
        hookedFish = null;
        isRaceActive = true;
        candidates.Clear();
        
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, attractionRadius, fishLayer);

        foreach (Collider2D nearbyCollider in nearbyColliders)
        {
            FishController candidate = nearbyCollider.GetComponentInParent<FishController>();

            if (candidate == null || candidates.Contains(candidate))
            {
                continue;
            }
            candidates.Add(candidate);
        }
        
        
        //use a stable order so simultaneous claims have
        //a  deterministic result.
        candidates.Sort((left,right)=>left.GetInstanceID().CompareTo(right.GetInstanceID()));

        foreach (FishController candidate in candidates)
        {
            candidate.BeginApproach(transform);
        }
        
        Debug.Log(
            $"Bite race started with {candidates.Count} candidate fish.");
    }
    public void CancelRace()
    {
        if (!isRaceActive)
        {
            return;
        }

        isRaceActive = false;
        foreach (FishController candidate in candidates)
        {
            if (candidate != null)
            {
                candidate.ResetToIdle();
            }
        }

        candidates.Clear();
        hookedFish = null;
        // Cancellation is not a bite or timeout outcome.
    }

    private void Update()
    {
        if (!isRaceActive)
        {
            return;
        }

        elapsedTime += Time.deltaTime;

        // Check candidates in Inspector array order.
        // The first fish within range becomes the only winner.
        foreach (FishController candidate in candidates)
        {
            if (candidate == null ||
                candidate.State != FishState.Approaching)
            {
                continue;
            }

            float distanceToBait = Vector2.Distance(
                candidate.transform.position,
                transform.position);

            if (distanceToBait <= hookRadius)
            {
                CompleteWithWinner(candidate);
                return;
            }
        }

        if (elapsedTime >= baitingDuration)
        {
            CompleteWithTimeout();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attractionRadius <= 0f)
        {
            return;
        }

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, attractionRadius);
    }
    private void CompleteWithWinner(FishController winner)
    {
        isRaceActive = false;
        hookedFish = winner;

        foreach (FishController candidate in candidates)
        {
            if (candidate == null)
            {
                continue;
            }

            if (candidate == winner)
            {
                candidate.MarkHooked(transform);
            }
            else
            {
                candidate.ResetToIdle();
            }
        }

        FishHooked?.Invoke(winner);
    }

    private void CompleteWithTimeout()
    {
        isRaceActive = false;
        hookedFish = null;

        foreach (FishController candidate in candidates)
        {
            if (candidate != null)
            {
                candidate.ResetToIdle();
            }
        }

        TimedOut?.Invoke();
    }
}
