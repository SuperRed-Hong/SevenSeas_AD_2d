using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneLoader : MonoBehaviour
{
    [SerializeField] private SceneCatalog sceneCatalog;
    private static bool gameplayRequested;
    private static bool developerRequested;
    private static E_SceneID? bootstrapDestination;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEntryRequest()
    {
        gameplayRequested = false;
        developerRequested = false;
        bootstrapDestination = null;
    }

    public static E_SceneID ConsumeBootstrapDestination()
    {
        E_SceneID destination = bootstrapDestination ?? E_SceneID.MainMenu;
        bootstrapDestination = null;
        return destination;
    }

    public static bool ConsumeGameplayRequest()
    {
        bool requested = gameplayRequested;
        gameplayRequested = false;
        return requested;
    }

    public void LoadScene(E_SceneID sceneId)
        => LoadScene(sceneId, false);

    public static bool ConsumeDeveloperRequest()
    {
        bool requested = developerRequested;
        developerRequested = false;
        return requested;
    }

    public static void LoadMainMenuWithDeveloper()
    {
        if (AppRoot.Instance != null) AppRoot.Instance.SceneLoader.LoadScene(E_SceneID.MainMenu, true);
        else LoadWithoutAppRoot(E_SceneID.MainMenu, true);
    }

    private void LoadScene(E_SceneID sceneId, bool showDeveloper)
    {
        if (!sceneCatalog.TryGetSceneName(sceneId, out string sceneName))
        {
            Debug.LogError($"SceneCatalog does not contain a scene for ID: {sceneId}");
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError($"Scene ID {sceneId} does not have a valid scene name.");
            return;
        }

        LoadResolvedScene(sceneId, sceneName, showDeveloper);
    }

    public static void LoadWithoutAppRoot(E_SceneID sceneId)
        => LoadWithoutAppRoot(sceneId, false);

    private static void LoadWithoutAppRoot(E_SceneID sceneId, bool showDeveloper)
    {
        if (AppRoot.Instance == null &&
            (sceneId == E_SceneID.AttitudeControlTest || sceneId == E_SceneID.GyroscopeTest))
        {
            bootstrapDestination = sceneId;
            gameplayRequested = false;
            developerRequested = false;
            Time.timeScale = 1f;
            SceneManager.LoadSceneAsync("BootStrap");
            return;
        }
        string sceneName;
        switch (sceneId)
        {
            case E_SceneID.MainMenu:
            case E_SceneID.GameScene: sceneName = "FishingLoopTest"; break;
            case E_SceneID.Leaderboard: sceneName = "Leaderboard"; break;
            case E_SceneID.AttitudeControlTest: sceneName = "AttitudeControlTest"; break;
            case E_SceneID.GyroscopeTest: sceneName = "GyroscopeCastTest"; break;
            default: Debug.LogError($"No direct-play route for {sceneId}."); return;
        }
        LoadResolvedScene(sceneId, sceneName, showDeveloper);
    }

    private static void LoadResolvedScene(E_SceneID sceneId, string sceneName, bool showDeveloper)
    {
        gameplayRequested = sceneId == E_SceneID.GameScene;
        developerRequested = sceneId == E_SceneID.MainMenu && showDeveloper;
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync(sceneName);
    }
}
