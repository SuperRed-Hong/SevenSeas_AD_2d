using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneLoader : MonoBehaviour
{
    [SerializeField] private SceneCatalog sceneCatalog;

    public void LoadScene(E_SceneID sceneId)
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

        SceneManager.LoadSceneAsync(sceneName);
    }
}
