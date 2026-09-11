using UnityEngine;

public sealed class FishAppearance : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer fishRenderer;

    [SerializeField]
    private FishAppearanceProfile appearanceProfile;

    [SerializeField]
    private FishAppearanceCategory category;

    private Sprite revealedSprite;
    private Sprite hiddenSprite;
    private Color hiddenColor;
    private bool isRevealed;
    private Animator silhouetteAnimator;
    private bool restoreSilhouetteAnimation;

    public FishAppearanceCategory Category => category;
    public Sprite RevealedSprite => revealedSprite;

    private void Awake()
    {
        if (fishRenderer == null)
        {
            fishRenderer = GetComponent<SpriteRenderer>();
        }

        if (fishRenderer != null)
        {
            silhouetteAnimator = fishRenderer.GetComponent<Animator>();
            // Capture the final variant appearance without changing its transform or collider.
            hiddenSprite = fishRenderer.sprite;
            hiddenColor = fishRenderer.color;
        }
    }

    public void Reveal()
    {
        if (isRevealed || fishRenderer == null || appearanceProfile == null)
        {
            return;
        }

        // Keep this fish's identity when a failed catch restores its silhouette.
        if (revealedSprite == null)
        {
            revealedSprite = appearanceProfile.PickRandomSprite(category);
        }

        if (revealedSprite == null)
        {
            return;
        }

        // The silhouette clip also writes m_Sprite; suspend only this renderer's animator.
        restoreSilhouetteAnimation = silhouetteAnimator != null && silhouetteAnimator.enabled;
        if (restoreSilhouetteAnimation) silhouetteAnimator.enabled = false;
        fishRenderer.sprite = revealedSprite;
        fishRenderer.color = Color.white;
        isRevealed = true;
    }

    public void RestoreHiddenAppearance()
    {
        if (!isRevealed)
        {
            return;
        }

        if (fishRenderer != null)
        {
            fishRenderer.sprite = hiddenSprite;
            fishRenderer.color = hiddenColor;
        }

        isRevealed = false;
        if (restoreSilhouetteAnimation && silhouetteAnimator != null) silhouetteAnimator.enabled = true;
        restoreSilhouetteAnimation = false;
    }

    private void OnDisable()
    {
        // Successful catches and future object reuse must not retain the revealed appearance.
        RestoreHiddenAppearance();
    }
}
