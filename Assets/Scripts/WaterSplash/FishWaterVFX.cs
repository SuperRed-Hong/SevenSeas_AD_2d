using UnityEngine;

// Run after FishController has updated its following position.
[DefaultExecutionOrder(100)]
public class FishWaterVFX : MonoBehaviour
{
    [SerializeField] private GameObject swimRipple;
    [SerializeField] private GameObject hookedSplash;

    [SerializeField]
    [Tooltip("Random quiet time in seconds between swim ripple cycles.")]
    private Vector2 swimRippleInterval = new Vector2(1.2f, 2.2f);

    private FishController fishController;
    private ReelingController reelingController;
    private Vector3 previousPosition;
    private Animator swimRippleAnimator;
    private float rippleWaitRemaining;
    private bool isRipplePlaying;

    private void Awake()
    {
        fishController = GetComponent<FishController>();
        if (swimRipple != null)
        {
            swimRippleAnimator = swimRipple.GetComponent<Animator>();
        }
    }

    private void OnEnable()
    {
        previousPosition = transform.position;
        SetRippleVisible(false);
        SetSplashVisible(false);
        isRipplePlaying = false;
        // Spread newly spawned fish across the first ripple cycle.
        rippleWaitRemaining = Random.Range(0f, swimRippleInterval.y);
    }

    private void OnValidate()
    {
        swimRippleInterval.x = Mathf.Max(0.05f, swimRippleInterval.x);
        swimRippleInterval.y = Mathf.Max(swimRippleInterval.x, swimRippleInterval.y);
    }

    public void Configure(ReelingController controller)
    {
        reelingController = controller;
        previousPosition = transform.position;
    }

    private void LateUpdate()
    {
        float distanceMoved = Vector2.Distance(
            previousPosition, transform.position);

        previousPosition = transform.position;

        bool isDragging =
            fishController != null &&
            fishController.State == FishState.Hooked &&
            reelingController != null &&
            reelingController.IsActive;

        // Ignore tiny position changes and hide while gameplay is paused.
        bool isMoving =
            Time.deltaTime > 0f &&
            distanceMoved > 0.01f * Time.deltaTime;

        // Keep ambient ripples independent of the bite race and other fish.
        bool showSwimRipple =
            fishController != null &&
            fishController.State != FishState.Hooked;

        UpdateSwimRipple(showSwimRipple);
        SetSplashVisible(isDragging && isMoving);
    }

    private void UpdateSwimRipple(bool canPlay)
    {
        if (!canPlay)
        {
            if (isRipplePlaying)
            {
                isRipplePlaying = false;
                rippleWaitRemaining = Random.Range(swimRippleInterval.x, swimRippleInterval.y);
            }
            SetRippleVisible(false);
            return;
        }

        if (Time.deltaTime <= 0f || swimRippleAnimator == null ||
            swimRippleAnimator.runtimeAnimatorController == null)
        {
            return;
        }

        if (isRipplePlaying)
        {
            // Finish one cycle even when the shared animation clip is set to loop.
            if (swimRippleAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
            {
                SetRippleVisible(false);
                isRipplePlaying = false;
                rippleWaitRemaining = Random.Range(swimRippleInterval.x, swimRippleInterval.y);
            }
            return;
        }

        rippleWaitRemaining -= Time.deltaTime;
        if (rippleWaitRemaining > 0f)
        {
            return;
        }

        SetRippleVisible(true);
        swimRippleAnimator.Play(0, 0, 0f);
        swimRippleAnimator.Update(0f);
        isRipplePlaying = true;
    }

    private void OnDisable()
    {
        SetRippleVisible(false);
        SetSplashVisible(false);
    }

    private void SetRippleVisible(bool visible)
    {
        if (swimRipple != null && swimRipple.activeSelf != visible)
        {
            swimRipple.SetActive(visible);
        }
    }

    private void SetSplashVisible(bool visible)
    {
        if (hookedSplash != null && hookedSplash.activeSelf != visible)
        {
            hookedSplash.SetActive(visible);
        }
    }
}
