using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

// Runs after map selection, before any spawner Start or the first rendered menu frame.
[DefaultExecutionOrder(-750)]
public sealed class FishingIntroController : MonoBehaviour
{
    [SerializeField] private FishingIntroProfile profile;
    [SerializeField] private FishingMapSelector maps;
    [SerializeField] private FishingCameraController cameras;
    [SerializeField] private Transform waterVisual;
    [SerializeField] private BoxCollider2D fishMovementArea;
    [SerializeField] private FishSpawner[] fishSpawners;
    [SerializeField] private Transform shorePlayer;
    [SerializeField] private TMP_FontAsset uiFont;

    private GameObject scenery;
    private GameObject overlay;
    private RectTransform topBar, bottomBar, safeArea;
    private Button skipButton;
    private CinemachineCamera menu;
    private CinemachineCamera shot;
    private Vector3 originalMenuPosition;
    private Quaternion originalMenuRotation;
    private LensSettings originalMenuLens;
    private Vector3 surveyTop, platformPose;
    private bool playing, completed, skipRequested;
    private float bars;
    public bool IsPrepared { get; private set; }
    public bool IsPlaying => playing;
    public bool HasCompleted => completed;
    public bool HasStartedMovement { get; private set; }
    public event System.Action MovementStarted;
    public GameObject MenuMap { get; private set; }

    private void Awake()
    {
        if (profile == null || maps == null || cameras == null || cameras.MenuCamera == null ||
            cameras.OverviewCamera == null || cameras.IntroCamera == null || waterVisual == null || fishMovementArea == null ||
            shorePlayer == null || fishSpawners == null || fishSpawners.Length == 0)
        {
            Debug.LogError("Fishing intro requires profile, cameras, maps, water, fish and shore references.", this);
            return;
        }
        GameObject source = maps.ChooseMenuMap();
        if (source == null) return;
        menu = cameras.MenuCamera;
        shot = cameras.IntroCamera;
        originalMenuPosition = menu.transform.position;
        originalMenuRotation = menu.transform.rotation;
        originalMenuLens = menu.Lens;
        Physics2D.SyncTransforms();
        Bounds area = fishMovementArea.bounds;
        Bounds sceneryBounds = area;
        EncapsulateMap(ref sceneryBounds, source);
        EncapsulateMap(ref sceneryBounds, maps.SelectedMap);
        float offsetY = Mathf.Max(profile.menuVerticalOffset,
            sceneryBounds.size.y + 2f * originalMenuLens.OrthographicSize + profile.sceneryGap);
        Vector3 offset = Vector3.up * offsetY;

        scenery = new GameObject("MenuScenery (runtime)");
        scenery.transform.SetParent(transform, false);
        // Only copy the map and water, never the landscape root containing the dock/player.
        MenuMap = Instantiate(source, scenery.transform, true);
        MenuMap.name = source.name + " Menu";
        MenuMap.transform.position = source.transform.position + offset;
        MenuMap.SetActive(true);
        foreach (var tilemapCollider in MenuMap.GetComponentsInChildren<TilemapCollider2D>())
            tilemapCollider.ProcessTilemapChanges();

        // Extend the existing water material through the transit corridor. This is visual only.
        Transform water = Instantiate(waterVisual, scenery.transform, true);
        water.name = "Menu and transit water";
        water.position = waterVisual.position + offset * 0.5f + Vector3.forward * 0.02f;
        foreach (var collider in water.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (var collider in water.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
        var waterRenderer = water.GetComponentInChildren<Renderer>();
        if (waterRenderer != null && waterRenderer.bounds.size.y > 0.001f)
        {
            // Scale along world Y through a non-rotated parent, preserving the shader/mesh orientation.
            var waterStretch = new GameObject("Water extent").transform;
            waterStretch.SetParent(scenery.transform, false);
            waterStretch.position = water.position;
            water.SetParent(waterStretch, true);
            float requiredHeight = offsetY + sceneryBounds.size.y + 4f * originalMenuLens.OrthographicSize;
            waterStretch.localScale = new Vector3(1f, Mathf.Max(1f, requiredHeight / waterRenderer.bounds.size.y), 1f);
        }

        var boundaryRoot = new GameObject("Menu fish movement area");
        boundaryRoot.transform.SetParent(scenery.transform, false);
        boundaryRoot.transform.SetPositionAndRotation(fishMovementArea.transform.position + offset,
            fishMovementArea.transform.rotation);
        boundaryRoot.transform.localScale = fishMovementArea.transform.lossyScale;
        var boundary = boundaryRoot.AddComponent<BoxCollider2D>();
        boundary.size = fishMovementArea.size;
        boundary.offset = fishMovementArea.offset;
        boundary.isTrigger = true;
        Physics2D.SyncTransforms();
        foreach (var spawner in fishSpawners)
            if (spawner != null) spawner.CreateAmbientCopy(scenery.transform, offset, boundary);

        menu.transform.position = originalMenuPosition + offset;
        menu.PreviousStateIsValid = false;
        cameras.ShowMenu();
        float visibleHalfHeight = profile.surveyOrthographicSize * (1f - 2f * profile.barHeight);
        surveyTop = new Vector3(area.center.x, area.max.y - visibleHalfHeight, originalMenuPosition.z);
        platformPose = new Vector3(shorePlayer.position.x,
            shorePlayer.position.y + visibleHalfHeight * 0.45f, originalMenuPosition.z);
        BuildOverlay();
        IsPrepared = true;
    }

    public void Skip()
    {
        if (playing && !completed) skipRequested = true;
    }

    public IEnumerator Play()
    {
        if (!IsPrepared || playing || completed) yield break;
        playing = true;
        skipRequested = false;
        overlay.SetActive(true);
        skipButton.interactable = true;
        yield return cameras.BeginIntro();
        HasStartedMovement = true;
        MovementStarted?.Invoke();
        yield return MoveTo(surveyTop, profile.surveyOrthographicSize, profile.entrySeconds, false);
        if (!skipRequested)
        {
            yield return AnimateBars(profile.barHeight, profile.barSeconds, true);
            yield return SurveyTo(platformPose);
        }
        float held = 0f;
        while (!skipRequested && held < profile.platformHoldSeconds)
        {
            held += Time.unscaledDeltaTime;
            yield return null;
        }
        skipButton.interactable = false;
        // Both normal completion and skip reach the same exact gameplay pose before releasing entry.
        float duration = skipRequested ? profile.skipSeconds : profile.handoffSeconds;
        yield return MoveTo(cameras.OverviewCamera.transform.position,
            cameras.OverviewCamera.Lens.OrthographicSize, duration, true);
        shot.transform.rotation = cameras.OverviewCamera.transform.rotation;
        shot.Lens = cameras.OverviewCamera.Lens;
        cameras.CompleteIntro();
        // Keep the outgoing camera at the handoff pose throughout any Brain blend.
        yield return null;
        FinishPresentation();
        completed = true;
        playing = false;
    }

    private IEnumerator SurveyTo(Vector3 target)
    {
        // Constant world speed: map length determines duration, without a faster middle section.
        while (shot.transform.position != target)
        {
            if (skipRequested) yield break;
            float speed = Mathf.Max(0.01f, profile.surveySpeed);
            shot.transform.position = Vector3.MoveTowards(shot.transform.position, target,
                speed * Time.unscaledDeltaTime);
            yield return null;
        }
        shot.transform.position = target;
    }

    private IEnumerator MoveTo(Vector3 target, float size, float seconds, bool handoff)
    {
        Vector3 start = shot.transform.position;
        float startSize = shot.Lens.OrthographicSize;
        float startBars = bars;
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            if (!handoff && skipRequested) yield break;
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / seconds));
            shot.transform.position = Vector3.Lerp(start, target, t);
            var lens = shot.Lens;
            lens.OrthographicSize = Mathf.Lerp(startSize, size, t);
            shot.Lens = lens;
            if (handoff) SetBars(Mathf.Lerp(startBars, 0f, t));
            yield return null;
        }
        shot.transform.position = target;
        var finalLens = shot.Lens;
        finalLens.OrthographicSize = size;
        shot.Lens = finalLens;
        if (handoff) SetBars(0f);
    }

    private IEnumerator AnimateBars(float target, float seconds, bool interruptible)
    {
        float start = bars;
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            if (interruptible && skipRequested) yield break;
            elapsed += Time.unscaledDeltaTime;
            SetBars(Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / seconds))));
            yield return null;
        }
        SetBars(target);
    }

    private void FinishPresentation()
    {
        SetBars(0f);
        if (overlay != null) overlay.SetActive(false);
        // Inactive menu fish cannot be collected by the gameplay ecology's global queries.
        if (scenery != null) scenery.SetActive(false);
    }

    private void OnDisable()
    {
        if (!playing) return;
        StopAllCoroutines();
        cameras.CompleteIntro();
        FinishPresentation();
        playing = false;
        // Cancellation is not completion and must not start the session.
    }

    private void OnDestroy()
    {
        if (overlay != null) Destroy(overlay);
        if (scenery != null) Destroy(scenery);
    }

    private void BuildOverlay()
    {
        overlay = new GameObject("Intro overlay (runtime)", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        overlay.transform.SetParent(transform, false);
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1440f, 2304f);
        scaler.matchWidthOrHeight = 0.5f;
        topBar = MakeRect("Top bar", overlay.transform, Vector2.up, Vector2.one);
        bottomBar = MakeRect("Bottom bar", overlay.transform, Vector2.zero, Vector2.right);
        topBar.gameObject.AddComponent<Image>().color = Color.black;
        bottomBar.gameObject.AddComponent<Image>().color = Color.black;
        topBar.GetComponent<Image>().raycastTarget = false;
        bottomBar.GetComponent<Image>().raycastTarget = false;
        safeArea = MakeRect("Safe area", overlay.transform, Vector2.zero, Vector2.one);
        var buttonRect = MakeRect("Skip", safeArea, Vector2.one, Vector2.one);
        buttonRect.pivot = Vector2.one;
        buttonRect.anchoredPosition = new Vector2(-32f, -32f);
        buttonRect.sizeDelta = new Vector2(260f, 90f);
        var background = buttonRect.gameObject.AddComponent<Image>();
        background.color = new Color(0.05f, 0.09f, 0.12f, 0.9f);
        skipButton = buttonRect.gameObject.AddComponent<Button>();
        skipButton.targetGraphic = background;
        skipButton.onClick.AddListener(Skip);
        var labelRect = MakeRect("Label", buttonRect, Vector2.zero, Vector2.one);
        var label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        if (uiFont != null) label.font = uiFont;
        label.text = "SKIP >";
        label.fontSize = 30f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        SetBars(0f);
        overlay.SetActive(false);
    }

    private static void EncapsulateMap(ref Bounds bounds, GameObject map)
    {
        // Renderer.bounds is not reliable for inactive menu candidates before their first update.
        foreach (var tilemap in map.GetComponentsInChildren<Tilemap>(true))
        {
            Bounds local = tilemap.localBounds;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
                bounds.Encapsulate(tilemap.transform.TransformPoint(local.center +
                    Vector3.Scale(local.extents, new Vector3(x, y, 1f))));
        }
    }

    private static RectTransform MakeRect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private void SetBars(float height)
    {
        bars = height;
        if (topBar == null) return;
        topBar.anchorMin = new Vector2(0f, 1f - height);
        bottomBar.anchorMax = new Vector2(1f, height);
    }

    private void LateUpdate()
    {
        if (!playing || safeArea == null || Screen.width <= 0 || Screen.height <= 0) return;
        Rect safe = Screen.safeArea;
        safeArea.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        safeArea.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
    }
}
