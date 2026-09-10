using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Observe movement decisions rather than running a second collision algorithm.
[InitializeOnLoad]
public static class FishRuntimeDiagnostics
{
    private static double nextCapture;
    private static readonly Queue<string> recentCaptures = new();
    static FishRuntimeDiagnostics()
    {
        EditorApplication.update += Update;
        EditorApplication.delayCall += CaptureWiring;
    }

    [MenuItem("Tools/Seven Seas/Capture Fishing Scene Wiring")]
    private static void CaptureWiring()
    {
        var rows = new List<string> { $"{DateTime.Now:O} playMode={EditorApplication.isPlaying} platform={Application.platform} timeScale={Time.timeScale} focused={Application.isFocused}" };
        foreach (var component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID))
        {
            if (component == null || component.gameObject.scene.name != "FishingLoopTest") continue;
            if (component is not FishingLoopController && component is not FishSpawner &&
                component is not FishBiteRaceController && component is not FishEcologyController &&
                component is not FishingHaptics && component is not FishingPauseController) continue;
            rows.Add($"{component.GetType().Name} object={component.name} scene={component.gameObject.scene.path} enabled={component.enabled} active={component.gameObject.activeInHierarchy}");
            var serialized = new SerializedObject(component);
            var property = serialized.GetIterator();
            while (property.NextVisible(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                var target = property.objectReferenceValue;
                rows.Add($"  {property.propertyPath} => {(target != null ? target.name + " [" + target.GetType().Name + "] " + AssetDatabase.GetAssetPath(target) : "NULL")}");
                if (target is FishController prefab)
                    rows.Add($"    prefab enabled={prefab.enabled} active={prefab.gameObject.activeSelf} layer={prefab.gameObject.layer} colliders={prefab.GetComponentsInChildren<Collider2D>(true).Length}");
            }
            if (component is FishSpawner spawner)
            {
                var area = spawner.GetComponent<BoxCollider2D>();
                rows.Add($"  spawnArea={(area != null ? area.bounds.ToString() : "MISSING")}");
            }
            if (component is FishBiteRaceController)
                rows.Add($"  fishLayer={serialized.FindProperty("fishLayer").intValue}");
            if (component is FishingHaptics haptics)
                rows.Add($"  vibrationEnabled={haptics.VibrationEnabled}");
        }
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "SevenSeasFishingWiring.txt"), string.Join("\n", rows));
    }
    private static void Update()
    {
        if (!EditorApplication.isPlaying) { recentCaptures.Clear(); return; }
        if (EditorApplication.isCompiling || EditorApplication.timeSinceStartup < nextCapture) return;
        nextCapture = EditorApplication.timeSinceStartup + 1;
        var loop = UnityEngine.Object.FindFirstObjectByType<FishingLoopController>();
        if (loop != null && (loop.CurrentState == FishingLoopState.Baiting || loop.CurrentState == FishingLoopState.Reeling)) Capture(loop);
    }
    [MenuItem("Tools/Seven Seas/Capture Fish Diagnostics")]
    private static void CaptureNow()
    {
        if (!EditorApplication.isPlaying) return;
        var loop = UnityEngine.Object.FindFirstObjectByType<FishingLoopController>();
        if (loop != null) Capture(loop);
    }
    private static void Capture(FishingLoopController loop)
    {
        var rows = new List<string>();
        foreach (var fish in UnityEngine.Object.FindObjectsByType<FishController>(FindObjectsSortMode.InstanceID))
        {
            fish.TryGetComponent<FishAppearance>(out var appearance);
            rows.Add($"{fish.name}#{fish.GetInstanceID()} category={(appearance != null ? appearance.Category.ToString() : "missing")}" +
                $" state={fish.State} canNavigate={fish.CanNavigate} external={fish.IsExternallyNavigating}" +
                $" position={fish.transform.position} mouth={fish.BitePosition}" +
                $" hookDistance={Vector2.Distance(fish.BitePosition, loop.HookPosition):F4}" +
                $" canClaim={fish.CanClaimBait} facingNavigation={fish.IsFacingNavigationTarget}" +
                $" navigation={fish.NavigationStatus} waypoint={fish.NavigationWaypoint}" +
                $" blocker={(fish.NavigationBlocker != null ? fish.NavigationBlocker.name : "none")}");
        }
        string snapshot = $"{DateTime.Now:O} scene={loop.gameObject.scene.path} platform={Application.platform} frame={Time.frameCount} attempt={loop.CurrentAttemptId}" +
            $" state={loop.CurrentState} timeScale={Time.timeScale} delta={Time.deltaTime}" +
            $" hooked={(loop.HookedFish != null ? loop.HookedFish.name : "none")}\n" + string.Join("\n", rows);
        recentCaptures.Enqueue(snapshot);
        while (recentCaptures.Count > 60) recentCaptures.Dequeue();
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "SevenSeasFishDiagnostics.txt"), snapshot);
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "SevenSeasFishDiagnosticsHistory.txt"), string.Join("\n\n", recentCaptures));
    }
}
