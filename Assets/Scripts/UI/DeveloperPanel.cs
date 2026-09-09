using TMPro;
using UnityEngine;

public sealed class DeveloperPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text tutorialStatus;
    [SerializeField] private TMP_Text gamesStatus;
    [SerializeField] private TMP_Text[] nameLabels;
    [SerializeField] private string[] developerNames = new string[7];

    public void Open()
    {
        Refresh();
        panelRoot.transform.SetAsLastSibling();
        panelRoot.SetActive(true);
    }

    public void Close() => panelRoot.SetActive(false);

    public void ToggleTutorialCompleted()
    {
        LocalPlayerProgress.TutorialCompleted = !LocalPlayerProgress.TutorialCompleted;
        Refresh();
    }

    private void Refresh()
    {
        tutorialStatus.text = "Tutorial Completed: " + (LocalPlayerProgress.TutorialCompleted ? "TRUE" : "FALSE");
        gamesStatus.text = "Games Started: " + LocalPlayerProgress.GamesStarted;
        for (int i = 0; i < nameLabels.Length; i++)
        {
            if (nameLabels[i] == null) continue;
            string name = i < developerNames.Length ? developerNames[i] : "";
            nameLabels[i].text = string.IsNullOrWhiteSpace(name) ? $"{i + 1:00}  ---" : name;
        }
    }
}
