using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class FishMovementArea : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        var area = GetComponent<BoxCollider2D>();
        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0f, 0.9f, 1f, 0.8f);
        Gizmos.DrawWireCube(area.offset, area.size);
        Gizmos.matrix = previous;
    }
}
