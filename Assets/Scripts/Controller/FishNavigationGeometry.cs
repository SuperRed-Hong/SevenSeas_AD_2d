using System.Collections.Generic;
using UnityEngine;

// Pure pose queries. Queries never move the fish or change its navigation state.
public sealed class FishNavigationGeometry
{
    private struct Body
    {
        public Vector3 localCenter;
        public Vector2 size;
        public float localAngle;
        public CapsuleDirection2D direction;
        public bool box;
    }

    private readonly List<Body> bodies = new();
    private readonly List<Collider2D> overlaps = new();
    private readonly List<RaycastHit2D> hits = new();
    private Transform root;
    public Collider2D LastBlocker { get; private set; }
    public float BoundingRadius { get; private set; }
    public bool HasShape => bodies.Count > 0;

    public void Capture(Transform fishRoot)
    {
        root = fishRoot;
        bodies.Clear();
        BoundingRadius = 0f;
        foreach (Collider2D collider in root.GetComponentsInChildren<Collider2D>())
        {
            if (!collider.enabled || !collider.gameObject.activeInHierarchy) continue;
            Body body = new Body();
            Vector3 scale = collider.transform.lossyScale;
            if (collider is CapsuleCollider2D capsule)
            {
                body.localCenter = root.InverseTransformPoint(capsule.transform.TransformPoint(capsule.offset));
                body.size = new Vector2(capsule.size.x * Mathf.Abs(scale.x), capsule.size.y * Mathf.Abs(scale.y));
                body.direction = capsule.direction;
                body.localAngle = Mathf.DeltaAngle(root.eulerAngles.z, capsule.transform.eulerAngles.z);
            }
            else if (collider is CircleCollider2D circle)
            {
                body.localCenter = root.InverseTransformPoint(circle.transform.TransformPoint(circle.offset));
                float diameter = circle.radius * 2f * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
                body.size = new Vector2(diameter, diameter);
                body.direction = CapsuleDirection2D.Horizontal;
            }
            else
            {
                // Legacy non-capsule bodies use their collider bounds, never the sprite rectangle.
                body.localCenter = root.InverseTransformPoint(collider.bounds.center);
                body.size = collider.bounds.size;
                body.localAngle = -root.eulerAngles.z;
                body.box = true;
            }
            bodies.Add(body);
            float centerDistance = ((Vector2)(root.TransformPoint(body.localCenter) - root.position)).magnitude;
            float bodyRadius = body.box ? body.size.magnitude * 0.5f : Mathf.Max(body.size.x, body.size.y) * 0.5f;
            BoundingRadius = Mathf.Max(BoundingRadius, centerDistance + bodyRadius);
        }
    }

    public bool IsPoseAllowed(Vector3 position, Quaternion rotation, BoxCollider2D area, float padding, bool avoidFish = false)
    {
        bool previous = Physics2D.queriesHitTriggers;
        try
        {
            Physics2D.queriesHitTriggers = true;
            return PoseAllowed(position, rotation, area, padding, avoidFish);
        }
        finally { Physics2D.queriesHitTriggers = previous; }
    }

    public bool IsTranslationAllowed(Vector3 start, Vector3 end, Quaternion rotation, BoxCollider2D area, float padding)
    {
        bool previous = Physics2D.queriesHitTriggers;
        try
        {
            Physics2D.queriesHitTriggers = true;
            return TranslationAllowed(start, end, rotation, area, padding);
        }
        finally { Physics2D.queriesHitTriggers = previous; }
    }

    public bool IsRotationAllowed(Vector3 position, Quaternion from, Quaternion to, BoxCollider2D area,
        float padding, float maxStepDegrees)
    {
        bool previous = Physics2D.queriesHitTriggers;
        try
        {
            Physics2D.queriesHitTriggers = true;
            return RotationAllowed(position, from, to, area, padding, maxStepDegrees);
        }
        finally { Physics2D.queriesHitTriggers = previous; }
    }

    private bool PoseAllowed(Vector3 position, Quaternion rotation, BoxCollider2D area, float padding, bool avoidFish = false)
    {
        LastBlocker = null;
        if (root == null || !HasShape) return false;
        Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, root.lossyScale);
        foreach (Body body in bodies)
        {
            Vector2 center = matrix.MultiplyPoint3x4(body.localCenter);
            float angle = rotation.eulerAngles.z + body.localAngle;
            Vector2 size = body.size + Vector2.one * (Mathf.Max(0f, padding) * 2f);
            if (!FitsArea(center, size, angle, body, area)) return false;
            overlaps.Clear();
            if (body.box) Physics2D.OverlapBox(center, size, angle, Filter(), overlaps);
            else Physics2D.OverlapCapsule(center, size, body.direction, angle, Filter(), overlaps);
            foreach (Collider2D collider in overlaps)
            {
                bool otherFish = avoidFish && collider != null && collider.enabled && collider.gameObject.activeInHierarchy &&
                    collider.transform != root && !collider.transform.IsChildOf(root) &&
                    collider.GetComponentInParent<FishController>() != null;
                if (!IsObstacle(collider) && !otherFish) continue;
                LastBlocker = collider;
                return false;
            }
        }
        return true;
    }

    private bool TranslationAllowed(Vector3 start, Vector3 end, Quaternion rotation, BoxCollider2D area, float padding)
    {
        if (!PoseAllowed(start, rotation, area, padding) || !PoseAllowed(end, rotation, area, padding)) return false;
        Vector2 delta = end - start;
        float distance = delta.magnitude;
        if (distance <= 0.000001f) return true;
        Matrix4x4 matrix = Matrix4x4.TRS(start, rotation, root.lossyScale);
        foreach (Body body in bodies)
        {
            Vector2 center = matrix.MultiplyPoint3x4(body.localCenter);
            float angle = rotation.eulerAngles.z + body.localAngle;
            Vector2 size = body.size + Vector2.one * (Mathf.Max(0f, padding) * 2f);
            hits.Clear();
            if (body.box) Physics2D.BoxCast(center, size, angle, delta / distance, Filter(), hits, distance);
            else Physics2D.CapsuleCast(center, size, body.direction, angle, delta / distance, Filter(), hits, distance);
            foreach (RaycastHit2D hit in hits)
            {
                if (!IsObstacle(hit.collider)) continue;
                LastBlocker = hit.collider;
                return false;
            }
        }
        return true;
    }

    private bool RotationAllowed(Vector3 position, Quaternion from, Quaternion to, BoxCollider2D area,
        float padding, float maxStepDegrees)
    {
        if (!PoseAllowed(position, from, area, padding)) return false;
        float delta = Mathf.DeltaAngle(from.eulerAngles.z, to.eulerAngles.z);
        int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(delta) / Mathf.Clamp(maxStepDegrees, 0.5f, 10f)));
        float step = delta / steps;
        // Every intermediate body lies within this dilation of the midpoint pose.
        float arcPadding = 2f * BoundingRadius * Mathf.Sin(Mathf.Abs(step) * Mathf.Deg2Rad * 0.25f);
        for (int i = 0; i < steps; i++)
        {
            Quaternion midpoint = Quaternion.Euler(0f, 0f, from.eulerAngles.z + (i + 0.5f) * step);
            if (!PoseAllowed(position, midpoint, area, padding + arcPadding)) return false;
        }
        return PoseAllowed(position, to, area, padding);
    }

    private static bool FitsArea(Vector2 center, Vector2 size, float angle, Body body, BoxCollider2D area)
    {
        if (area == null) return true;
        Vector2 half = area.size * 0.5f;
        if (body.box)
        {
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            {
                Vector2 corner = center + (Vector2)(rotation * new Vector3(x * size.x * 0.5f, y * size.y * 0.5f));
                Vector2 local = (Vector2)area.transform.InverseTransformPoint(corner) - area.offset;
                if (Mathf.Abs(local.x) > half.x || Mathf.Abs(local.y) > half.y) return false;
            }
            return true;
        }
        bool horizontal = body.direction == CapsuleDirection2D.Horizontal;
        float radius = (horizontal ? size.y : size.x) * 0.5f;
        float segment = Mathf.Max(0f, (horizontal ? size.x : size.y) * 0.5f - radius);
        float radians = (angle + (horizontal ? 0f : 90f)) * Mathf.Deg2Rad;
        Vector2 axis = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        Matrix4x4 inverse = area.transform.worldToLocalMatrix;
        float radiusX = radius * Mathf.Sqrt(inverse.m00 * inverse.m00 + inverse.m01 * inverse.m01);
        float radiusY = radius * Mathf.Sqrt(inverse.m10 * inverse.m10 + inverse.m11 * inverse.m11);
        for (int sign = -1; sign <= 1; sign += 2)
        {
            Vector2 local = (Vector2)area.transform.InverseTransformPoint(center + axis * (segment * sign)) - area.offset;
            if (Mathf.Abs(local.x) + radiusX > half.x || Mathf.Abs(local.y) + radiusY > half.y) return false;
        }
        return true;
    }

    private bool IsObstacle(Collider2D collider)
    {
        if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy ||
            collider.transform == root || collider.transform.IsChildOf(root)) return false;
        return ReelingObstacle.IsBlockingCollider(collider);
    }

    private static ContactFilter2D Filter() => new ContactFilter2D { useTriggers = true, useLayerMask = false };
}
