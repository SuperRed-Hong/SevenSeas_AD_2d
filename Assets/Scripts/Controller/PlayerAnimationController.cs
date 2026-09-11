using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{

    [SerializeField, Min(0f)] private float movementThreshold = 0.01f;
    [SerializeField] private FishingLoopController fishingLoopController;
    [SerializeField] private SpriteRenderer fishingPoleRenderer;
    
    
    private static readonly int CastID = Animator.StringToHash("Cast");
    private static readonly int MoveXId = Animator.StringToHash("MoveX");
    private static readonly int IsHoldingRodID = Animator.StringToHash("IsHoldingRod");
    
    private Animator animator;

    private float previousX;

    private void Awake()
    {
        animator  = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        previousX = transform.position.x;
        if (fishingLoopController != null)
        {
            fishingLoopController.StateChanged += HandleStateChanged;
            HandleStateChanged(fishingLoopController.CurrentState);
        }
    }

    private void HandleStateChanged(FishingLoopState state)
    {
        bool isHoldingRod = state == FishingLoopState.Striking ||
                            state == FishingLoopState.Casting ||
                            state == FishingLoopState.Baiting ||
                            state == FishingLoopState.Reeling;
        animator.SetBool(IsHoldingRodID, isHoldingRod);
        if (isHoldingRod && fishingPoleRenderer != null)
        {
            fishingPoleRenderer.enabled = false;
        }
        
        if (state == FishingLoopState.Casting)
        {
            animator.SetTrigger(CastID);
        }
    }

    private void LateUpdate()
    {
        float currentX = transform.position.x;
        float deltaX = currentX - previousX;
        
        previousX = currentX;
        
        float speedX = Time.deltaTime>0f? deltaX/ Time.deltaTime: 0f;
        
        float direction = Mathf.Abs(speedX) > movementThreshold? Mathf.Sign(speedX) : 0f;
        
        animator.SetFloat(MoveXId, direction);

        // Keep the separate pole hidden while the sprite already contains one.
        if (fishingPoleRenderer != null)
        {
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
            bool spriteContainsPole = current.IsName("Player_Cast") ||
                                      current.IsName("Player_HoldRod") ||
                                      (animator.IsInTransition(0) &&
                                       (next.IsName("Player_Cast") || next.IsName("Player_HoldRod")));
            fishingPoleRenderer.enabled =
                !animator.GetBool(IsHoldingRodID) && !spriteContainsPole;
        }
    }

    private void OnDisable()
    {
        if (animator != null)
        {
            animator.SetFloat(MoveXId, 0f);
        }
        
        if (fishingLoopController != null)
        {
            fishingLoopController.StateChanged -= HandleStateChanged;
        }
    }

}
