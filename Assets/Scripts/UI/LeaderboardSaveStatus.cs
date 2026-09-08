using TMPro;
using UnityEngine;

public sealed class LeaderboardSaveStatus : MonoBehaviour
{
    [SerializeField] private FishingLoopController loopController;
    [SerializeField] private TMP_Text statusText;

    private void OnEnable()
    {
        statusText.text = loopController.FinalScoreSaved
            ? "Local best scores updated" : "Could not save local score";
    }
}
