using System.Runtime.InteropServices;
using UnityEngine;

public static class RuntimeInputPlatform
{
    private static bool? usesMobileControls;

    public static bool UsesMobileControls
    {
        get
        {
            if (usesMobileControls.HasValue)
            {
                return usesMobileControls.Value;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            usesMobileControls = SevenSeasIsMobileBrowser() != 0;
#else
            usesMobileControls = Application.isMobilePlatform;
#endif
            return usesMobileControls.Value;
        }
    }

    public static bool IsWebMobilePlayer
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return UsesMobileControls;
#else
            return false;
#endif
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        usesMobileControls = null;
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int SevenSeasIsMobileBrowser();
#endif
}
