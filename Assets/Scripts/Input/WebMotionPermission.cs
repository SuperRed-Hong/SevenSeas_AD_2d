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

    public static WebMotionPermissionState State { get; private set; } =
        WebMotionPermissionState.NotRequested;

    public static bool TouchFallbackActive { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        instance = null;
        State = WebMotionPermissionState.NotRequested;
        TouchFallbackActive = false;
    }

    public static void RequestIfNeeded()
    {
        if (!RuntimeInputPlatform.UsesMobileControls)
        {
            State = WebMotionPermissionState.NotRequired;
            return;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        EnsureReceiver();
        if (State == WebMotionPermissionState.Requesting ||
            State == WebMotionPermissionState.Granted)
        {
            return;
        }

        State = WebMotionPermissionState.Requesting;
        SevenSeasRequestMotionPermission(ReceiverName);
#else
        State = WebMotionPermissionState.Granted;
#endif
    }

    public static void ActivateTouchFallback()
    {
        if (!RuntimeInputPlatform.IsWebMobilePlayer)
        {
            return;
        }

        TouchFallbackActive = true;
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
        State = result switch
        {
            "granted" => WebMotionPermissionState.Granted,
            "denied" => WebMotionPermissionState.Denied,
            _ => WebMotionPermissionState.Unsupported
        };
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void SevenSeasRequestMotionPermission(string receiverName);
#endif
}
