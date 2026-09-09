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
    static FishRuntimeDiagnostics() => EditorApplication.update += Update;
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
        string snapshot = $"{DateTime.Now:O} frame={Time.frameCount} attempt={loop.CurrentAttemptId}" +
            $" state={loop.CurrentState} timeScale={Time.timeScale} delta={Time.deltaTime}" +
            $" hooked={(loop.HookedFish != null ? loop.HookedFish.name : "none")}\n" + string.Join("\n", rows);
        recentCaptures.Enqueue(snapshot);
        while (recentCaptures.Count > 60) recentCaptures.Dequeue();
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "SevenSeasFishDiagnostics.txt"), snapshot);
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "SevenSeasFishDiagnosticsHistory.txt"), string.Join("\n\n", recentCaptures));
    }
}