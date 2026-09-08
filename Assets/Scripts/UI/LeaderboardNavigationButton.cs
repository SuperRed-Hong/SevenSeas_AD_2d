using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class LeaderboardNavigationButton : MonoBehaviour
{
    [SerializeField] private E_SceneID targetScene = E_SceneID.Leaderboard;

    public void Navigate()
    {
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
