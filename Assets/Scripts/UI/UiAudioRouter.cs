using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum UiButtonSound { Auto, Confirm, Back, Select, None }

// Only UI audio survives scene changes; music and ambience remain scene-owned.
[DefaultExecutionOrder(10000)]
public sealed class UiAudioRouter : MonoBehaviour
{
    private static UiAudioRouter instance;
    private AudioSource source;
    private AudioClip selectClip;
    private AudioClip confirmClip;
    private AudioClip backClip;
    private float selectLevel;
    private float confirmLevel;
    private float backLevel;
    private bool soundEnabled = true;
    private bool configured;
    private UiButtonSound pending = UiButtonSound.None;
    private int pendingPriority;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() => EnsureInstance();

    private static UiAudioRouter EnsureInstance()
    {
        if (instance == null)
            instance = new GameObject("UiAudioRouter").AddComponent<UiAudioRouter>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        instance = null;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (GameObject root in scene.GetRootGameObjects()) RegisterRoot(root);
    }

    public static void RegisterRoot(GameObject root)
    {
        if (root == null) return;
        EnsureInstance();
        foreach (Button button in root.GetComponentsInChildren<Button>(true)) RegisterButton(button);
    }

    public static void RegisterButton(Button button, UiButtonSound sound = UiButtonSound.Auto)
    {
        if (button == null) return;
        UiButtonAudioFeedback feedback = button.GetComponent<UiButtonAudioFeedback>();
        if (feedback == null) feedback = button.gameObject.AddComponent<UiButtonAudioFeedback>();
        feedback.Bind(button, sound);
    }

    public static bool ResolveSoundEnabled(bool initialValue)
    {
        UiAudioRouter router = EnsureInstance();
        return router.configured ? router.soundEnabled : initialValue;
    }

    public static void Configure(AudioClip select, AudioClip confirm, AudioClip back,
        float volume, float selectGain, float confirmGain, float backGain, bool enabled)
    {
        UiAudioRouter router = EnsureInstance();
        router.selectClip = select;
        router.confirmClip = confirm;
        router.backClip = back;
        router.selectLevel = selectGain;
        router.confirmLevel = confirmGain;
        router.backLevel = backGain;
        router.source.volume = volume;
        router.configured = true;
        SetSoundEnabled(enabled);
    }

    public static void SetSoundEnabled(bool value)
    {
        UiAudioRouter router = EnsureInstance();
        if (router.soundEnabled == value) return;
        router.soundEnabled = value;
        if (value) return;
        router.pending = UiButtonSound.None;
        router.pendingPriority = 0;
        router.source.Stop();
    }

    public static void Queue(UiButtonSound sound, bool activation = false)
    {
        if (sound == UiButtonSound.None || sound == UiButtonSound.Auto) return;
        UiAudioRouter router = EnsureInstance();
        if (!router.soundEnabled) return;
        int priority = activation || sound != UiButtonSound.Select ? 2 : 1;
        if (priority < router.pendingPriority) return;
        router.pending = sound;
        router.pendingPriority = priority;
    }

    private void LateUpdate()
    {
        UiButtonSound sound = pending;
        pending = UiButtonSound.None;
        pendingPriority = 0;
        if (!soundEnabled) return;
        AudioClip clip;
        float gain;
        switch (sound)
        {
            case UiButtonSound.Select: clip = selectClip; gain = selectLevel; break;
            case UiButtonSound.Confirm: clip = confirmClip; gain = confirmLevel; break;
            case UiButtonSound.Back: clip = backClip; gain = backLevel; break;
            default: return;
        }
        if (clip != null) source.PlayOneShot(clip, gain);
    }

    public void PlayUiSelect() => Queue(UiButtonSound.Select, true);
    public void PlayUiConfirm() => Queue(UiButtonSound.Confirm, true);
    public void PlayUiBack() => Queue(UiButtonSound.Back, true);

    internal static bool IsBackAction(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        return name.StartsWith("Close", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("Back", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("Cancel", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("Return", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("Resume", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("MainMenu", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("MAIN MENU", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("返回", StringComparison.Ordinal) ||
            name.StartsWith("关闭", StringComparison.Ordinal);
    }
}
