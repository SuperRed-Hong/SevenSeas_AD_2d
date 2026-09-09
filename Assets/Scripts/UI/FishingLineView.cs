using UnityEngine;

[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(LineRenderer))]
public sealed class FishingLineView : MonoBehaviour
{
    [SerializeField] private FishingLoopController loopController;
    [SerializeField] private Transform playerAnchor;
    [SerializeField] private Transform hookVisual;
    [SerializeField] private Transform hookRoot;
    [SerializeField] private Vector3 playerOffset;
    [SerializeField, Min(0.001f)] private float lineWidth = 0.035f;
    [SerializeField] private Color lineColor = new Color(1f, 0.94f, 0.75f, 0.9f);

    private LineRenderer line;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = false;
        line.positionCount = 2;
        line.widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
        line.enabled = false;
    }

    private void LateUpdate()
    {
        Transform endpoint = null;
        if (loopController != null && loopController.gameObject.activeInHierarchy)
        {
            switch (loopController.CurrentState)
            {
                case FishingLoopState.Casting:
                    endpoint = hookVisual;
                    break;
                case FishingLoopState.Baiting:
                case FishingLoopState.Striking:
                    // Landing does not detach the line while waiting for a bite or strike.
                    endpoint = hookRoot;
                    break;
                case FishingLoopState.Reeling:
                    FishController fish = loopController.HookedFish;
                    endpoint = fish != null && fish.gameObject.activeInHierarchy
                        ? fish.transform : hookRoot;
                    break;
            }
        }

        line.enabled = playerAnchor != null && endpoint != null && endpoint.gameObject.activeInHierarchy;
        if (!line.enabled) return;

        // Draw after hook and fish movement, including the hook's airborne offset.
        line.widthMultiplier = lineWidth;
        line.startColor = lineColor;
        line.endColor = lineColor;
        line.SetPosition(0, playerAnchor.TransformPoint(playerOffset));
        line.SetPosition(1, endpoint.position);
    }

    private void OnDisable()
    {
        if (line != null) line.enabled = false;
    }
}
