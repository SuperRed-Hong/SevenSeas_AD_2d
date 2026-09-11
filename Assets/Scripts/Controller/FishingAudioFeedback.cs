using System.Collections.Generic;
using UnityEngine;

public sealed class FishingAudioFeedback : MonoBehaviour
{
    [SerializeField] private FishingLoopController loop;
    [SerializeField, Tooltip("开场运镜开始时启动环境循环音，并使用 Ambient Fade In Seconds 渐入。")]
    private FishingIntroController intro;
    [SerializeField] private AudioClip uiSelect;
    [SerializeField] private AudioClip uiConfirm;
    [SerializeField] private AudioClip uiBack;
    [SerializeField] private AudioClip waterLanding;
    [SerializeField] private AudioClip strikeSuccess;
    [SerializeField] private AudioClip catchSuccess;
    [SerializeField] private AudioClip attemptFailure;
    [SerializeField, Tooltip("Played once when the session ends, whether time or hooks run out.")]
    private AudioClip gameOverClip;
    [SerializeField, Tooltip("Played when a predator devours the hooked small fish.")]
    private AudioClip predationBite;
    [SerializeField, Tooltip("Looped while a hooked fish is being retrieved.")]
    private AudioClip reelingLoop;
    [SerializeField, Tooltip("Played when a strike lands outside the target band and the window stays open.")]
    private AudioClip strikeRejected;
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
    [SerializeField, Range(0f, 3f)] private float gameOverLevel = 1f;
    [SerializeField, Range(0f, 3f)] private float predationBiteLevel = 1.4f;
    [SerializeField, Range(0f, 3f)] private float strikeRejectedLevel = 0.8f;
    [SerializeField, Range(0f, 1f)] private float reelingLevel = 0.6f;
    [SerializeField, Min(0f), Tooltip("Seconds for the reeling loop to fade in and out. Zero switches instantly.")]
    private float reelingFadeSeconds = 0.25f;
    [SerializeField, Range(0.5f, 1.5f), Tooltip("Reeling loop pitch while the line is slack.")]
    private float reelingPitchMin = 0.95f;
    [SerializeField, Range(0.5f, 1.5f), Tooltip("Reeling loop pitch as tension approaches breaking point.")]
    private float reelingPitchMax = 1.15f;
    [SerializeField, Min(0f), Tooltip("Seconds for the reeling pitch to travel its full range. Zero tracks tension instantly.")]
    private float reelingPitchSmoothing = 0.35f;
    [SerializeField, Range(0f, 0.25f), Tooltip("Random pitch spread on repeated one-shots. Zero disables variation.")]
    private float pitchVariation = 0.06f;
    [SerializeField, Range(0f, 3f)] private float castLaunchLevel = 1f;

    private const int VariedSourceCount = 4;

    private readonly HashSet<int> settledAttempts = new();
    private AudioSource gameplaySource;
    private AudioSource ambientSource;
    private AudioSource musicSource;
    private AudioSource reelingSource;
    private FishingLoopState previousState;
    private bool paused;
    private bool runStarted;
    private bool gameOver;
    private bool ambientStarted;
    private bool ambientPlaying;
    private float ambientFadeProgress;
    private bool reelingActive;
    private float reelingFadeProgress;
    private float reelingPitch = 1f;
    private AudioSource[] variedSources;
    private int nextVariedSource;
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
                StopVaried();
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
        // Recover an omitted scene reference without binding to an intro in another loaded scene.
        if (intro == null)
        {
            foreach (var candidate in FindObjectsByType<FishingIntroController>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID))
            {
                if (candidate.gameObject.scene != gameObject.scene) continue;
                if (intro != null)
                {
                    Debug.LogError("Multiple fishing intros in this scene. Assign the audio Intro reference explicitly.", this);
                    intro = null;
                    break;
                }
                intro = candidate;
            }
        }
        if (intro != null)
        {
            intro.MovementStarted += HandleIntroMovementStarted;
            if (intro.HasStartedMovement) runStarted = true;
        }
        if (loop == null) return;
        previousState = loop.CurrentState;
        reelingActive = previousState == FishingLoopState.Reeling;
        gameOver = previousState == FishingLoopState.GameOver;
        loop.DistanceMultiplierLocked += HandleWaterLanding;
        loop.StateChanged += HandleStateChanged;
        loop.CatchCompleted += HandleCatch;
        loop.AttemptFailed += HandleFailure;
        loop.HookedFishReplaced += HandlePredation;
        loop.StrikeRejected += HandleStrikeRejected;
        // This scene also contains the menu; music starts when its interface appears.
        ResumeAmbient();
    }

    private void OnDisable()
    {
        if (intro != null) intro.MovementStarted -= HandleIntroMovementStarted;
        if (loop != null)
        {
            loop.DistanceMultiplierLocked -= HandleWaterLanding;
            loop.StateChanged -= HandleStateChanged;
            loop.CatchCompleted -= HandleCatch;
            loop.AttemptFailed -= HandleFailure;
            loop.HookedFishReplaced -= HandlePredation;
            loop.StrikeRejected -= HandleStrikeRejected;
        }

        if (gameplaySource != null) gameplaySource.Stop();
        StopVaried();
        if (ambientSource != null) ambientSource.Stop();
        ResetAmbientFade();
        StopReeling();
        if (musicSource != null) musicSource.Stop();
        ambientStarted = false;
        musicStarted = false;
        runStarted = false;
    }

    public void PlayUiSelect() => UiAudioRouter.Queue(UiButtonSound.Select, true);

    private void HandleIntroMovementStarted()
    {
        runStarted = true;
        ResumeAmbient();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Intro Wave requested: clip={(ambientLoop != null ? ambientLoop.name : "MISSING")}, " +
            $"soundEnabled={soundEnabled}, paused={paused}, gameOver={gameOver}, " +
            $"playing={(ambientSource != null && ambientSource.isPlaying)}, fade={ambientFadeInSeconds:F2}s, targetVolume={volume * ambientLevel:F3}", this);
#endif
    }
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
            StopVaried();
            PauseAmbient();
            if (musicSource != null) musicSource.Pause();
        }
        else ResumeAmbient();
    }

    private void HandleWaterLanding()
    {
        if (!gameOver) PlayVaried(waterLanding, waterLandingLevel);
    }

    private void HandleStateChanged(FishingLoopState state)
    {
        reelingActive = state == FishingLoopState.Reeling;
        if (state == FishingLoopState.GameOver)
        {
            if (gameOver) return;
            gameOver = true;
            if (gameplaySource != null) gameplaySource.Stop();
            StopVaried();
            if (ambientSource != null) ambientSource.Stop();
            ResetAmbientFade();
            if (musicSource != null) musicSource.Stop();
            ambientStarted = false;
            musicStarted = false;
            StopReeling();
            PlayGameplay(gameOverClip, gameOverLevel);
        }
        else if (!gameOver)
        {
            if (state == FishingLoopState.Casting && previousState != FishingLoopState.Casting)
            {
                runStarted = true;
                ResumeAmbient();
                PlayVaried(castLaunch, castLaunchLevel);
            }

            if (previousState == FishingLoopState.Striking && state == FishingLoopState.Reeling)
                PlayVaried(strikeSuccess, strikeSuccessLevel);
        }

        previousState = state;
    }

    private void HandleCatch(CatchResult result)
    {
        if (!settledAttempts.Add(result.AttemptId)) return;
        if (!gameOver) PlayGameplay(catchSuccess, catchSuccessLevel);
    }

    // Retried after every cooldown, so this is one of the most repeated sounds in the loop.
    private void HandleStrikeRejected()
    {
        if (!gameOver) PlayVaried(strikeRejected, strikeRejectedLevel);
    }

    // The coordinator only exchanges the catch once per attempt, so this needs no settling guard.
    private void HandlePredation()
    {
        if (!gameOver) PlayVaried(predationBite, predationBiteLevel);
    }

    private void HandleFailure(AttemptFailureResult result)
    {
        if (!settledAttempts.Add(result.AttemptId)) return;
        // The last-hook failure follows GameOver, which already played the session verdict.
        if (!gameOver) PlayGameplay(attemptFailure, attemptFailureLevel);
    }


    // Pitch is a property of the source rather than of the call, so a retuned one-shot would
    // also retune whatever is still ringing on the same source. Repeated sounds therefore get
    // their own small pool; verdict sounds stay on gameplaySource at an unaltered pitch so they
    // remain instantly recognisable.
    private void PlayVaried(AudioClip clip, float level)
    {
        if (!isActiveAndEnabled || !soundEnabled || paused || clip == null) return;
        EnsureSources();
        AudioSource source = TakeVariedSource();
        source.pitch = pitchVariation > 0f
            ? 1f + Random.Range(-pitchVariation, pitchVariation)
            : 1f;
        source.PlayOneShot(clip, level);
    }

    private AudioSource TakeVariedSource()
    {
        foreach (AudioSource candidate in variedSources)
            if (!candidate.isPlaying) return candidate;
        // Everything is busy, so reuse in order and accept retuning the longest-running one.
        AudioSource oldest = variedSources[nextVariedSource];
        nextVariedSource = (nextVariedSource + 1) % variedSources.Length;
        return oldest;
    }

    private void StopVaried()
    {
        if (variedSources == null) return;
        foreach (AudioSource source in variedSources)
            if (source != null) source.Stop();
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

    // Reconciled every frame so pausing, muting and game over all take effect without their own branches.
    private void UpdateReelingPlayback()
    {
        bool shouldPlay = reelingActive && soundEnabled && !paused && !gameOver && reelingLoop != null;
        float target = shouldPlay ? 1f : 0f;
        reelingFadeProgress = reelingFadeSeconds > 0f
            ? Mathf.MoveTowards(reelingFadeProgress, target, Time.unscaledDeltaTime / reelingFadeSeconds)
            : target;

        // Tension is the player's failure meter, so riding it with pitch turns the loop into a
        // readable warning instead of decoration. Smoothing keeps a jittery meter from warbling.
        float tensionPitch = Mathf.Lerp(reelingPitchMin, reelingPitchMax,
            loop != null ? loop.ReelingTension01 : 0f);
        reelingPitch = reelingPitchSmoothing > 0f
            ? Mathf.MoveTowards(reelingPitch, tensionPitch,
                Mathf.Abs(reelingPitchMax - reelingPitchMin) * Time.unscaledDeltaTime / reelingPitchSmoothing)
            : tensionPitch;

        if (shouldPlay)
        {
            EnsureSources();
            if (reelingSource.clip != reelingLoop)
            {
                reelingSource.clip = reelingLoop;
                reelingSource.time = 0f;
            }
            reelingSource.pitch = reelingPitch;
            // Pausing rather than stopping means the next retrieval resumes mid-clip instead of
            // replaying the same opening splash every time.
            if (!reelingSource.isPlaying) reelingSource.Play();
        }
        else if (reelingSource != null && reelingSource.isPlaying && reelingFadeProgress <= 0f)
            reelingSource.Pause();
    }

    private void StopReeling()
    {
        reelingActive = false;
        reelingFadeProgress = 0f;
        if (reelingSource == null) return;
        reelingSource.Stop();
        reelingSource.volume = 0f;
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
        if (reelingSource == null) reelingSource = CreateSource("ReelingAudio", true);
        if (variedSources == null)
        {
            variedSources = new AudioSource[VariedSourceCount];
            for (int i = 0; i < variedSources.Length; i++)
                variedSources[i] = CreateSource($"GameplayAudioVaried{i + 1}", false);
        }
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
        UpdateReelingPlayback();
        ApplyVolumes();
        SyncUiAudio();
    }

    private void SyncUiAudio() => UiAudioRouter.Configure(uiSelect, uiConfirm, uiBack, volume,
        uiSelectLevel, uiConfirmLevel, uiBackLevel, soundEnabled);

    private void ApplyVolumes()
    {
        if (gameplaySource == null || ambientSource == null || musicSource == null ||
            reelingSource == null || variedSources == null) return;
        gameplaySource.volume = volume;
        ambientSource.volume = volume * ambientLevel * ambientFadeProgress;
        musicSource.volume = volume * musicLevel;
        reelingSource.volume = volume * reelingLevel * reelingFadeProgress;
        foreach (AudioSource source in variedSources) source.volume = volume;
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
