using System.Collections.Generic;
using UnityEngine;

public sealed class FishingAudioFeedback : MonoBehaviour
{
    [SerializeField] private FishingLoopController loop;
    [SerializeField] private AudioClip uiSelect;
    [SerializeField] private AudioClip uiConfirm;
    [SerializeField] private AudioClip uiBack;
    [SerializeField] private AudioClip waterLanding;
    [SerializeField] private AudioClip strikeSuccess;
    [SerializeField] private AudioClip catchSuccess;
    [SerializeField] private AudioClip attemptFailure;
    [SerializeField] private AudioClip castLaunch;
    [SerializeField] private AudioClip musicLoop;
    [SerializeField] private AudioClip ambientLoop;
    [SerializeField] private bool soundEnabled = true;
    [SerializeField, Range(0f, 1f)] private float volume = 0.3f;
    [SerializeField, Range(0f, 1f)] private float musicLevel = 0.5f;
    [SerializeField, Range(0f, 1f)] private float ambientLevel = 0.35f;
    [SerializeField, Min(0f), Tooltip("Seconds for ambient audio to fade in when starting or resuming. Zero disables the fade.")]
    private float ambientFadeInSeconds = 1.5f;
    [SerializeField, Range(0f, 3f)] private float uiSelectLevel = 1f;
    [SerializeField, Range(0f, 3f)] private float uiConfirmLevel = 1f;
    [SerializeField, Range(0f, 3f)] private float uiBackLevel = 1f;
    [SerializeField, Range(0f, 3f)] private float waterLandingLevel = 1f;
    [SerializeField, Range(0f, 3f)] private float strikeSuccessLevel = 1f;
    [SerializeField, Range(0f, 3f)] private float catchSuccessLevel = 2f;
    [SerializeField, Range(0f, 3f)] private float attemptFailureLevel = 1f;
    [SerializeField, Range(0f, 3f)] private float castLaunchLevel = 1f;

    private readonly HashSet<int> settledAttempts = new();
    private AudioSource gameplaySource;
    private AudioSource ambientSource;
    private AudioSource musicSource;
    private FishingLoopState previousState;
    private bool paused;
    private bool runStarted;
    private bool gameOver;
    private bool ambientStarted;
    private bool ambientPlaying;
    private float ambientFadeProgress;
    private bool musicStarted;

    public bool SoundEnabled
    {
        get => soundEnabled;
        set
        {
            if (soundEnabled == value) return;
            soundEnabled = value;
            UiAudioRouter.SetSoundEnabled(value);
            if (!value)
            {
                if (gameplaySource != null) gameplaySource.Stop();
                PauseAmbient();
                if (musicSource != null) musicSource.Pause();
            }
            else
            {
                // Re-enabling sound never replays missed or interrupted one-shots.
                ResumeAmbient();
            }
        }
    }

    private void Awake()
    {
        soundEnabled = UiAudioRouter.ResolveSoundEnabled(soundEnabled);
        EnsureSources();
        SyncUiAudio();
    }

    private void OnEnable()
    {
        if (loop == null) return;
        previousState = loop.CurrentState;
        gameOver = previousState == FishingLoopState.GameOver;
        loop.DistanceMultiplierLocked += HandleWaterLanding;
        loop.StateChanged += HandleStateChanged;
        loop.CatchCompleted += HandleCatch;
        loop.AttemptFailed += HandleFailure;
        // This scene also contains the menu; music starts when its interface appears.
        ResumeAmbient();
    }

    private void OnDisable()
    {
        if (loop != null)
        {
            loop.DistanceMultiplierLocked -= HandleWaterLanding;
            loop.StateChanged -= HandleStateChanged;
            loop.CatchCompleted -= HandleCatch;
            loop.AttemptFailed -= HandleFailure;
        }

        if (gameplaySource != null) gameplaySource.Stop();
        if (ambientSource != null) ambientSource.Stop();
        ResetAmbientFade();
        if (musicSource != null) musicSource.Stop();
        ambientStarted = false;
        musicStarted = false;
        runStarted = false;
    }

    public void PlayUiSelect() => UiAudioRouter.Queue(UiButtonSound.Select, true);
    public void PlayUiConfirm() => UiAudioRouter.Queue(UiButtonSound.Confirm, true);
    public void PlayUiBack() => UiAudioRouter.Queue(UiButtonSound.Back, true);

    public void SetPaused(bool value)
    {
        if (paused == value) return;
        paused = value;
        if (value)
        {
            // UI has its own source, so navigation remains audible while gameplay is suspended.
            if (gameplaySource != null) gameplaySource.Stop();
            PauseAmbient();
            if (musicSource != null) musicSource.Pause();
        }
        else ResumeAmbient();
    }

    private void HandleWaterLanding()
    {
        if (!gameOver) PlayGameplay(waterLanding, waterLandingLevel);
    }

    private void HandleStateChanged(FishingLoopState state)
    {
        if (state == FishingLoopState.GameOver)
        {
            gameOver = true;
            if (gameplaySource != null) gameplaySource.Stop();
            if (ambientSource != null) ambientSource.Stop();
            ResetAmbientFade();
            if (musicSource != null) musicSource.Stop();
            ambientStarted = false;
            musicStarted = false;
        }
        else if (!gameOver)
        {
            if (state == FishingLoopState.Casting && previousState != FishingLoopState.Casting)
            {
                runStarted = true;
                ResumeAmbient();
                PlayGameplay(castLaunch, castLaunchLevel);
            }

            if (previousState == FishingLoopState.Striking && state == FishingLoopState.Reeling)
                PlayGameplay(strikeSuccess, strikeSuccessLevel);
        }

        previousState = state;
    }

    private void HandleCatch(CatchResult result)
    {
        if (!settledAttempts.Add(result.AttemptId)) return;
        if (!gameOver) PlayGameplay(catchSuccess, catchSuccessLevel);
    }

    private void HandleFailure(AttemptFailureResult result)
    {
        if (!settledAttempts.Add(result.AttemptId)) return;
        // The last-hook failure is published after the coordinator enters GameOver.
        PlayGameplay(attemptFailure, attemptFailureLevel);
    }

    private void PlayGameplay(AudioClip clip, float level)
    {
        if (!isActiveAndEnabled || !soundEnabled || paused || clip == null) return;
        EnsureSources();
        gameplaySource.PlayOneShot(clip, level);
    }

    private void ResumeAmbient()
    {
        if (!isActiveAndEnabled || !soundEnabled || paused || gameOver)
            return;
        EnsureSources();
        if (runStarted && ambientLoop != null && !ambientPlaying)
        {
            ambientFadeProgress = ambientFadeInSeconds > 0f ? 0f : 1f;
            ApplyVolumes();
            ResumeLoop(ambientSource, ambientLoop, ref ambientStarted);
            ambientPlaying = true;
        }
        ResumeLoop(musicSource, musicLoop, ref musicStarted);
    }

    private void PauseAmbient()
    {
        if (ambientSource != null) ambientSource.Pause();
        ResetAmbientFade();
    }

    private void ResetAmbientFade()
    {
        ambientPlaying = false;
        ambientFadeProgress = 0f;
        if (ambientSource != null) ambientSource.volume = 0f;
    }

    private static void ResumeLoop(AudioSource source, AudioClip clip, ref bool started)
    {
        if (clip == null) return;
        if (started)
        {
            source.UnPause();
            return;
        }

        source.clip = clip;
        source.Play();
        started = true;
    }

    private void EnsureSources()
    {
        if (gameplaySource == null) gameplaySource = CreateSource("GameplayAudio", false);
        if (ambientSource == null) ambientSource = CreateSource("AmbientAudio", true);
        if (musicSource == null) musicSource = CreateSource("MusicAudio", true);
        ApplyVolumes();
    }

    // Keep Inspector adjustments audible while the loops are already playing.
    private void LateUpdate()
    {
        if (ambientPlaying && soundEnabled && !paused && !gameOver)
        {
            ambientFadeProgress = ambientFadeInSeconds > 0f
                ? Mathf.MoveTowards(ambientFadeProgress, 1f, Time.unscaledDeltaTime / ambientFadeInSeconds)
                : 1f;
        }
        ApplyVolumes();
        SyncUiAudio();
    }

    private void SyncUiAudio() => UiAudioRouter.Configure(uiSelect, uiConfirm, uiBack, volume,
        uiSelectLevel, uiConfirmLevel, uiBackLevel, soundEnabled);

    private void ApplyVolumes()
    {
        if (gameplaySource == null || ambientSource == null || musicSource == null) return;
        gameplaySource.volume = volume;
        ambientSource.volume = volume * ambientLevel * ambientFadeProgress;
        musicSource.volume = volume * musicLevel;
    }

    private AudioSource CreateSource(string name, bool looping)
    {
        var child = new GameObject(name);
        child.transform.SetParent(transform, false);
        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = looping;
        source.spatialBlend = 0f;
        source.volume = volume;
        return source;
    }
}
