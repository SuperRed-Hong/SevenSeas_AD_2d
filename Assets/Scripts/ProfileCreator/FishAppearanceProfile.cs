using System.Collections.Generic;
using UnityEngine;

public enum FishAppearanceCategory
{
    Small,
    Medium,
    Large,
    Special
}

[CreateAssetMenu(fileName = "FishAppearanceProfile", menuName = "Seven Seas/Fish Appearance Profile")]
public sealed class FishAppearanceProfile : ScriptableObject
{
    [SerializeField] private List<Sprite> small = new();
    [SerializeField] private List<Sprite> medium = new();
    [SerializeField] private List<Sprite> large = new();
    [SerializeField] private List<Sprite> special = new();

    public Sprite PickRandomSprite(FishAppearanceCategory category)
    {
        List<Sprite> sprites;
        switch (category)
        {
            case FishAppearanceCategory.Small: sprites = small; break;
            case FishAppearanceCategory.Medium: sprites = medium; break;
            case FishAppearanceCategory.Large: sprites = large; break;
            case FishAppearanceCategory.Special: sprites = special; break;
            default: return null;
        }

        if (sprites == null)
        {
            return null;
        }

        // Ignore empty Inspector slots while keeping valid entries equally likely.
        int validCount = 0;
        foreach (Sprite sprite in sprites)
        {
            if (sprite != null) validCount++;
        }

        if (validCount == 0)
        {
            return null;
        }

        int selectedIndex = Random.Range(0, validCount);
        foreach (Sprite sprite in sprites)
        {
            if (sprite == null) continue;
            if (selectedIndex-- == 0) return sprite;
        }

        return null;
    }
}
