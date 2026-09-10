using UnityEngine;

[CreateAssetMenu(fileName = "FishEcologyProfile", menuName = "Seven Seas/Fish Ecology Profile")]
public sealed class FishEcologyProfile : ScriptableObject
{
    [Header("逃离（收线期间，未挂钩的 Small 鱼）")]
    [SerializeField, Min(0f), Tooltip("鱼中心距离鱼钩不超过此值时开始逃离。单位：Unity 世界单位。仅在 Reeling 收线期间生效。")]
    private float fleeEnterRadius = 3f;
    [SerializeField, Min(0f), Tooltip("已逃离的鱼距离鱼钩达到此值后恢复 Idle。必须大于进入半径，避免在边缘反复切换；自动保证至少大 0.1。单位：世界单位。")]
    private float fleeExitRadius = 4f;
    [SerializeField, Min(0f), Tooltip("逃离速度相对于 FishController.swimSpeed（基础追饵速度）的倍数。1.5 表示 1.5 倍，不是相对于 Idle 慢游速度。")]
    private float fleeSpeedMultiplier = 1.5f;
    [Header("捕食（收线期间，目标为挂钩的 Small 鱼）")]
    [SerializeField, Min(0f), Tooltip("捕食鱼与挂钩小鱼的中心距离不超过此值时，进行本竿唯一一次概率判定。单位：世界单位；不是鱼钩的吸引半径。")]
    private float predatorDetectionRadius = 5f;
    [SerializeField, Range(0f, 1f), Tooltip("每条符合条件的捕食鱼每竿尝试追捕的概率。0.3 = 30%；失败不重复抽取，成功仍需追上并接触猎物。每竿最多完成一次替换。")]
    private float predationProbability = 0.3f;
    [SerializeField, Min(0f), Tooltip("追捕速度相对于该鱼 FishController.swimSpeed 的倍数。1.5 表示基础追饵速度的 1.5 倍。")]
    private float predatorSpeedMultiplier = 1.5f;
    [SerializeField, Min(0f), Tooltip("捕食鱼嘴点到猎物 Collider 最近点的允许距离。单位：世界单位；越大越容易提前接触。仍需朝向猎物，且嘴到接触点之间没有障碍。")]
    private float mouthContactTolerance = 0.1f;
    [SerializeField, Tooltip("允许 Large 类别参与捕食挂钩的小鱼；仍需通过距离、概率和接触检查。关闭不影响普通追饵。")]
    private bool largeCanPredate = true;
    [SerializeField, Tooltip("允许 Special 类别参与捕食挂钩的小鱼；规则与 Large 相同。关闭不影响普通追饵。")]
    private bool specialCanPredate = true;

    public float FleeEnterRadius => fleeEnterRadius;
    public float FleeExitRadius => fleeExitRadius;
    public float FleeSpeedMultiplier => fleeSpeedMultiplier;
    public float PredatorDetectionRadius => predatorDetectionRadius;
    public float PredationProbability => predationProbability;
    public float PredatorSpeedMultiplier => predatorSpeedMultiplier;
    public float MouthContactTolerance => mouthContactTolerance;
    public bool LargeCanPredate => largeCanPredate;
    public bool SpecialCanPredate => specialCanPredate;

    public bool IsPredator(FishAppearanceCategory category)
    {
        return (largeCanPredate && category == FishAppearanceCategory.Large) ||
            (specialCanPredate && category == FishAppearanceCategory.Special);
    }

    private void OnValidate()
    {
        fleeEnterRadius = Mathf.Max(0f, fleeEnterRadius);
        fleeExitRadius = Mathf.Max(fleeEnterRadius + 0.1f, fleeExitRadius);
        fleeSpeedMultiplier = Mathf.Max(0f, fleeSpeedMultiplier);
        predatorDetectionRadius = Mathf.Max(0f, predatorDetectionRadius);
        predationProbability = Mathf.Clamp01(predationProbability);
        predatorSpeedMultiplier = Mathf.Max(0f, predatorSpeedMultiplier);
        mouthContactTolerance = Mathf.Max(0f, mouthContactTolerance);
    }
}
