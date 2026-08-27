using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GyroscopeDebugSceneSetup
{
    private static readonly Color PanelColor = new Color(0.035f, 0.045f, 0.06f, 0.94f);
    private static readonly Color TrackColor = new Color(1f, 1f, 1f, 0.09f);
    private static readonly Color TextColor = new Color(0.92f, 0.95f, 0.98f, 1f);
    private static readonly Color XColor = new Color(1f, 0.28f, 0.28f, 1f);
    private static readonly Color YColor = new Color(0.28f, 0.9f, 0.4f, 1f);
    private static readonly Color ZColor = new Color(0.25f, 0.55f, 1f, 1f);

    [MenuItem("Seven Seas/Setup Gyroscope Debug Scene")]
    public static void Setup()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (scene.name != "Scene1")
        {
            Debug.LogError("Open Scene1 before running the gyroscope debug setup.");
            return;
        }

        GameObject circle = GameObject.Find("Circle");

        if (circle == null)
        {
            Debug.LogError("Scene1 does not contain a GameObject named Circle.");
            return;
        }

        GameObject existingHud = GameObject.Find("GyroscopeDebugHUD");

        if (existingHud != null)
        {
            Debug.LogError("GyroscopeDebugHUD already exists. Setup was not run again.");
            return;
        }

        MotionSensorTest legacyTest = circle.GetComponent<MotionSensorTest>();

        if (legacyTest != null)
        {
            Undo.RecordObject(legacyTest, "Disable Legacy Motion Sensor Test");
            legacyTest.enabled = false;
        }

        Vector3 worldPosition = circle.transform.position;
        Undo.SetTransformParent(circle.transform, null, "Detach Circle From Camera");
        circle.transform.position = worldPosition;

        GameObject debugRoot = new GameObject("GyroscopeDebug");
        Undo.RegisterCreatedObjectUndo(debugRoot, "Create Gyroscope Debug Root");
        GyroscopeReader reader = debugRoot.AddComponent<GyroscopeReader>();
        AttitudeReader attitudeReader = debugRoot.AddComponent<AttitudeReader>();

        AttitudeCircleController controller = circle.GetComponent<AttitudeCircleController>();

        if (controller == null)
        {
            controller = Undo.AddComponent<AttitudeCircleController>(circle);
        }

        SetObjectReference(controller, "attitudeReader", attitudeReader);

        GameObject canvasObject = new GameObject(
            "GyroscopeDebugHUD",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create Gyroscope Debug HUD");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform panel = CreateImage(
            "Panel",
            canvasObject.transform,
            PanelColor,
            new Vector2(0.02f, 0.04f),
            new Vector2(0.45f, 0.96f));

        TMP_Text statusText = CreateText(
            "Status",
            panel,
            "GYRO OFFLINE",
            28f,
            FontStyles.Bold,
            TextAlignmentOptions.Left,
            new Vector2(0.04f, 0.9f),
            new Vector2(0.96f, 0.98f));

        AxisRow xRow = CreateAxisRow("X", panel, 0.79f, XColor);
        AxisRow yRow = CreateAxisRow("Y", panel, 0.7f, YColor);
        AxisRow zRow = CreateAxisRow("Z", panel, 0.61f, ZColor);

        TMP_Text magnitudeText = CreateText(
            "Magnitude",
            panel,
            "MAG  0.00",
            25f,
            FontStyles.Bold,
            TextAlignmentOptions.Left,
            new Vector2(0.04f, 0.53f),
            new Vector2(0.48f, 0.59f));
        TMP_Text peakText = CreateText(
            "Peak",
            panel,
            "PEAK  0.00",
            25f,
            FontStyles.Bold,
            TextAlignmentOptions.Right,
            new Vector2(0.52f, 0.53f),
            new Vector2(0.96f, 0.59f));

        RectTransform graphBackground = CreateImage(
            "Graph",
            panel,
            new Color(0f, 0f, 0f, 0.28f),
            new Vector2(0.04f, 0.08f),
            new Vector2(0.96f, 0.5f));
        RectTransform graphLines = CreateRect(
            "Lines",
            graphBackground,
            Vector2.zero,
            Vector2.one);
        GyroscopeHistoryGraphic historyGraphic = graphLines.gameObject.AddComponent<GyroscopeHistoryGraphic>();
        SetObjectReference(historyGraphic, "reader", reader);

        CreateText(
            "GraphLabel",
            panel,
            "RAW ANGULAR VELOCITY  |  LAST 3 SECONDS",
            18f,
            FontStyles.Normal,
            TextAlignmentOptions.Left,
            new Vector2(0.04f, 0.015f),
            new Vector2(0.96f, 0.065f));

        GyroscopeDebugHUD hud = canvasObject.AddComponent<GyroscopeDebugHUD>();
        SerializedObject hudObject = new SerializedObject(hud);
        hudObject.FindProperty("reader").objectReferenceValue = reader;
        hudObject.FindProperty("historyGraphic").objectReferenceValue = historyGraphic;
        hudObject.FindProperty("statusText").objectReferenceValue = statusText;
        hudObject.FindProperty("xValueText").objectReferenceValue = xRow.ValueText;
        hudObject.FindProperty("yValueText").objectReferenceValue = yRow.ValueText;
        hudObject.FindProperty("zValueText").objectReferenceValue = zRow.ValueText;
        hudObject.FindProperty("magnitudeText").objectReferenceValue = magnitudeText;
        hudObject.FindProperty("peakText").objectReferenceValue = peakText;
        hudObject.FindProperty("xBar").objectReferenceValue = xRow.Fill;
        hudObject.FindProperty("yBar").objectReferenceValue = yRow.Fill;
        hudObject.FindProperty("zBar").objectReferenceValue = zRow.Fill;
        hudObject.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = debugRoot;

        Debug.Log("Gyroscope debug movement and HUD setup completed for Scene1.");
    }

    private static AxisRow CreateAxisRow(
        string axis,
        Transform parent,
        float centerY,
        Color axisColor)
    {
        RectTransform row = CreateRect(
            axis + "Row",
            parent,
            new Vector2(0.04f, centerY - 0.035f),
            new Vector2(0.96f, centerY + 0.035f));
        TMP_Text valueText = CreateText(
            axis + "Value",
            row,
            axis + "   0.00",
            24f,
            FontStyles.Bold,
            TextAlignmentOptions.Left,
            new Vector2(0f, 0f),
            new Vector2(0.28f, 1f));
        RectTransform track = CreateImage(
            axis + "Track",
            row,
            TrackColor,
            new Vector2(0.3f, 0.18f),
            new Vector2(1f, 0.82f));
        RectTransform fill = CreateImage(
            axis + "Fill",
            track,
            axisColor,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 1f));
        CreateImage(
            axis + "Zero",
            track,
            new Color(1f, 1f, 1f, 0.55f),
            new Vector2(0.4985f, 0f),
            new Vector2(0.5015f, 1f));

        return new AxisRow(valueText, fill);
    }

    private static RectTransform CreateRect(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(gameObject, "Create " + name);
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
        Vector2 anchorMax)
    {
        RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = TextColor;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static void SetObjectReference(
        Object target,
        string propertyName,
        Object reference)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).objectReferenceValue = reference;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private readonly struct AxisRow
    {
        public AxisRow(TMP_Text valueText, RectTransform fill)
        {
            ValueText = valueText;
            Fill = fill;
        }

        public TMP_Text ValueText { get; }
        public RectTransform Fill { get; }
    }
}
