using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GyroscopeCastTestSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/GyroscopeCastTest.unity";
    private const string ProfilePath = "Assets/Settings/CastTuningProfile.asset";
    private const string CatalogPath = "Assets/Scripts/SceneManagement/SceneCatalog.asset";

    private static readonly Color BackgroundColor = new Color(0.055f, 0.075f, 0.08f, 1f);
    private static readonly Color PanelColor = new Color(0.035f, 0.045f, 0.055f, 0.94f);
    private static readonly Color TrackColor = new Color(1f, 1f, 1f, 0.09f);
    private static readonly Color TextColor = new Color(0.92f, 0.95f, 0.96f, 1f);
    private static readonly Color MutedTextColor = new Color(0.62f, 0.69f, 0.71f, 1f);
    private static readonly Color AccentColor = new Color(0.1f, 0.78f, 0.68f, 1f);
    private static readonly Color XColor = new Color(1f, 0.3f, 0.3f, 1f);
    private static readonly Color YColor = new Color(0.3f, 0.88f, 0.42f, 1f);
    private static readonly Color ZColor = new Color(0.3f, 0.58f, 1f, 1f);

    [MenuItem("Seven Seas/Create Gyroscope Cast Test Scene")]
    public static void CreateScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CastTuningProfile profile = GetOrCreateProfile();

        Camera camera = CreateCamera();
        GameObject ballObject = CreateBall(profile);
        CreateGround();

        CastCameraFollow cameraFollow = camera.gameObject.AddComponent<CastCameraFollow>();
        SetObjectReference(cameraFollow, "target", ballObject.transform);

        GameObject sensorRoot = new GameObject("CastMotionSystem");
        GyroscopeReader reader = sensorRoot.AddComponent<GyroscopeReader>();
        CastGestureDetector detector = sensorRoot.AddComponent<CastGestureDetector>();
        detector.Configure(reader, profile);
        EditorUtility.SetDirty(detector);

        CastBallController ball = ballObject.GetComponent<CastBallController>();
        CastTestController testController = sensorRoot.AddComponent<CastTestController>();
        SetObjectReference(testController, "detector", detector);
        SetObjectReference(testController, "ball", ball);
        SetObjectReference(testController, "cameraFollow", cameraFollow);

        CreateHud(reader, detector, ball, testController);
        CreateEventSystem();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        AddSceneToCatalog();
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = sensorRoot;

        Debug.Log("Gyroscope cast test scene created and registered.");
    }

    private static CastTuningProfile GetOrCreateProfile()
    {
        CastTuningProfile profile = AssetDatabase.LoadAssetAtPath<CastTuningProfile>(ProfilePath);

        if (profile != null)
        {
            return profile;
        }

        if (AssetDatabase.AssetPathExists(ProfilePath))
        {
            throw new UnityException("Existing CastTuningProfile could not be loaded.");
        }

        profile = ScriptableObject.CreateInstance<CastTuningProfile>();
        AssetDatabase.CreateAsset(profile, ProfilePath);
        AssetDatabase.SaveAssets();
        return profile;
    }

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = BackgroundColor;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        return camera;
    }

    private static GameObject CreateBall(CastTuningProfile profile)
    {
        GameObject ballObject = new GameObject(
            "CastBall",
            typeof(SpriteRenderer),
            typeof(Rigidbody2D),
            typeof(CircleCollider2D),
            typeof(CastBallController));
        ballObject.transform.position = new Vector3(-2.5f, -3.65f, 0f);
        ballObject.transform.localScale = Vector3.one * 0.85f;

        SpriteRenderer renderer = ballObject.GetComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        renderer.color = AccentColor;
        renderer.sortingOrder = 5;

        Rigidbody2D body = ballObject.GetComponent<Rigidbody2D>();
        body.gravityScale = 1f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        CircleCollider2D collider = ballObject.GetComponent<CircleCollider2D>();
        collider.radius = 0.5f;

        CastBallController controller = ballObject.GetComponent<CastBallController>();
        controller.Configure(profile);
        EditorUtility.SetDirty(controller);
        return ballObject;
    }

    private static void CreateGround()
    {
        GameObject ground = new GameObject(
            "Ground",
            typeof(SpriteRenderer),
            typeof(BoxCollider2D));
        ground.transform.position = new Vector3(45f, -4.35f, 0f);
        ground.transform.localScale = new Vector3(100f, 0.35f, 1f);

        SpriteRenderer renderer = ground.GetComponent<SpriteRenderer>();
        renderer.sprite = LoadProjectSprite("Assets/Square.png");
        renderer.color = new Color(0.18f, 0.28f, 0.29f, 1f);
        renderer.sortingOrder = 1;

        BoxCollider2D collider = ground.GetComponent<BoxCollider2D>();
        collider.size = Vector2.one;

        for (int distance = 0; distance <= 50; distance += 5)
        {
            GameObject marker = new GameObject("DistanceMarker_" + distance, typeof(SpriteRenderer));
            marker.transform.SetParent(ground.transform, false);
            marker.transform.localPosition = new Vector3((distance - 45f) / 100f, 1.2f, 0f);
            marker.transform.localScale = new Vector3(0.015f, 2.4f, 1f);
            SpriteRenderer markerRenderer = marker.GetComponent<SpriteRenderer>();
            markerRenderer.sprite = LoadProjectSprite("Assets/Square.png");
            markerRenderer.color = new Color(0.46f, 0.62f, 0.62f, 0.65f);
            markerRenderer.sortingOrder = 2;
        }
    }

    private static void CreateHud(
        GyroscopeReader reader,
        CastGestureDetector detector,
        CastBallController ball,
        CastTestController testController)
    {
        GameObject canvasObject = new GameObject(
            "CastDebugHUD",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform header = CreateImage(
            "Header",
            canvasObject.transform,
            PanelColor,
            new Vector2(0.03f, 0.835f),
            new Vector2(0.97f, 0.97f));
        CreateText(
            "Title",
            header,
            "CAST MOTION TEST",
            34f,
            FontStyles.Bold,
            TextAlignmentOptions.Left,
            new Vector2(0.04f, 0.6f),
            new Vector2(0.58f, 0.92f),
            TextColor);
        TMP_Text sensorText = CreateText(
            "Sensor",
            header,
            "GYRO OFFLINE",
            20f,
            FontStyles.Normal,
            TextAlignmentOptions.Right,
            new Vector2(0.5f, 0.62f),
            new Vector2(0.96f, 0.9f),
            MutedTextColor);
        TMP_Text stateText = CreateText(
            "State",
            header,
            "STATE  UNAVAILABLE",
            25f,
            FontStyles.Bold,
            TextAlignmentOptions.Left,
            new Vector2(0.04f, 0.12f),
            new Vector2(0.56f, 0.48f),
            AccentColor);
        TMP_Text axisText = CreateText(
            "Axis",
            header,
            "CAST AXIS  X",
            23f,
            FontStyles.Bold,
            TextAlignmentOptions.Right,
            new Vector2(0.54f, 0.12f),
            new Vector2(0.96f, 0.48f),
            TextColor);

        RectTransform values = CreateImage(
            "Values",
            canvasObject.transform,
            PanelColor,
            new Vector2(0.03f, 0.64f),
            new Vector2(0.97f, 0.82f));
        TMP_Text rawVelocityText = CreateValue(values, "RawVelocity", "RAW  0.00 rad/s", 0.7f, true);
        TMP_Text filteredVelocityText = CreateValue(values, "FilteredVelocity", "FILTERED  0.00 rad/s", 0.7f, false);
        TMP_Text rawPeakText = CreateValue(values, "RawPeak", "RAW PEAK  0.00", 0.38f, true);
        TMP_Text filteredPeakText = CreateValue(values, "FilteredPeak", "FILTERED PEAK  0.00", 0.38f, false);
        TMP_Text powerText = CreateValue(values, "Power", "CAST POWER  0%", 0.06f, true);
        TMP_Text velocityText = CreateValue(values, "Velocity", "BALL VELOCITY  0.00, 0.00", 0.06f, false);

        RectTransform axisPanel = CreateImage(
            "AxisPanel",
            canvasObject.transform,
            PanelColor,
            new Vector2(0.03f, 0.49f),
            new Vector2(0.97f, 0.625f));
        RectTransform xBar = CreateAxisRow("X", axisPanel, 0.72f, XColor);
        RectTransform yBar = CreateAxisRow("Y", axisPanel, 0.42f, YColor);
        RectTransform zBar = CreateAxisRow("Z", axisPanel, 0.12f, ZColor);

        RectTransform graphPanel = CreateImage(
            "GraphPanel",
            canvasObject.transform,
            new Color(0.02f, 0.027f, 0.033f, 0.92f),
            new Vector2(0.03f, 0.265f),
            new Vector2(0.97f, 0.475f));
        RectTransform graphRect = CreateRect(
            "Graph",
            graphPanel,
            new Vector2(0.035f, 0.12f),
            new Vector2(0.965f, 0.94f));
        GyroscopeHistoryGraphic history = graphRect.gameObject.AddComponent<GyroscopeHistoryGraphic>();
        SetObjectReference(history, "reader", reader);
        SetFloat(history, "displayRange", 8f);
        CreateText(
            "GraphLabel",
            graphPanel,
            "ANGULAR VELOCITY  |  X / Y / Z  |  3 s",
            17f,
            FontStyles.Normal,
            TextAlignmentOptions.Left,
            new Vector2(0.035f, 0.01f),
            new Vector2(0.965f, 0.11f),
            MutedTextColor);

        TMP_Text distanceText = CreateText(
            "Distance",
            canvasObject.transform,
            "DISTANCE  0.00 m",
            34f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.16f),
            new Vector2(0.92f, 0.23f),
            TextColor);

        Button menuButton = CreateButton(
            "MainMenuButton",
            canvasObject.transform,
            "MAIN MENU",
            new Vector2(0.03f, 0.035f),
            new Vector2(0.46f, 0.105f),
            new Color(0.16f, 0.2f, 0.22f, 0.98f));
        SceneNavigationButton navigation = menuButton.gameObject.AddComponent<SceneNavigationButton>();
        SetEnum(navigation, "targetScene", (int)E_SceneID.MainMenu);
        UnityEventTools.AddPersistentListener(menuButton.onClick, navigation.Navigate);

        Button restartButton = CreateButton(
            "RestartButton",
            canvasObject.transform,
            "RESTART",
            new Vector2(0.54f, 0.035f),
            new Vector2(0.97f, 0.105f),
            AccentColor);

        CastDebugHUD hud = canvasObject.AddComponent<CastDebugHUD>();
        SetObjectReference(hud, "reader", reader);
        SetObjectReference(hud, "detector", detector);
        SetObjectReference(hud, "ball", ball);
        SetObjectReference(hud, "testController", testController);
        SetObjectReference(hud, "sensorText", sensorText);
        SetObjectReference(hud, "stateText", stateText);
        SetObjectReference(hud, "axisText", axisText);
        SetObjectReference(hud, "rawVelocityText", rawVelocityText);
        SetObjectReference(hud, "filteredVelocityText", filteredVelocityText);
        SetObjectReference(hud, "rawPeakText", rawPeakText);
        SetObjectReference(hud, "filteredPeakText", filteredPeakText);
        SetObjectReference(hud, "powerText", powerText);
        SetObjectReference(hud, "velocityText", velocityText);
        SetObjectReference(hud, "distanceText", distanceText);
        SetObjectReference(hud, "xBar", xBar);
        SetObjectReference(hud, "yBar", yBar);
        SetObjectReference(hud, "zBar", zBar);
        SetFloat(hud, "barRange", 8f);
        UnityEventTools.AddPersistentListener(restartButton.onClick, hud.RestartTest);
    }

    private static TMP_Text CreateValue(
        Transform parent,
        string name,
        string value,
        float centerY,
        bool left)
    {
        return CreateText(
            name,
            parent,
            value,
            21f,
            FontStyles.Bold,
            left ? TextAlignmentOptions.Left : TextAlignmentOptions.Right,
            new Vector2(left ? 0.04f : 0.5f, centerY),
            new Vector2(left ? 0.5f : 0.96f, centerY + 0.24f),
            TextColor);
    }

    private static RectTransform CreateAxisRow(
        string axis,
        Transform parent,
        float centerY,
        Color color)
    {
        CreateText(
            axis + "Label",
            parent,
            axis,
            21f,
            FontStyles.Bold,
            TextAlignmentOptions.Left,
            new Vector2(0.04f, centerY),
            new Vector2(0.12f, centerY + 0.2f),
            color);
        RectTransform track = CreateImage(
            axis + "Track",
            parent,
            TrackColor,
            new Vector2(0.13f, centerY + 0.035f),
            new Vector2(0.96f, centerY + 0.16f));
        RectTransform fill = CreateImage(
            axis + "Fill",
            track,
            color,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 1f));
        CreateImage(
            axis + "Zero",
            track,
            new Color(1f, 1f, 1f, 0.5f),
            new Vector2(0.498f, 0f),
            new Vector2(0.502f, 1f));
        return fill;
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color color)
    {
        RectTransform rect = CreateImage(name, parent, color, anchorMin, anchorMax);
        Image image = rect.GetComponent<Image>();
        image.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        CreateText(
            "Label",
            rect,
            label,
            24f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            Vector2.zero,
            Vector2.one,
            name == "RestartButton" ? new Color(0.02f, 0.08f, 0.075f, 1f) : TextColor);
        return button;
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
        eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    private static RectTransform CreateRect(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static RectTransform CreateImage(
        string name,
        Transform parent,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        string value,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color color)
    {
        RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static Sprite LoadProjectSprite(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    }

    private static void AddSceneToBuildSettings()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

        if (scenes.Any(scene => scene.path == ScenePath))
        {
            return;
        }

        EditorBuildSettings.scenes = scenes
            .Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) })
            .ToArray();
    }

    private static void AddSceneToCatalog()
    {
        SceneCatalog catalog = AssetDatabase.LoadAssetAtPath<SceneCatalog>(CatalogPath);

        if (catalog == null)
        {
            Debug.LogError("SceneCatalog asset was not found.");
            return;
        }

        SerializedObject serializedCatalog = new SerializedObject(catalog);
        SerializedProperty entries = serializedCatalog.FindProperty("sceneEntries");

        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);

            if (entry.FindPropertyRelative("id").enumValueIndex != (int)E_SceneID.GyroscopeTest)
            {
                continue;
            }

            entry.FindPropertyRelative("sceneName").stringValue = "GyroscopeCastTest";
            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return;
        }

        int index = entries.arraySize;
        entries.InsertArrayElementAtIndex(index);
        SerializedProperty newEntry = entries.GetArrayElementAtIndex(index);
        newEntry.FindPropertyRelative("id").enumValueIndex = (int)E_SceneID.GyroscopeTest;
        newEntry.FindPropertyRelative("sceneName").stringValue = "GyroscopeCastTest";
        serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
    }

    private static void SetObjectReference(Object target, string propertyName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Object target, string propertyName, float value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).floatValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetEnum(Object target, string propertyName, int value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).enumValueIndex = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}
