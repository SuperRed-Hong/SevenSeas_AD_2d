using System.Runtime.InteropServices;
using UnityEngine;

public enum WebMotionPermissionState
{
    NotRequired,
    NotRequested,
    Requesting,
    Granted,
    Denied,
    Unsupported
}

public sealed class WebMotionPermission : MonoBehaviour
{
    private const string ReceiverName = "SevenSeasWebMotionPermission";
    private static WebMotionPermission instance;

    public static WebMotionPermissionState MotionState { get; private set; } = WebMotionPermissionState.NotRequested;
    public static WebMotionPermissionState OrientationState { get; private set; } = WebMotionPermissionState.NotRequested;
    public static bool IsInIframe { get; private set; }

    public static WebMotionPermissionState State
    {
        get
        {
            if (MotionState == WebMotionPermissionState.Requesting ||
                OrientationState == WebMotionPermissionState.Requesting)
                return WebMotionPermissionState.Requesting;
            if (MotionState == WebMotionPermissionState.Granted ||
                OrientationState == WebMotionPermissionState.Granted)
                return WebMotionPermissionState.Granted;
            if (MotionState == WebMotionPermissionState.Denied ||
                OrientationState == WebMotionPermissionState.Denied)
                return WebMotionPermissionState.Denied;
            return MotionState;
        }
    }

    private static bool automaticTouchFallback;
    public static bool ForceTouchControls { get; private set; }
    public static bool TouchFallbackActive => ForceTouchControls || automaticTouchFallback;

    public static void SetForceTouchControls(bool value) => ForceTouchControls = value;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        instance = null;
        MotionState = OrientationState = WebMotionPermissionState.NotRequested;
        IsInIframe = false;
        automaticTouchFallback = false;
        ForceTouchControls = false;
    }

    public static void RequestIfNeeded()
    {
        if (ForceTouchControls) return;
        if (!RuntimeInputPlatform.UsesMobileControls)
        {
            MotionState = OrientationState = WebMotionPermissionState.NotRequired;
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        EnsureReceiver();
        IsInIframe = SevenSeasIsInIframe() != 0;
        if (State == WebMotionPermissionState.Requesting ||
            MotionState == WebMotionPermissionState.Granted)
        {
            return;
        }

        MotionState = OrientationState = WebMotionPermissionState.Requesting;
        SevenSeasRequestMotionPermission(ReceiverName);
#else
        MotionState = OrientationState = WebMotionPermissionState.Granted;
#endif
    }

    public static void ActivateTouchFallback()
    {
        if (!RuntimeInputPlatform.IsWebMobilePlayer)
        {
            return;
        }

        automaticTouchFallback = true;
        Debug.Log("Web motion input is unavailable. Touch controls are now active.");
    }

    private static void EnsureReceiver()
    {
        if (instance != null)
        {
            return;
        }

        GameObject receiver = new GameObject(ReceiverName);
        instance = receiver.AddComponent<WebMotionPermission>();
        DontDestroyOnLoad(receiver);
    }

    public void ReceivePermissionResult(string result)
    {
        // Default missing/malformed fields so neither channel remains Requesting.
        MotionState = OrientationState = WebMotionPermissionState.Unsupported;
        if (!string.IsNullOrEmpty(result))
        {
            if (!result.Contains("="))
            {
                MotionState = OrientationState = ParseState(result);
            }
            else
            {
                foreach (string part in result.Split(';'))
                {
                    int separator = part.IndexOf('=');
                    if (separator <= 0) continue;
                    string key = part.Substring(0, separator).Trim();
                    WebMotionPermissionState value = ParseState(part.Substring(separator + 1).Trim());
                    if (key == "motion") MotionState = value;
                    else if (key == "orientation") OrientationState = value;
                }
            }
        }
        Debug.Log($"Web motion permission: motion={MotionState}, orientation={OrientationState}");
    }

    private static WebMotionPermissionState ParseState(string value)
    {
        return value switch
        {
            "granted" => WebMotionPermissionState.Granted,
            "denied" => WebMotionPermissionState.Denied,
            _ => WebMotionPermissionState.Unsupported
        };
    }

    public static void OpenTopLevel()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SevenSeasOpenSelfTopLevel();
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void SevenSeasRequestMotionPermission(string receiverName);
    [DllImport("__Internal")]
    private static extern int SevenSeasIsInIframe();
    [DllImport("__Internal")]
    private static extern void SevenSeasOpenSelfTopLevel();
#endif
}
