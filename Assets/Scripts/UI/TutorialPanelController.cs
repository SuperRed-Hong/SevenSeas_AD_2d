using UnityEngine;
using UnityEngine.UI;

public class TutorialPanelController : MonoBehaviour
{
    [SerializeField] private GameObject tutorialRoot;
    [SerializeField] private GameObject[] pages;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    private System.Action onCompleted;
    private System.Action onCancelled;

    private int currentPage;
    private bool hasReachedLastPage;

    // Lets a caller that hid itself to show the tutorial know when to come back.
    public bool IsOpen => tutorialRoot != null && tutorialRoot.activeInHierarchy;

    public void Open()
    {
        onCompleted = null;
        onCancelled = null;
        Show();
    }

    public bool OpenForFirstGame(System.Action completed, System.Action cancelled)
    {
        if (tutorialRoot == null || pages == null || pages.Length == 0)
            return false;
        onCompleted = completed;
        onCancelled = cancelled;
        Show();
        return true;
    }

    private void Show()
    {
        hasReachedLastPage = false;
        ShowPage(0);
        tutorialRoot.transform.SetAsLastSibling();
        tutorialRoot.SetActive(true);
    }

    public void Next()
    {
        ShowPage(currentPage + 1);
    }

    public void Previous()
    {
        ShowPage(currentPage - 1);
    }

    public void ShowPage(int index)
    {
       if(pages == null || pages.Length == 0) return;
       
       currentPage = Mathf.Clamp(index, 0,  pages.Length - 1);

       for (int i = 0; i < pages.Length; i++)
       {
           if (pages[i] != null)
           {
               pages[i].SetActive(i == currentPage);
           }
       }
       if (previousButton != null) previousButton.interactable = currentPage > 0;
       if (nextButton != null) nextButton.interactable = currentPage < pages.Length - 1;
       if (currentPage == pages.Length - 1) hasReachedLastPage = true;
    }

    public void Close()
    {
        bool completedTutorial = onCompleted != null && hasReachedLastPage;
        var callback = completedTutorial ? onCompleted : onCancelled;
        // Clear callbacks before continuing so repeated close clicks cannot start twice.
        onCompleted = null;
        onCancelled = null;
        if (completedTutorial) LocalPlayerProgress.TutorialCompleted = true;
        tutorialRoot.SetActive(false);
        callback?.Invoke();
    }
}
