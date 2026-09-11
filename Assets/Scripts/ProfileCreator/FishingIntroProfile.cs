using UnityEngine;

[CreateAssetMenu(menuName = "Seven Seas/Fishing Intro Profile")]
public sealed class FishingIntroProfile : ScriptableObject
{
    [Min(0f)] public float entrySeconds = 2f;
    [Min(0.01f), Tooltip("Downward survey speed in world units per second. Lower values move more slowly.")]
    public float surveySpeed = 2f;
    [Min(0f)] public float platformHoldSeconds = 0.6f;
    [Min(0f)] public float handoffSeconds = 0.8f;
    [Min(0f)] public float skipSeconds = 0.35f;
    [Min(0f)] public float barSeconds = 0.35f;
    [Range(0f, 0.3f), Tooltip("Height of EACH black bar as a fraction of screen height. 0.1 = 10% top + 10% bottom.")]
    public float barHeight = 0.1f;
    [Min(1f)] public float surveyOrthographicSize = 7f;
    [Min(0f), Tooltip("Minimum vertical separation; expanded if needed to keep the two maps apart.")]
    public float menuVerticalOffset = 55f;
    [Min(0f)] public float sceneryGap = 4f;

    private void OnValidate()
    {
        entrySeconds = Mathf.Max(0f, entrySeconds);
        surveySpeed = Mathf.Max(0.01f, surveySpeed);
        platformHoldSeconds = Mathf.Max(0f, platformHoldSeconds);
        handoffSeconds = Mathf.Max(0f, handoffSeconds);
        skipSeconds = Mathf.Max(0f, skipSeconds);
        barSeconds = Mathf.Max(0f, barSeconds);
        barHeight = Mathf.Clamp(barHeight, 0f, 0.3f);
        surveyOrthographicSize = Mathf.Max(1f, surveyOrthographicSize);
        menuVerticalOffset = Mathf.Max(0f, menuVerticalOffset);
        sceneryGap = Mathf.Max(0f, sceneryGap);
    }
}
