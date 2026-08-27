using UnityEngine;

public sealed class CastCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float horizontalLookAhead = 1.5f;
    [SerializeField, Min(0.01f)] private float smoothTime = 0.2f;

    private Vector3 startPosition;
    private float horizontalVelocity;

    private void Awake()
    {
        startPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        float desiredX = Mathf.Max(
            startPosition.x,
            target.position.x + horizontalLookAhead);
        float x = Mathf.SmoothDamp(
            transform.position.x,
            desiredX,
            ref horizontalVelocity,
            smoothTime);
        transform.position = new Vector3(x, startPosition.y, startPosition.z);
    }

    public void ResetCamera()
    {
        horizontalVelocity = 0f;
        transform.position = startPosition;
    }
}
