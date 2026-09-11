using UnityEngine;

public static class LocalPlayerProgress
{
    private const string TutorialKey = "SevenSeas.TutorialCompleted";
    private const string GamesKey = "SevenSeas.GamesStarted";

    public static bool TutorialCompleted
    {
        get => PlayerPrefs.GetInt(TutorialKey, 0) != 0;
        set
        {
            PlayerPrefs.SetInt(TutorialKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static int GamesStarted => PlayerPrefs.GetInt(GamesKey, 0);

    public static void RecordGameStarted()
    {
        PlayerPrefs.SetInt(GamesKey, GamesStarted < int.MaxValue ? GamesStarted + 1 : int.MaxValue);
        PlayerPrefs.Save();
    }
}
