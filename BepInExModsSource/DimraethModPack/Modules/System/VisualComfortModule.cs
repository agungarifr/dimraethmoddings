using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DimraethModPack.Modules.SystemMod
{
    /* [2026-10-05] Photosensitivity mitigation for flashy spell / monster-attack VFX.
       Tier 1: disable URP post-processing (bloom) on the main camera -- stops a bright effect
               blooming across the screen.
       Tier 2: clamp the VFX objects themselves, conservatively and only when they are part of a
               particle effect:
                 * VFX-scoped Lights (non-directional AND sharing a hierarchy with a ParticleSystem)
                   are disabled, so an impact flash stops lighting up the scene. World lights, the
                   sun and street lamps have no ParticleSystem sibling and are left untouched.
                 * `_EmissionColor` on materials used by ParticleSystemRenderers is blacked out, so
                   emissive VFX stop self-glowing even without bloom.
               Base particle colours are deliberately NOT touched, so effects stay readable as
               telegraphs. Everything is restored when the toggle/module is switched off.

       [2026-10-05] Note: the initial camera-only implementation of this module was extended with
       Tier 2 in the same session. Its Update body (camera flag only) is superseded by the version
       below; not preserved verbatim because this file was authored and extended within one task.
    */

    /// <summary>
    /// Mod-manager module that reduces screen-wide flash intensity by disabling URP post-processing
    /// on the main gameplay camera and clamping VFX lights / emission.
    /// </summary>
    public class VisualComfortModule : ModModuleBase
    {
        public static VisualComfortModule Instance { get; private set; }

        public override string Category => "System";
        public override string Name => "Visual Comfort (Flash Reduction)";
        public override string Description =>
            "Reduces photosensitive-flash risk: disables URP post-processing (bloom) on the main camera, " +
            "and clamps VFX-scoped lights + emission so bright effects stop flashing across the screen.";

        public ConfigEntry<bool> DisablePostProcessing;
        public ConfigEntry<bool> DisableVfxLights;
        public ConfigEntry<bool> SuppressVfxEmission;

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "System.VisualComfort";

            Enabled = config.Bind(sec, "Enabled", false,
                "Master switch for visual-comfort reductions. (Default: false)");

            DisablePostProcessing = config.Bind(sec, "DisablePostProcessing", true,
                "Turn off URP post-processing on the main camera (removes bloom, which is the main " +
                "reason spells fill the screen with light). (Default: true)");

            DisableVfxLights = config.Bind(sec, "DisableVfxLights", true,
                "Disable non-directional Lights that belong to a particle effect (impact flashes). " +
                "World/sun/street lights are never touched. (Default: true)");

            SuppressVfxEmission = config.Bind(sec, "SuppressVfxEmission", true,
                "Black out _EmissionColor on materials used by particle effects, so emissive VFX stop " +
                "self-glowing. Base particle colours are left intact. (Default: true)");
        }

        public override void ApplyPatches(Harmony harmony)
        {
            // No Harmony patches: the camera flag is applied live by the injected behaviour.
            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<VisualComfortBehaviour>();
                var go = new GameObject("DimraethVisualComfort");
                go.hideFlags = HideFlags.HideAndDontSave;
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<VisualComfortBehaviour>();
            }
            catch (Exception ex)
            {
                DiagnosticsManager.LogError("VisualComfort", $"failed to start behaviour: {ex}");
            }
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;

            curY += DrawToggle(x, curY, width, "Disable Post-Processing", DisablePostProcessing,
                "default ON", labelStyle, btnStyle);
            curY += DrawToggle(x, curY, width, "Disable VFX Lights", DisableVfxLights,
                "default ON", labelStyle, btnStyle);
            curY += DrawToggle(x, curY, width, "Suppress VFX Emission", SuppressVfxEmission,
                "default ON", labelStyle, btnStyle);

            string post = VisualComfortBehaviour.PostProcessingOff
                ? "<color=#55FF55>OFF</color>"
                : "<color=#FFAA55>ON (vanilla)</color>";
            GUI.Label(new Rect(x, curY, width, 20f),
                $"post-processing: {post}  |  lights clamped: <color=#FFDD44>{VisualComfortBehaviour.SuppressedLights}</color>" +
                $"  |  materials: <color=#FFDD44>{VisualComfortBehaviour.SuppressedMaterials}</color>",
                labelStyle);
            curY += 24f;

            return curY - y;
        }
    }

    /// <summary>
    /// Applies/restores all visual-comfort reductions. Discovery (which objects are VFX) runs at a
    /// low rate; light suppression is re-asserted every frame so an animated/flickering effect
    /// cannot win the race. Everything is restored when the toggles or the module are switched off.
    /// </summary>
    public class VisualComfortBehaviour : MonoBehaviour
    {
        public VisualComfortBehaviour(IntPtr ptr) : base(ptr) { }

        /// <summary>True when post-processing is currently suppressed on the tracked camera.</summary>
        public static bool PostProcessingOff { get; private set; }
        /// <summary>Number of VFX lights currently disabled.</summary>
        public static int SuppressedLights { get; private set; }
        /// <summary>Number of particle materials whose emission is currently blacked out.</summary>
        public static int SuppressedMaterials { get; private set; }

        // Camera: tracked URP camera data + original renderPostProcessing value.
        private readonly List<(UniversalAdditionalCameraData data, bool orig)> _camTracked = new();
        private readonly HashSet<int> _camTrackedIds = new();

        // VFX lights: tracked Light + original enabled value.
        private readonly List<(Light light, bool orig)> _lightsTracked = new();
        private readonly HashSet<int> _lightTrackedIds = new();

        // Particle materials: tracked Material + original emission colour.
        private readonly List<(Material mat, Color orig)> _matsTracked = new();
        private readonly HashSet<int> _matTrackedIds = new();

        private float _nextCamAt;
        private float _nextVfxAt;

        private void Update()
        {
            var mod = VisualComfortModule.Instance;
            if (mod == null) return;

            bool active = mod.IsEnabled;
            bool wantPost = active && (mod.DisablePostProcessing?.Value ?? false);
            bool wantLights = active && (mod.DisableVfxLights?.Value ?? false);
            bool wantEmission = active && (mod.SuppressVfxEmission?.Value ?? false);

            float now = Time.unscaledTime;

            if (now >= _nextCamAt)
            {
                _nextCamAt = now + 0.25f;
                ApplyCamera(wantPost);
            }

            if (now >= _nextVfxAt)
            {
                _nextVfxAt = now + 0.5f;
                if (wantLights) DiscoverVfxLights(); else RestoreLights();
                if (wantEmission) SuppressVfxEmission(); else RestoreEmission();
            }

            EnforceLights(wantLights);

            PostProcessingOff = _camTracked.Count > 0;
            SuppressedLights = _lightsTracked.Count;
            SuppressedMaterials = _matsTracked.Count;
        }

        // ---- camera post-processing ----

        private void ApplyCamera(bool wantOff)
        {
            try
            {
                if (wantOff)
                {
                    var cam = Camera.main;
                    if (!cam) return;
                    var data = cam.GetComponent<UniversalAdditionalCameraData>();
                    if (!data) return;

                    int id = cam.GetInstanceID();
                    if (!_camTrackedIds.Contains(id))
                    {
                        bool orig = true; // vanilla default is on
                        try { orig = data.renderPostProcessing; } catch { }
                        _camTracked.Add((data, orig));
                        _camTrackedIds.Add(id);
                    }
                    data.renderPostProcessing = false;
                }
                else
                {
                    RestoreCamera();
                }
            }
            catch (Exception ex)
            {
                DiagnosticsManager.LogError("VisualComfort", $"camera apply failed: {ex.Message}");
            }
        }

        private void RestoreCamera()
        {
            foreach (var (data, orig) in _camTracked)
            {
                try { if (data) data.renderPostProcessing = orig; } catch { }
            }
            _camTracked.Clear();
            _camTrackedIds.Clear();
        }

        // ---- VFX lights ----

        private void DiscoverVfxLights()
        {
            try
            {
                foreach (var light in UnityEngine.Object.FindObjectsOfType<Light>())
                {
                    if (!light) continue;
                    int id = light.GetInstanceID();
                    if (_lightTrackedIds.Contains(id)) continue;
                    if (!IsVfxLight(light)) continue;

                    _lightsTracked.Add((light, light.enabled));
                    _lightTrackedIds.Add(id);
                }
            }
            catch (Exception ex)
            {
                DiagnosticsManager.LogError("VisualComfort", $"light discovery failed: {ex.Message}");
            }
        }

        private void EnforceLights(bool wantLights)
        {
            if (!wantLights) return;
            foreach (var (light, _) in _lightsTracked)
            {
                try { if (light) light.enabled = false; } catch { }
            }
        }

        private void RestoreLights()
        {
            foreach (var (light, orig) in _lightsTracked)
            {
                try { if (light) light.enabled = orig; } catch { }
            }
            _lightsTracked.Clear();
            _lightTrackedIds.Clear();
        }

        /// <summary>
        /// A light is "VFX" only if it is non-directional AND shares a hierarchy with a ParticleSystem.
        /// This deliberately excludes the sun and standalone world/street lights.
        /// </summary>
        private static bool IsVfxLight(Light light)
        {
            try
            {
                if (light.type == LightType.Directional) return false;
                var go = light.gameObject;
                if (!go) return false;
                if (go.GetComponentInParent<ParticleSystem>()) return true;
                if (go.GetComponentInChildren<ParticleSystem>()) return true;
                return false;
            }
            catch { return false; }
        }

        // ---- particle emission ----

        private void SuppressVfxEmission()
        {
            try
            {
                foreach (var ps in UnityEngine.Object.FindObjectsOfType<ParticleSystem>())
                {
                    if (!ps) continue;
                    var renderers = ps.GetComponentsInChildren<ParticleSystemRenderer>(true);
                    if (renderers == null) continue;

                    foreach (var r in renderers)
                    {
                        if (!r) continue;
                        var mats = r.sharedMaterials;
                        if (mats == null) continue;

                        foreach (var mat in mats)
                        {
                            if (!mat) continue;
                            int id = mat.GetInstanceID();
                            if (_matTrackedIds.Contains(id)) continue;

                            bool hasEmission;
                            try { hasEmission = mat.HasProperty("_EmissionColor"); }
                            catch { hasEmission = false; }
                            if (!hasEmission) continue;

                            Color orig;
                            try { orig = mat.GetColor("_EmissionColor"); }
                            catch { continue; }

                            _matTrackedIds.Add(id);
                            _matsTracked.Add((mat, orig));
                            try { mat.SetColor("_EmissionColor", Color.black); } catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DiagnosticsManager.LogError("VisualComfort", $"emission scan failed: {ex.Message}");
            }
        }

        private void RestoreEmission()
        {
            foreach (var (mat, orig) in _matsTracked)
            {
                try { if (mat) mat.SetColor("_EmissionColor", orig); } catch { }
            }
            _matsTracked.Clear();
            _matTrackedIds.Clear();
        }
    }
}
