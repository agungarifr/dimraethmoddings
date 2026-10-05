using System;
using System.Diagnostics;
using BepInEx.Configuration;
using UnityEngine;

namespace DimraethModPack.Core
{
    public static class DiagnosticsManager
    {
        public static ConfigEntry<bool> EnableDiagnostics;
        public static ConfigEntry<bool> VerboseModuleLogging;
        public static ConfigEntry<float> StutterThresholdMs;
        public static ConfigEntry<float> MemoryLogIntervalSec;

        // Metrics
        public static float CurrentFps { get; private set; } = 60f;
        public static float AvgFps { get; private set; } = 60f;
        public static float OnePercentLowFps { get; private set; } = 60f;
        public static int StutterSpikeCount { get; private set; } = 0;
        public static float MaxSpikeDurationMs { get; private set; } = 0f;
        public static float ManagedRamMb { get; private set; } = 0f;
        public static float ProcessWorkingSetMb { get; private set; } = 0f;

        private static float _lastMemoryLogTime = 0f;
        private static float _prevManagedBytes = 0f;
        private static readonly float[] _recentFrameTimes = new float[100];
        private static int _frameIndex = 0;
        private static int _frameCount = 0;
        private static Process _currentProcess;

        public static void Init(ConfigFile config)
        {
            try
            {
                _currentProcess = Process.GetCurrentProcess();
            }
            catch { }

            string sec = "Diagnostics";
            EnableDiagnostics = config.Bind(sec, "EnableDiagnostics", true,
                "Enable real-time lag/stutter detector, RAM leak profiler, and diagnostic warnings (Default: true)");

            VerboseModuleLogging = config.Bind(sec, "VerboseModuleLogging", true,
                "Print real-time diagnostic logs when modules execute (Default: true)");

            StutterThresholdMs = config.Bind(sec, "StutterThresholdMs", 50.0f,
                "Frame time in milliseconds that triggers a stutter/freeze alert (Default: 50.0ms, i.e. < 20 FPS)");

            MemoryLogIntervalSec = config.Bind(sec, "MemoryLogIntervalSec", 15.0f,
                "Interval in seconds to log memory usage and monitor memory leaks (Default: 15.0s)");

            UpdateMemoryMetrics();
        }

        public static void Log(string tag, string message)
        {
            // [2026-09-26 15:20] Disabled (user request): the verbose module "log feed" (per-execution
            // messages from NoClickPickup/Relationship/etc., plus the periodic MemoryWatch line) was
            // noisy in BepInEx\LogOutput.log. This is now a no-op. Re-enable by uncommenting the body
            // (also still gated by the Diagnostics.VerboseModuleLogging config).
            // if (VerboseModuleLogging == null || !VerboseModuleLogging.Value) return;
            // DimraethModPackPlugin.Log?.LogInfo($"[{tag}] {message}");
        }

        public static void LogWarning(string tag, string message)
        {
            DimraethModPackPlugin.Log?.LogWarning($"[{tag}] {message}");
        }

        public static void LogError(string tag, string message)
        {
            DimraethModPackPlugin.Log?.LogError($"[{tag}] {message}");
        }

        private static float _lastLagWarningTime = 0f;

        public static void OnUpdate()
        {
            if (EnableDiagnostics == null || !EnableDiagnostics.Value) return;

            float dt = Time.unscaledDeltaTime;
            if (dt <= 0.0001f) return;

            float frameMs = dt * 1000f;
            CurrentFps = 1f / dt;

            // Rolling frame times for 1% low and avg
            _recentFrameTimes[_frameIndex] = dt;
            _frameIndex = (_frameIndex + 1) % _recentFrameTimes.Length;
            if (_frameCount < _recentFrameTimes.Length) _frameCount++;

            // Detect Stutter / Frame Freeze
            float threshold = StutterThresholdMs != null ? StutterThresholdMs.Value : 50f;
            if (frameMs >= threshold)
            {
                StutterSpikeCount++;
                if (frameMs > MaxSpikeDurationMs) MaxSpikeDurationMs = frameMs;

                /* [2026-09-26 15:20] Disabled (user request): the LagDetector stutter-spike warning
                   spammed BepInEx\LogOutput.log. The spike counting above is kept so the menu HUD
                   still reports a count; only the log output is off. Re-enable by uncommenting.
                // Debounce logging to at most once per 3.0s to avoid adding disk I/O during heavy frame drops
                if (Time.unscaledTime - _lastLagWarningTime >= 3.0f)
                {
                    _lastLagWarningTime = Time.unscaledTime;
                    UpdateMemoryMetrics();
                    LogWarning("LagDetector",
                        $"⚠️ STUTTER SPIKE: Frame took {frameMs:F1}ms ({CurrentFps:F0} FPS)! Spike #{StutterSpikeCount} | " +
                        $"Managed RAM: {ManagedRamMb:F1}MB | Working Set: {ProcessWorkingSetMb:F1}MB");
                }
                */
            }

            // Periodic Memory & Leak Monitor
            float memInterval = MemoryLogIntervalSec != null ? MemoryLogIntervalSec.Value : 15f;
            if (Time.unscaledTime - _lastMemoryLogTime >= memInterval)
            {
                _lastMemoryLogTime = Time.unscaledTime;
                CheckMemoryStatus(memInterval);
            }
        }

        private static void UpdateMemoryMetrics()
        {
            try
            {
                long managedBytes = GC.GetTotalMemory(false);
                ManagedRamMb = managedBytes / (1024f * 1024f);

                if (_currentProcess != null)
                {
                    _currentProcess.Refresh();
                    ProcessWorkingSetMb = _currentProcess.WorkingSet64 / (1024f * 1024f);
                }
            }
            catch { }
        }

        private static void CheckMemoryStatus(float interval)
        {
            UpdateMemoryMetrics();

            // Calculate rolling stats
            float sum = 0f;
            float maxFrame = 0f;
            for (int i = 0; i < _frameCount; i++)
            {
                float t = _recentFrameTimes[i];
                sum += t;
                if (t > maxFrame) maxFrame = t;
            }
            AvgFps = _frameCount > 0 ? (_frameCount / sum) : CurrentFps;
            OnePercentLowFps = maxFrame > 0f ? (1f / maxFrame) : CurrentFps;

            // Memory delta leak detection
            float currentManaged = ManagedRamMb;
            float deltaMb = currentManaged - _prevManagedBytes;
            _prevManagedBytes = currentManaged;

            if (deltaMb > 40f) // Jumper alert (> 40MB in single interval)
            {
                // [2026-09-26 15:20] Disabled (user request): MemoryWatch surge log (noisy diagnostics output).
                // LogWarning("MemoryWatch",
                //     $"⚠️ FAST RAM SURGE DETECTED: +{deltaMb:F1}MB allocated in {interval:F0}s! " +
                //     $"Managed RAM: {currentManaged:F1}MB | Working Set: {ProcessWorkingSetMb:F1}MB | Avg FPS: {AvgFps:F0}");
            }
            else if (VerboseModuleLogging != null && VerboseModuleLogging.Value)
            {
                // [2026-09-26 15:20] Disabled (user request): periodic MemoryWatch log line (part of the "log feed").
                // Log("MemoryWatch",
                //     $"Managed RAM: {currentManaged:F1}MB | Process RAM: {ProcessWorkingSetMb:F1}MB | " +
                //     $"FPS: {CurrentFps:F0} (Avg: {AvgFps:F0}, 1% Low: {OnePercentLowFps:F0}) | Spikes: {StutterSpikeCount}");
            }
        }
    }
}
