using UnityEngine;
[RequireComponent(typeof(GyroscopeReader))]
[RequireComponent(
    typeof(SceneLoader),
    typeof(AttitudeReader),
    typeof(AttitudeCalibrationService))]
public sealed class AppRoot : MonoBehaviour
{
    public static AppRoot Instance { get; private set; }

    public SceneLoader SceneLoader { get; private set; }
    public AttitudeReader AttitudeReader { get; private set; }
    public GyroscopeReader GyroscopeReader { get; private set; }
    public AttitudeCalibrationService AttitudeCalibration
    {
        get;
        private set;
    }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SceneLoader = GetComponent<SceneLoader>();
        AttitudeReader = GetComponent<AttitudeReader>();

        AttitudeCalibration =
            GetComponent<AttitudeCalibrationService>();
        GyroscopeReader = GetComponent<GyroscopeReader>();
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