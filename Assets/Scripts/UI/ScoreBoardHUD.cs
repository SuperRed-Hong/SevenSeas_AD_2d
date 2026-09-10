using TMPro;
using UnityEngine;

// 纯显示：只读 ScoreTracker 的分数事件，不参与计分规则。
// 收放动作交给 PullOutPanel，这里只负责加分时临时弹出和文字表现。
public sealed class ScoreBoardHUD : MonoBehaviour
{
    [Header("Source")]
    [SerializeField]
    [Tooltip("提供分数与变更事件的局内计分器。")]
    private ScoreTracker scoreTracker;

    [Header("Parts")]
    [SerializeField]
    [Tooltip("负责收放和点击的抽屉面板。")]
    private PullOutPanel pullOutPanel;

    [SerializeField]
    [Tooltip("卷轴米色区里的分数文字。")]
    private TMP_Text scoreText;

    [Header("Auto Reveal")]
    [SerializeField]
    [Tooltip("关掉后分数变化不再自动弹出，只能点击展开。")]
    private bool revealOnScoreChanged = true;

    [SerializeField, Min(0f)]
    [Tooltip("自动弹出后保持展开的停留秒数。")]
    private float holdDuration = 2f;

    [Header("Text")]
    [SerializeField]
    [Tooltip("分数文字格式，{0} 是分数。")]
    private string scoreFormat = "SCORE {0}";

    [SerializeField, Min(1f)]
    [Tooltip("加分时文字放大的峰值倍数。")]
    private float popScale = 1.2f;

    [SerializeField, Min(0f)]
    [Tooltip("放大回落的时长（秒）。")]
    private float popDuration = 0.25f;

    private float popTimeLeft;
    private Vector3 textBaseScale = Vector3.one;

    private void Awake()
    {
        if (scoreTracker == null || pullOutPanel == null)
        {
            Debug.LogError("ScoreBoardHUD 需要 ScoreTracker 和 PullOutPanel。", this);
            enabled = false;
            return;
        }

        if (scoreText != null)
        {
            textBaseScale = scoreText.transform.localScale;
        }

        ApplyScore(scoreTracker.Score);
    }

    private void OnEnable()
    {
        if (scoreTracker == null)
        {
            return;
        }

        scoreTracker.ScoreChanged += HandleScoreChanged;
        // 重新启用时补一次当前分数，避免错过关闭期间的变更。
        ApplyScore(scoreTracker.Score);
    }

    private void OnDisable()
    {
        if (scoreTracker != null)
        {
            scoreTracker.ScoreChanged -= HandleScoreChanged;
        }

        popTimeLeft = 0f;
        if (scoreText != null)
        {
            scoreText.transform.localScale = textBaseScale;
        }
    }

    private void HandleScoreChanged(int score)
    {
        ApplyScore(score);
        popTimeLeft = popDuration;

        if (revealOnScoreChanged && pullOutPanel != null)
        {
            pullOutPanel.OpenTemporarily(holdDuration);
        }
    }

    private void LateUpdate()
    {
        ApplyPop(Time.deltaTime);
    }

    private void ApplyScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = string.Format(scoreFormat, score);
        }
    }

    private void ApplyPop(float deltaTime)
    {
        if (scoreText == null || popDuration <= 0f)
        {
            return;
        }

        if (popTimeLeft <= 0f)
        {
            scoreText.transform.localScale = textBaseScale;
            return;
        }

        popTimeLeft = Mathf.Max(0f, popTimeLeft - deltaTime);
        float remaining = popTimeLeft / popDuration;
        scoreText.transform.localScale =
            textBaseScale * Mathf.Lerp(1f, popScale, remaining);
    }
}
