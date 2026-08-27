using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GameViewPresetInitializer
{
    // 在这里配置团队需要共享的 Game View 尺寸。
    private static readonly Preset[] Presets =
    {

        new("GalaxyPad Landscape 2304x1440", 2304, 1440, false),
        new("GalaxyPad Portrait 1440x2304", 1440, 2304, false),
    };

    static GameViewPresetInitializer()
    {
        // 等 Unity Editor 完成初始化后再执行。
        EditorApplication.delayCall += InstallPresets;
    }

    [MenuItem("Tools/Game View/Install Shared Presets")]
    private static void InstallPresets()
    {
        try
        {
            foreach (Preset preset in Presets)
            {
                AddPresetIfMissing(preset);
            }

            Debug.Log("[Game View] Shared presets are ready.");
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[Game View] Could not install shared presets. " +
                $"Unity's internal API may have changed.\n{exception}");
        }
    }

    private static void AddPresetIfMissing(Preset preset)
    {
        Assembly editorAssembly = typeof(Editor).Assembly;

        Type sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
        Type sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
        Type sizeTypeEnum = editorAssembly.GetType("UnityEditor.GameViewSizeType");
        Type groupTypeEnum = editorAssembly.GetType(
            "UnityEditor.GameViewSizeGroupType");

        if (sizesType == null ||
            sizeType == null ||
            sizeTypeEnum == null ||
            groupTypeEnum == null)
        {
            throw new InvalidOperationException(
                "Unity Game View internal types were not found.");
        }

        object singleton = sizesType
            .GetProperty(
                "instance",
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.FlattenHierarchy)
            ?.GetValue(null);

        if (singleton == null)
        {
            throw new InvalidOperationException(
                "Unity GameViewSizes instance was not found.");
        }

        // Android 是编辑器中最常用的 Game View 分组。
        object standaloneGroup = Enum.Parse(groupTypeEnum, "Android");

        object group = sizesType
            .GetMethod(
                "GetGroup",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic)
            ?.Invoke(singleton, new[] { standaloneGroup });

        if (group == null)
        {
            throw new InvalidOperationException(
                "Standalone Game View group was not found.");
        }

        Type groupType = group.GetType();

        MethodInfo getTotalCount = groupType.GetMethod(
            "GetTotalCount",
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic);

        MethodInfo getGameViewSize = groupType.GetMethod(
            "GetGameViewSize",
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic);

        MethodInfo addCustomSize = groupType.GetMethod(
            "AddCustomSize",
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic);

        PropertyInfo baseTextProperty = sizeType.GetProperty(
            "baseText",
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic);

        int count = (int)getTotalCount.Invoke(group, null);

        for (int index = 0; index < count; index++)
        {
            object existingSize =
                getGameViewSize.Invoke(group, new object[] { index });

            string existingName =
                baseTextProperty?.GetValue(existingSize) as string;

            if (string.Equals(
                    existingName,
                    preset.Name,
                    StringComparison.Ordinal))
            {
                return;
            }
        }

        object viewSizeType = Enum.Parse(
            sizeTypeEnum,
            preset.IsAspectRatio ? "AspectRatio" : "FixedResolution");

        object newSize = Activator.CreateInstance(
            sizeType,
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic,
            null,
            new object[]
            {
                viewSizeType,
                preset.Width,
                preset.Height,
                preset.Name
            },
            null);

        addCustomSize.Invoke(group, new[] { newSize });

        Debug.Log($"[Game View] Added preset: {preset.Name}");
    }

    private readonly struct Preset
    {
        public string Name { get; }
        public int Width { get; }
        public int Height { get; }
        public bool IsAspectRatio { get; }

        public Preset(
            string name,
            int width,
            int height,
            bool isAspectRatio)
        {
            Name = name;
            Width = width;
            Height = height;
            IsAspectRatio = isAspectRatio;
        }
    }
}
