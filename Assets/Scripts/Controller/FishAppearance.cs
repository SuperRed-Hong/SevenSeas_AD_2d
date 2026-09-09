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
    }

    private void OnDisable()
    {
        // Successful catches and future object reuse must not retain the revealed appearance.
        RestoreHiddenAppearance();
    }
}
