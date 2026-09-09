using UnityEngine;
using UnityEngine.UI;

public class TutorialPanelController : MonoBehaviour
{
    [SerializeField] private GameObject tutorialRoot;
    [SerializeField] private GameObject[] pages;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    private int currentPage;

    public void Open()
    {
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
    }

    public void Close()
    {
        tutorialRoot.SetActive(false);
    }
}
