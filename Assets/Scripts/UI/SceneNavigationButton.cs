using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class SceneNavigationButton : MonoBehaviour
{
    [SerializeField] private E_SceneID targetScene;

    public void Navigate()
    {
        if (AppRoot.Instance == null)
        {
            SceneLoader.LoadWithoutAppRoot(targetScene);
            return;
        }

        GetComponent<Button>().interactable = false;
        AppRoot.Instance.SceneLoader.LoadScene(targetScene);
    }
}
