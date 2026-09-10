using UnityEngine;
using System.Collections.Generic;

public sealed class ReelingObstacle : MonoBehaviour
{
    public static bool IsBlockingCollider(Collider2D collider)
    {
        if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy) return false;
        ReelingObstacle marker = collider.GetComponentInParent<ReelingObstacle>();
        return (marker != null && marker.isActiveAndEnabled) ||
            collider is UnityEngine.Tilemaps.TilemapCollider2D ||
            (collider is CompositeCollider2D && collider.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() != null);
    }

    private static readonly List<Collider2D> queryResults = new();

    public static bool ContainsPoint(Vector2 position)
    {
        using var scope = new TriggerQueryScope(true);
        Physics2D.OverlapPoint(position, QueryFilter(), queryResults);
        return HasMarkedObstacle();
    }

    public static bool OverlapsBounds(Bounds bounds)
    {
        using var scope = new TriggerQueryScope(true);
        Physics2D.OverlapBox(bounds.center, bounds.size, 0f, QueryFilter(), queryResults);
        return HasMarkedObstacle();
    }

    private static readonly List<RaycastHit2D> segmentResults = new();

    public static bool BlocksCircleSweep(Vector2 start, Vector2 end, float radius)
    {
        using var scope = new TriggerQueryScope(true);
        Physics2D.OverlapCircle(start, radius, QueryFilter(), queryResults);
        if (HasMarkedObstacle()) return true;
        Physics2D.OverlapCircle(end, radius, QueryFilter(), queryResults);
        if (HasMarkedObstacle()) return true;
        Vector2 delta = end - start;
        float distance = delta.magnitude;
        if (distance <= 0.000001f) return false;
        Physics2D.CircleCast(start, radius, delta / distance, QueryFilter(), segmentResults, distance);
        foreach (RaycastHit2D hit in segmentResults)
        {
            if (IsBlockingCollider(hit.collider)) return true;
        }
        return false;
    }

    public static bool BlocksSegment(Vector2 start, Vector2 end)
    {
        using var scope = new TriggerQueryScope(true);
        // Explicit endpoint checks also work when queriesStartInColliders is disabled.
        if (ContainsPoint(start) || ContainsPoint(end)) return true;
        Physics2D.Linecast(start, end, QueryFilter(), segmentResults);
        foreach (RaycastHit2D hit in segmentResults)
        {
            if (IsBlockingCollider(hit.collider)) return true;
        }
        return false;
    }

    private static ContactFilter2D QueryFilter()
    {
        // Include trigger obstacles regardless of the global query setting.
        return new ContactFilter2D { useTriggers = true, useLayerMask = false };
    }

    // Unity 6000.3 queries can still exclude triggers despite ContactFilter2D.useTriggers.
    // Synchronous queries restore the project setting before returning, including exceptions.
    private readonly struct TriggerQueryScope : System.IDisposable
    {
        private readonly bool previous;
        public TriggerQueryScope(bool includeTriggers)
        {
            previous = Physics2D.queriesHitTriggers;
            Physics2D.queriesHitTriggers = includeTriggers;
        }
        public void Dispose() => Physics2D.queriesHitTriggers = previous;
    }

    private static bool HasMarkedObstacle()
    {
        foreach (Collider2D candidate in queryResults)
        {
            if (IsBlockingCollider(candidate))
            {
                return true;
            }
        }

        return false;
    }
}
