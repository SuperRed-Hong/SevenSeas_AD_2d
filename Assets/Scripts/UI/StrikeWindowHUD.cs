using UnityEngine;

public sealed class StrikeWindowHUD : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private StrikeController strikeController;

    [SerializeField]
    [Tooltip("World-space transform used as the centre of the strike rings.")]
    private Transform anchor;

    [SerializeField]
    private LineRenderer targetBandRenderer;

    [SerializeField]
    private LineRenderer movingRingRenderer;

    [Header("Appearance")]

    [SerializeField, Min(0.1f)]
    [Tooltip("World-space radius represented by normalized radius 1.")]
    private float maximumWorldRadius = 1.5f;

    [SerializeField, Range(16, 128)]
    [Tooltip("Number of line segments used to draw each circle.")]
    private int segmentCount = 64;

    [SerializeField, Min(0.01f)]
    [Tooltip("World-space width of the moving ring.")]
    private float movingRingWidth = 0.05f;

    [SerializeField]
    private Color targetBandColor = Color.green;

    [SerializeField]
    private Color movingRingColor = Color.white;

    [SerializeField]
    private Color rejectedColor = Color.red;

    

    
    private void DrawCircle(
        LineRenderer lineRenderer,
        float radius)
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.positionCount = segmentCount;

        for (int i = 0; i < segmentCount; i++)
        {
            // Divide one full rotation evenly between all line segments.
            float angle =
                i / (float)segmentCount *
                Mathf.PI *
                2f;

            // Convert the angle and radius into a point on the circle.
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;

            lineRenderer.SetPosition(
                i,
                new Vector3(x, y, 0f));
        }
    }
    private void Update()
    {
        bool shouldShow =
            strikeController != null &&
            strikeController.IsActive;

        if (targetBandRenderer != null)
        {
            targetBandRenderer.enabled = shouldShow;
        }

        if (movingRingRenderer != null)
        {
            movingRingRenderer.enabled = shouldShow;
        }

        if (!shouldShow)
        {

            return;
        }

        if (anchor != null)
        {
            transform.position = anchor.position;
        }

        float innerRadius =
            strikeController.TargetBandInnerRadius *
            maximumWorldRadius;

        float outerRadius =
            strikeController.TargetBandOuterRadius *
            maximumWorldRadius;

        // A thick LineRenderer is centred halfway between the
        // inner and outer edges of the target band.
        float targetBandRadius =
            (innerRadius + outerRadius) * 0.5f;

        float targetBandWidth =
            outerRadius - innerRadius;

        
        
        // Use the controller's real cooldown state so the
        // visual feedback cannot drift from input availability.
        bool isShowingRejectedFeedback =
            strikeController.IsCoolingDown;

        Color currentTargetBandColor =
            isShowingRejectedFeedback
                ? rejectedColor
                : targetBandColor;

        Color currentMovingRingColor =
            isShowingRejectedFeedback
                ? rejectedColor
                : movingRingColor;
        
        targetBandRenderer.startWidth = targetBandWidth;
        targetBandRenderer.endWidth = targetBandWidth;
        targetBandRenderer.startColor = currentTargetBandColor;
        targetBandRenderer.endColor = currentTargetBandColor;

        DrawCircle(
            targetBandRenderer,
            targetBandRadius);

        movingRingRenderer.startWidth = movingRingWidth;
        movingRingRenderer.endWidth = movingRingWidth;
        movingRingRenderer.startColor = currentMovingRingColor;
        movingRingRenderer.endColor = currentMovingRingColor;

        DrawCircle(
            movingRingRenderer,
            strikeController.RingRadius01 *
            maximumWorldRadius);
    } 
    
    
    
}