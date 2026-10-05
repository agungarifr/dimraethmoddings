using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace DimraethModPack.Modules.SystemMod
{
    /* [2026-10-05] NEW: photosensitivity recon pass.
       The user is concerned that flashy spell / monster-attack VFX (e.g. the Minotaur's yellow
       hit) could trigger photosensitive seizures. Before we clamp anything we must first know
       WHAT the offending effects actually are. This module is a read-only scanner: it enumerates
       particle systems, their materials (base + emission color) and Light components, and writes
       a stable, de-duplicated inventory to BepInEx\VfxFlashDump.txt. Nothing in the game is
       modified here -- it only observes. Once real names/colors are known, the follow-up
       "Visual Comfort" module will clamp the identified effects.
    */

    /// <summary>
    /// Mod-manager module that owns the VFX recon scan. Registration + config only; the actual
    /// per-frame enumeration runs on the injected <see cref="VfxScannerBehaviour"/> so it keeps
    /// running while the manager menu is closed.
    /// </summary>
    public class VfxFlashDiagnosticModule : ModModuleBase
    {
        public static VfxFlashDiagnosticModule Instance { get; private set; }

        public override string Category => "System";
        public override string Name => "VFX Flash Scanner (Diagnostic)";
        public override string Description =>
            "Read-only recon: dumps particle systems, lights and emission colors to " +
            "BepInEx\\VfxFlashDump.txt so flashy effects (e.g. the Minotaur yellow hit) can be identified.";

        // Config
        public ConfigEntry<float> ScanIntervalSec;
        public ConfigEntry<bool> IncludeLights;
        public ConfigEntry<bool> IncludePrefabAssets;

        // UI state
        private string _status = "";
        private float _statusTimer;

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "System.VfxFlashScanner";

            Enabled = config.Bind(sec, "Enabled", false,
                "Run the periodic VFX scan. Off = no scanning, no file writes. (Default: false)");

            ScanIntervalSec = config.Bind(sec, "ScanIntervalSec", 1.0f,
                "Seconds between automatic scans while Enabled. Lower catches shorter-lived effects " +
                "but costs more CPU. (Default: 1.0)");

            IncludeLights = config.Bind(sec, "IncludeLights", true,
                "Also inventory Light components (often the real source of a screen-wide flash). (Default: true)");

            IncludePrefabAssets = config.Bind(sec, "IncludePrefabAssets", true,
                "Also inventory authored prefab assets, not just live scene objects. This catches effects " +
                "even when they are not currently playing. Used by the automatic scan; the manual dump " +
                "always includes assets. (Default: true)");
        }

        public override void ApplyPatches(Harmony harmony)
        {
            // No Harmony patches: recon observes existing objects, it does not intercept game methods.
            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<VfxScannerBehaviour>();
                var go = new GameObject("DimraethVfxScanner");
                go.hideFlags = HideFlags.HideAndDontSave;
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<VfxScannerBehaviour>();
            }
            catch (Exception ex)
            {
                DiagnosticsManager.LogError("VfxScanner", $"failed to start scanner behaviour: {ex}");
            }
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;

            curY += DrawToggle(x, curY, width, "Include Lights", IncludeLights, "default ON", labelStyle, btnStyle);
            curY += DrawToggle(x, curY, width, "Include Prefab Assets", IncludePrefabAssets, "default ON", labelStyle, btnStyle);
            curY += DrawFloatSpinner(x, curY, width, "Scan Interval (s)", ScanIntervalSec, 0.5f, 0.25f, 30f, "default 1", labelStyle, btnStyle);

            if (GUI.Button(new Rect(x, curY, width, 26f), "Dump Now (full, includes assets)", btnStyle))
            {
                try
                {
                    int added = VfxDump.Capture(includeAssets: true);
                    SetStatus($"<color=#55FF55>Dumped {added} new object(s).</color>");
                }
                catch (Exception ex)
                {
                    SetStatus($"<color=#FF5555>Dump failed: {ex.Message}</color>");
                    DiagnosticsManager.LogError("VfxScanner", $"manual dump failed: {ex}");
                }
            }
            curY += 30f;

            GUI.Label(new Rect(x, curY, width, 20f),
                $"Total trackers seen: <color=#FFDD44>{VfxDump.SeenCount}</color>  |  File: BepInEx\\VfxFlashDump.txt",
                labelStyle);
            curY += 22f;

            if (_statusTimer > 0f)
            {
                _statusTimer -= Time.unscaledDeltaTime;
                GUI.Label(new Rect(x, curY, width, 20f), _status, labelStyle);
                curY += 22f;
            }

            return curY - y;
        }

        private void SetStatus(string msg)
        {
            _status = msg;
            _statusTimer = 4f;
        }
    }

    /// <summary>
    /// Injected MonoBehaviour that drives the automatic scan loop for <see cref="VfxFlashDiagnosticModule"/>.
    /// </summary>
    public class VfxScannerBehaviour : MonoBehaviour
    {
        public VfxScannerBehaviour(IntPtr ptr) : base(ptr) { }

        private float _nextScanAt;

        private void Update()
        {
            var mod = VfxFlashDiagnosticModule.Instance;
            if (mod == null || !mod.IsEnabled) return;

            float now = Time.unscaledTime;
            if (now < _nextScanAt) return;

            float interval = mod.ScanIntervalSec != null ? mod.ScanIntervalSec.Value : 1f;
            _nextScanAt = now + Mathf.Max(0.25f, interval);

            try
            {
                VfxDump.Capture(includeAssets: mod.IncludePrefabAssets == null || mod.IncludePrefabAssets.Value);
            }
            catch (Exception ex)
            {
                DiagnosticsManager.LogError("VfxScanner", $"auto scan failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Formats and appends the VFX inventory. De-duplicated by hierarchy path + object name so
    /// the file stays a stable catalogue rather than a spawn log.
    /// </summary>
    internal static class VfxDump
    {
        private static readonly HashSet<string> _seenSystems = new HashSet<string>();
        private static readonly HashSet<string> _seenLights = new HashSet<string>();

        public static int SeenCount => _seenSystems.Count + _seenLights.Count;

        private static string FilePath => Path.Combine(Paths.BepInExRootPath, "VfxFlashDump.txt");

        /// <summary>Enumerates VFX and appends anything not yet catalogued. Returns the number of new entries.</summary>
        public static int Capture(bool includeAssets)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"==== VFX capture {DateTime.Now:yyyy-MM-dd HH:mm:ss} (assets={includeAssets}) ====");
            int added = 0;

            try
            {
                foreach (var ps in UnityEngine.Object.FindObjectsOfType<ParticleSystem>())
                {
                    if (ps == null) continue;
                    if (DumpParticleSystem(ps, sb)) added++;
                }
            }
            catch (Exception ex) { sb.AppendLine("[particles/scene] " + ex.Message); }

            if (includeAssets)
            {
                try
                {
                    foreach (var ps in Resources.FindObjectsOfTypeAll<ParticleSystem>())
                    {
                        if (ps == null) continue;
                        if (DumpParticleSystem(ps, sb)) added++;
                    }
                }
                catch (Exception ex) { sb.AppendLine("[particles/assets] " + ex.Message); }
            }

            var mod = VfxFlashDiagnosticModule.Instance;
            bool wantLights = mod == null || mod.IncludeLights == null || mod.IncludeLights.Value;
            if (wantLights)
            {
                try
                {
                    foreach (var light in UnityEngine.Object.FindObjectsOfType<Light>())
                    {
                        if (light == null) continue;
                        if (DumpLight(light, sb)) added++;
                    }
                }
                catch (Exception ex) { sb.AppendLine("[lights/scene] " + ex.Message); }

                if (includeAssets)
                {
                    try
                    {
                        foreach (var light in Resources.FindObjectsOfTypeAll<Light>())
                        {
                            if (light == null) continue;
                            if (DumpLight(light, sb)) added++;
                        }
                    }
                    catch (Exception ex) { sb.AppendLine("[lights/assets] " + ex.Message); }
                }
            }

            if (added == 0)
            {
                sb.AppendLine("(no new VFX objects this pass)");
            }

            AppendToFile(sb.ToString());
            return added;
        }

        private static bool DumpParticleSystem(ParticleSystem ps, StringBuilder sb)
        {
            string path = HierarchyPath(ps.transform);
            string key = "PS|" + path + "|" + ps.name;
            if (!_seenSystems.Add(key)) return false;

            sb.AppendLine($"[PS] {path}");
            Try(sb, "scene", () => ps.gameObject.scene.name);
            Try(sb, "active", () => ps.gameObject.activeInHierarchy.ToString());
            Try(sb, "playing", () => ps.isPlaying.ToString());
            Try(sb, "particleCount", () => ps.particleCount.ToString());

            try
            {
                var main = ps.main;
                sb.AppendLine($"    main: maxParticles={main.maxParticles} loop={main.loop} " +
                              $"space={main.simulationSpace} startLifetime={Mmc(main.startLifetime)} " +
                              $"startSpeed={Mmc(main.startSpeed)} " +
                              $"startColor={Gradient(main.startColor)}");
            }
            catch (Exception ex) { sb.AppendLine("    main: " + ex.Message); }

            try
            {
                var em = ps.emission;
                /* [2026-10-05] Interop note: EmissionModule.enabled, .rateOverDistance and .burstCount
                   have no getter in this IL2CPP build (the generator only emitted setters), so they are
                   omitted. rateOverTime does have a getter and is the value that matters for flicker. */
                sb.AppendLine($"    emission: rateOverTime={Mmc(em.rateOverTime)}");
            }
            catch (Exception ex) { sb.AppendLine("    emission: " + ex.Message); }

            try
            {
                var r = ps.GetComponent<ParticleSystemRenderer>();
                if (r != null)
                {
                    sb.AppendLine($"    renderer: mode={r.renderMode} sortingFudge={r.sortingFudge}");
                    var mat = r.sharedMaterial;
                    if (mat != null)
                    {
                        sb.AppendLine($"    material: {mat.name}");
                        Try(sb, "      shader", () => mat.shader.name);
                        Try(sb, "      _BaseColor", () => ColorStr(mat.GetColor("_BaseColor")));
                        Try(sb, "      _EmissionColor", () => ColorStr(mat.GetColor("_EmissionColor")));
                        Try(sb, "      _Color", () => ColorStr(mat.GetColor("_Color")));
                    }
                }
            }
            catch (Exception ex) { sb.AppendLine("    renderer: " + ex.Message); }

            return true;
        }

        private static bool DumpLight(Light light, StringBuilder sb)
        {
            string path = HierarchyPath(light.transform);
            string key = "L|" + path + "|" + light.name;
            if (!_seenLights.Add(key)) return false;

            sb.AppendLine($"[LIGHT] {path}");
            Try(sb, "    scene", () => light.gameObject.scene.name);
            Try(sb, "    enabled", () => light.enabled.ToString());
            Try(sb, "    type", () => light.type.ToString());
            Try(sb, "    color", () => ColorStr(light.color));
            Try(sb, "    intensity", () => light.intensity.ToString("0.###"));
            Try(sb, "    range", () => light.range.ToString("0.###"));
            Try(sb, "    shadows", () => light.shadows.ToString());
            return true;
        }

        // ---- helpers ----

        private static void Try(StringBuilder sb, string label, Func<string> get)
        {
            try { sb.AppendLine($"{label}: {get()}"); }
            catch (Exception ex) { sb.AppendLine($"{label}: <{ex.Message}>"); }
        }

        private static string HierarchyPath(Transform t)
        {
            if (t == null) return "<null>";
            var sb = new StringBuilder(t.name);
            var p = t.parent;
            int guard = 0;
            while (p != null && guard++ < 128)
            {
                sb.Insert(0, p.name + "/");
                p = p.parent;
            }
            return sb.ToString();
        }

        private static string Mmc(ParticleSystem.MinMaxCurve c)
        {
            try
            {
                switch (c.mode)
                {
                    case ParticleSystemCurveMode.Constant: return c.constant.ToString("0.###");
                    case ParticleSystemCurveMode.TwoConstants: return $"{c.constantMin:0.###}..{c.constantMax:0.###}";
                    default: return c.mode.ToString();
                }
            }
            catch { return "<err>"; }
        }

        private static string Gradient(ParticleSystem.MinMaxGradient g)
        {
            try
            {
                switch (g.mode)
                {
                    case ParticleSystemGradientMode.Color: return ColorStr(g.color);
                    case ParticleSystemGradientMode.TwoColors: return ColorStr(g.colorMin) + ".." + ColorStr(g.colorMax);
                    default: return g.mode.ToString();
                }
            }
            catch { return "<err>"; }
        }

        private static string ColorStr(Color c) => $"RGBA({c.r:0.##},{c.g:0.##},{c.b:0.##},{c.a:0.##})";

        private static void AppendToFile(string text)
        {
            try
            {
                File.AppendAllText(FilePath, text);
            }
            catch (Exception ex)
            {
                DiagnosticsManager.LogError("VfxScanner", $"write failed: {ex.Message}");
            }
        }
    }
}
