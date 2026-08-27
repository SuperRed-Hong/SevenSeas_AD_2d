using UnityEngine;

[RequireComponent(typeof(SceneLoader))]
public sealed class AppRoot : MonoBehaviour
{
    public static AppRoot Instance { get; private set; }

    public SceneLoader SceneLoader { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SceneLoader = GetComponent<SceneLoader>();

        DontDestroyOnLoad(gameObject);
    }
    private void Start()
    {
        SceneLoader.LoadScene(E_SceneID.MainMenu);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}