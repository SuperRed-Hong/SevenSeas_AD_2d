using UnityEngine;
using System.Collections.Generic;

public sealed class ReelingObstacle : MonoBehaviour
{
    private static readonly List<Collider2D> queryResults = new();

    public static bool ContainsPoint(Vector2 position)
    {
        Physics2D.OverlapPoint(position, QueryFilter(), queryResults);
        return HasMarkedObstacle();
    }

    public static bool OverlapsBounds(Bounds bounds)
    {
        Physics2D.OverlapBox(bounds.center, bounds.size, 0f, QueryFilter(), queryResults);
        return HasMarkedObstacle();
    }

    private static ContactFilter2D QueryFilter()
    {
        // Include trigger obstacles regardless of the global query setting.
        return new ContactFilter2D { useTriggers = true, useLayerMask = false };
    }

    private static bool HasMarkedObstacle()
    {
        foreach (Collider2D candidate in queryResults)
        {
            ReelingObstacle obstacle = candidate.GetComponentInParent<ReelingObstacle>();
            if (obstacle != null && obstacle.isActiveAndEnabled)
            {
                return true;
            }
        }

        return false;
    }
}
