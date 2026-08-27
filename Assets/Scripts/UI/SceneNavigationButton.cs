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
            Debug.LogError(
                "Scene navigation requires the game to start from Bootstrap.");
            return;
        }

        GetComponent<Button>().interactable = false;
        AppRoot.Instance.SceneLoader.LoadScene(targetScene);
    }
}