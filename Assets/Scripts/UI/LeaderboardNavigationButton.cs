using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class LeaderboardNavigationButton : MonoBehaviour
{
    [SerializeField] private E_SceneID targetScene = E_SceneID.Leaderboard;
    [SerializeField] private bool showDeveloperOnMainMenu;
    private bool navigationStarted;

    private void OnEnable() => navigationStarted = false;

    public void Navigate()
    {
        if (navigationStarted) return;
        navigationStarted = true;
        if (targetScene == E_SceneID.MainMenu && showDeveloperOnMainMenu)
        {
            SceneLoader.LoadMainMenuWithDeveloper();
            return;
        }
        Time.timeScale = 1f;
        if (AppRoot.Instance != null)
        {
            AppRoot.Instance.SceneLoader.LoadScene(targetScene);
            return;
        }

        // Direct Editor play does not create AppRoot, but these UI routes still work.
        SceneLoader.LoadWithoutAppRoot(targetScene);
    }
}
