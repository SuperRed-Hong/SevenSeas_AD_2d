using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

// 用像素美术在当前场景里搭出计时条和记分板，并把引用接到 SessionTimer / ScoreTracker。
// 只新建这两个节点，不改动场景里其他已布置的对象。
public sealed class FishingHudBuilderWindow : EditorWindow
{
    private const string UiPartsFolder = "Assets/Art Asset Folder/UI/UI Parts/";
    private const string TimerSpritePath =
        UiPartsFolder + "Fishing-UI_0011_Timer.png";
    private const string BoardDefaultSpritePath =
        UiPartsFolder + "Fishing-UI_0018_Score-Board-Default.png";
    private const string BoardPullOutSpritePath =
        UiPartsFolder + "Fishing-UI_0016_Score-Board-Pull-Out.png";
    private const string PixelFontPath =
        "Assets/Art Asset Folder/Fonts/PressStart2P/Press_Start_2P/PressStart2P-Regular SDF.asset";

    private const string TimerObjectName = "TimerBar";
    private const string ScoreBoardObjectName = "ScoreBoard";

    // 计时条原图 39x29：米色内腔 x[0,24) y[14,23)，怀表内圈 x[27,35) y[14,24)。
    // 收起时只留 x[24,39)：怀表连同读数完整可见，左边一丝米色暗示还能拉出。
    private const float TimerSpriteWidth = 39f;
    private const float TimerSpriteHeight = 29f;
    private const float TimerCollapsedWidth = 15f;
    private static readonly Rect TimerFillRect = new(0f, 14f, 24f, 9f);
    private static readonly Rect TimerDialRect = new(27f, 14f, 8f, 10f);

    // 记分板拉出图 61x22：米色文字区 x[20,59) y[3,21)；收起图 15x22。
    private const float BoardExpandedWidth = 61f;
    private const float BoardCollapsedWidth = 15f;
    private const float BoardHeight = 22f;
    private const float BoardTextRightPadding = 2f;
    private const float BoardTextWidth = 39f;
    private const float BoardTextBottomPadding = 3f;
    private const float BoardTextTopPadding = 1f;

    private static readonly Color Ink = new(0.23f, 0.16f, 0.10f);

    private Canvas targetCanvas;
    private Sprite timerSprite;
    private Sprite boardCollapsedSprite;
    private Sprite boardExpandedSprite;
    private TMP_FontAsset pixelFont;

    private float pixelScale = 10f;
    private float topMargin = 40f;
    private bool buildTimerBar = true;
    private bool buildScoreBoard = true;

    [MenuItem("Tools/Seven Seas/Build Fishing HUD (Timer + Score Board)")]
    private static void OpenWindow()
    {
        GetWindow<FishingHudBuilderWindow>("Fishing HUD Builder");
    }

    private void OnEnable()
    {
        timerSprite = LoadSprite(TimerSpritePath);
        boardCollapsedSprite = LoadSprite(BoardDefaultSpritePath);
        boardExpandedSprite = LoadSprite(BoardPullOutSpritePath);
        pixelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFontPath);
        targetCanvas = FindDefaultCanvas();
    }

    private void OnGUI()
    {
        GUILayout.Label("Fishing HUD Builder", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "在当前打开的场景里生成 TimerBar 和 ScoreBoard。\n" +
            "点击木条/卷轴可收放，分数变化时记分板会自动弹出再收回。\n" +
            "同名节点会被替换，其他对象不动。生成后需要手动保存场景。",
            MessageType.Info);

        targetCanvas = (Canvas)EditorGUILayout.ObjectField(
            new GUIContent("Target Canvas", "生成到哪个 Canvas 下，默认找 GamePlayCanvas。"),
            targetCanvas, typeof(Canvas), true);

        timerSprite = (Sprite)EditorGUILayout.ObjectField(
            new GUIContent("Timer Sprite", "计时条木框：Fishing-UI_0011_Timer。"),
            timerSprite, typeof(Sprite), false);

        boardCollapsedSprite = (Sprite)EditorGUILayout.ObjectField(
            new GUIContent("Board Collapsed", "记分板收起图：Score-Board-Default。"),
            boardCollapsedSprite, typeof(Sprite), false);

        boardExpandedSprite = (Sprite)EditorGUILayout.ObjectField(
            new GUIContent("Board Expanded", "记分板拉出图：Score-Board-Pull-Out。"),
            boardExpandedSprite, typeof(Sprite), false);

        pixelFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
            new GUIContent("Pixel Font", "HUD 数字使用的 TMP 字体，可留空用默认字体。"),
            pixelFont, typeof(TMP_FontAsset), false);

        EditorGUILayout.Space();

        pixelScale = EditorGUILayout.Slider(
            new GUIContent("Pixel Scale", "原图放大倍数，取整数倍才不会糊。"),
            pixelScale, 2f, 24f);
        pixelScale = Mathf.Round(pixelScale);

        topMargin = EditorGUILayout.FloatField(
            new GUIContent("Top Margin", "距画布顶端的距离，单位是 Canvas 参考分辨率像素。"),
            topMargin);

        buildTimerBar = EditorGUILayout.Toggle(
            new GUIContent("Build Timer Bar", "生成左上角计时条。"), buildTimerBar);
        buildScoreBoard = EditorGUILayout.Toggle(
            new GUIContent("Build Score Board", "生成右上角可拉出记分板。"), buildScoreBoard);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            "Timer Bar Size",
            $"{TimerSpriteWidth * pixelScale} x {TimerSpriteHeight * pixelScale}");
        EditorGUILayout.LabelField(
            "Score Board Size",
            $"{BoardCollapsedWidth * pixelScale} -> {BoardExpandedWidth * pixelScale}" +
            $" x {BoardHeight * pixelScale}");

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(targetCanvas == null))
        {
            if (GUILayout.Button("Build HUD"))
            {
                Build();
            }
        }

        if (targetCanvas == null)
        {
            EditorGUILayout.HelpBox(
                "当前场景里没找到 Canvas，请手动指定。", MessageType.Warning);
        }
    }

    private void Build()
    {
        SessionTimer sessionTimer =
            FindFirstObjectByType<SessionTimer>(FindObjectsInactive.Include);
        ScoreTracker scoreTracker =
            FindFirstObjectByType<ScoreTracker>(FindObjectsInactive.Include);

        Undo.SetCurrentGroupName("Build Fishing HUD");
        int undoGroup = Undo.GetCurrentGroup();

        GameObject firstCreated = null;

        if (buildTimerBar)
        {
            firstCreated = BuildTimerBar(sessionTimer);
        }

        if (buildScoreBoard)
        {
            GameObject board = BuildScoreBoard(scoreTracker);
            if (firstCreated == null)
            {
                firstCreated = board;
            }
        }

        Undo.CollapseUndoOperations(undoGroup);

        if (firstCreated != null)
        {
            Selection.activeGameObject = firstCreated;
            EditorSceneManager.MarkSceneDirty(firstCreated.scene);
        }

        if (sessionTimer == null || scoreTracker == null)
        {
            Debug.LogWarning(
                "Fishing HUD Builder：场景里没找到 SessionTimer 或 ScoreTracker，" +
                "对应引用需要手动拖进 Inspector。");
        }

        // 点击收放走 EventSystem，缺了它 HUD 会照常显示但点不动。
        bool hasEventSystem = FindFirstObjectByType<EventSystem>(
            FindObjectsInactive.Include) != null;
        bool hasRaycaster = targetCanvas.GetComponent<GraphicRaycaster>() != null;
        if (!hasEventSystem || !hasRaycaster)
        {
            Debug.LogWarning(
                "Fishing HUD Builder：场景缺少 EventSystem 或 Canvas 上没有 " +
                "GraphicRaycaster，点击收放不会生效。");
        }
    }

    private GameObject BuildTimerBar(SessionTimer sessionTimer)
    {
        ReplaceExisting(TimerObjectName);

        float scale = pixelScale;

        RectTransform root = CreateRect(TimerObjectName, targetCanvas.transform);
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.sizeDelta = new Vector2(
            TimerSpriteWidth * scale, TimerSpriteHeight * scale);
        root.anchoredPosition = new Vector2(0f, -topMargin);

        // 遮罩裁掉滑出左缘的木条，同时把点击限制在露出的怀表上。
        root.gameObject.AddComponent<RectMask2D>();

        RectTransform bar = CreateRect("Bar", root);
        bar.anchorMin = new Vector2(0f, 0f);
        bar.anchorMax = new Vector2(0f, 1f);
        bar.pivot = new Vector2(0f, 0.5f);
        bar.sizeDelta = new Vector2(TimerSpriteWidth * scale, 0f);
        bar.anchoredPosition = Vector2.zero;

        Image frame = bar.gameObject.AddComponent<Image>();
        frame.sprite = timerSprite;
        // 木条本身就是点击热区：收起时只有怀表那一截可点。
        frame.raycastTarget = true;

        // 填充区按原图内腔定位，锚点固定在木框左下角。
        RectTransform fillArea = CreateRect("FillArea", bar);
        fillArea.anchorMin = Vector2.zero;
        fillArea.anchorMax = Vector2.zero;
        fillArea.pivot = Vector2.zero;
        fillArea.anchoredPosition = new Vector2(
            TimerFillRect.x * scale, TimerFillRect.y * scale);
        fillArea.sizeDelta = new Vector2(
            TimerFillRect.width * scale, TimerFillRect.height * scale);

        RectTransform fill = CreateRect("Fill", fillArea);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;

        Image fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = new Color(0.62f, 0.40f, 0.18f);
        fillImage.raycastTarget = false;

        RectTransform dialText = CreateRect("RemainingText", bar);
        dialText.anchorMin = Vector2.zero;
        dialText.anchorMax = Vector2.zero;
        dialText.pivot = Vector2.zero;
        dialText.anchoredPosition = new Vector2(
            TimerDialRect.x * scale, TimerDialRect.y * scale);
        dialText.sizeDelta = new Vector2(
            TimerDialRect.width * scale, TimerDialRect.height * scale);

        TextMeshProUGUI remainingText = CreateText(
            dialText,
            "90",
            Mathf.RoundToInt(TimerDialRect.height * scale * 0.55f));

        // 计时条只有一张图，收起完全靠位移，所以不填 collapsedSprite。
        // 读数要在收起时也看得见，因此不进 fadeWithOpen。
        PullOutPanel panel = root.gameObject.AddComponent<PullOutPanel>();
        var panelSerialized = new SerializedObject(panel);
        panelSerialized.FindProperty("panelRect").objectReferenceValue = bar;
        panelSerialized.FindProperty("panelImage").objectReferenceValue = frame;
        panelSerialized.FindProperty("anchorEdge").enumValueIndex =
            (int)PullOutPanel.AnchorEdge.Left;
        panelSerialized.FindProperty("collapsedWidth").floatValue =
            TimerCollapsedWidth * scale;
        panelSerialized.FindProperty("expandedWidth").floatValue =
            TimerSpriteWidth * scale;
        panelSerialized.FindProperty("expandedSprite").objectReferenceValue =
            timerSprite;
        panelSerialized.FindProperty("startExpanded").boolValue = true;
        panelSerialized.ApplyModifiedPropertiesWithoutUndo();

        TimerBarHUD hud = root.gameObject.AddComponent<TimerBarHUD>();
        var serialized = new SerializedObject(hud);
        serialized.FindProperty("sessionTimer").objectReferenceValue = sessionTimer;
        serialized.FindProperty("fillRect").objectReferenceValue = fill;
        serialized.FindProperty("fillImage").objectReferenceValue = fillImage;
        serialized.FindProperty("remainingText").objectReferenceValue = remainingText;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        Undo.RegisterCreatedObjectUndo(root.gameObject, "Create Timer Bar");
        return root.gameObject;
    }

    private GameObject BuildScoreBoard(ScoreTracker scoreTracker)
    {
        ReplaceExisting(ScoreBoardObjectName);

        float scale = pixelScale;

        RectTransform root = CreateRect(ScoreBoardObjectName, targetCanvas.transform);
        root.anchorMin = new Vector2(1f, 1f);
        root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(1f, 1f);
        root.sizeDelta = new Vector2(
            BoardExpandedWidth * scale, BoardHeight * scale);
        root.anchoredPosition = new Vector2(0f, -topMargin);

        // 遮罩裁掉被推出屏幕右缘的部分，收起时才只剩那截小标签。
        root.gameObject.AddComponent<RectMask2D>();

        RectTransform board = CreateRect("Board", root);
        board.anchorMin = new Vector2(1f, 0f);
        board.anchorMax = new Vector2(1f, 1f);
        board.pivot = new Vector2(1f, 0.5f);
        board.sizeDelta = new Vector2(BoardCollapsedWidth * scale, 0f);
        board.anchoredPosition = Vector2.zero;

        Image boardImage = board.gameObject.AddComponent<Image>();
        boardImage.sprite = boardCollapsedSprite;
        // 卷轴本身就是点击热区：收起时只有那截标签可点。
        boardImage.raycastTarget = true;

        RectTransform textRect = CreateRect("ScoreText", board);
        textRect.anchorMin = new Vector2(1f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(1f, 0.5f);
        textRect.sizeDelta = new Vector2(
            BoardTextWidth * scale,
            -(BoardTextBottomPadding + BoardTextTopPadding) * scale);
        textRect.anchoredPosition = new Vector2(
            -BoardTextRightPadding * scale,
            (BoardTextBottomPadding - BoardTextTopPadding) * 0.5f * scale);

        TextMeshProUGUI scoreText = CreateText(
            textRect,
            "SCORE 0",
            Mathf.RoundToInt(BoardHeight * scale * 0.32f));

        CanvasGroup textGroup = textRect.gameObject.AddComponent<CanvasGroup>();
        textGroup.alpha = 0f;

        PullOutPanel panel = root.gameObject.AddComponent<PullOutPanel>();
        var panelSerialized = new SerializedObject(panel);
        panelSerialized.FindProperty("panelRect").objectReferenceValue = board;
        panelSerialized.FindProperty("panelImage").objectReferenceValue = boardImage;
        panelSerialized.FindProperty("anchorEdge").enumValueIndex =
            (int)PullOutPanel.AnchorEdge.Right;
        panelSerialized.FindProperty("collapsedWidth").floatValue =
            BoardCollapsedWidth * scale;
        panelSerialized.FindProperty("expandedWidth").floatValue =
            BoardExpandedWidth * scale;
        panelSerialized.FindProperty("collapsedSprite").objectReferenceValue =
            boardCollapsedSprite;
        panelSerialized.FindProperty("expandedSprite").objectReferenceValue =
            boardExpandedSprite;
        panelSerialized.FindProperty("startExpanded").boolValue = false;

        SerializedProperty fadeGroups = panelSerialized.FindProperty("fadeWithOpen");
        fadeGroups.arraySize = 1;
        fadeGroups.GetArrayElementAtIndex(0).objectReferenceValue = textGroup;
        panelSerialized.ApplyModifiedPropertiesWithoutUndo();

        ScoreBoardHUD hud = root.gameObject.AddComponent<ScoreBoardHUD>();
        var serialized = new SerializedObject(hud);
        serialized.FindProperty("scoreTracker").objectReferenceValue = scoreTracker;
        serialized.FindProperty("pullOutPanel").objectReferenceValue = panel;
        serialized.FindProperty("scoreText").objectReferenceValue = scoreText;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        Undo.RegisterCreatedObjectUndo(root.gameObject, "Create Score Board");
        return root.gameObject;
    }

    private void ReplaceExisting(string objectName)
    {
        Transform existing = targetCanvas.transform.Find(objectName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing.gameObject);
        }
    }

    private TextMeshProUGUI CreateText(
        RectTransform rect, string content, int fontSize)
    {
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (pixelFont != null)
        {
            text.font = pixelFont;
        }

        text.text = content;
        text.color = Ink;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(6f, fontSize * 0.4f);
        text.fontSizeMax = fontSize;
        text.fontSize = fontSize;
        return text;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform))
            .GetComponent<RectTransform>();
        rect.gameObject.layer = parent.gameObject.layer;
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        return rect;
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .FirstOrDefault();
    }

    private static Canvas FindDefaultCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        return canvases.FirstOrDefault(c => c.name == "GamePlayCanvas")
            ?? canvases.FirstOrDefault();
    }
}
