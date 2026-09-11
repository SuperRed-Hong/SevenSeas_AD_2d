using TMPro;
using UnityEngine;

public sealed class DeveloperPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text tutorialStatus;
    [SerializeField] private TMP_Text gamesStatus;

    public void Open()
    {
        Refresh();
        panelRoot.transform.SetAsLastSibling();
        panelRoot.SetActive(true);
    }

    public void Close() => panelRoot.SetActive(false);

    [ContextMenu("Toggle Tutorial Completed")]
    public void ToggleTutorialCompleted()
    {
        LocalPlayerProgress.TutorialCompleted = !LocalPlayerProgress.TutorialCompleted;
        Refresh();
#if UNITY_EDITOR
        Debug.Log($"Tutorial Completed: {LocalPlayerProgress.TutorialCompleted}. Applies to the next Start.", this);
#endif
    }

    private void Refresh()
    {
        tutorialStatus.text = "Tutorial";
        gamesStatus.text = "Games Started: " + LocalPlayerProgress.GamesStarted;
    }
}
