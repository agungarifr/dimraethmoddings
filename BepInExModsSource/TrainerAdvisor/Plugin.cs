using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

namespace DimraethTrainerAdvisor
{
    // Standalone Harmony plugin. When the vanilla trainer window opens, it injects a
    // display-only panel that recommends the optimal physical / magic / hybrid point spends
    // for the current race & class, starting from the player's current attributes, level and
    // XP bar. It never spends, stages, or writes any point.
    [BepInPlugin("dev.dimraeth.traineradvisor", "Dimraeth Trainer Advisor", "1.0.0")]
    [BepInProcess("Dimraeth.exe")]
    public sealed class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        internal static Harmony Harmony;

        internal static bool Enabled;
        internal static bool AnchorInside;
        internal static float PanelWidth;
        internal static float PanelHeight;

        public override void Load()
        {
            Log = base.Log;
            Harmony = new Harmony("dev.dimraeth.traineradvisor");

            Enabled = Config.Bind(
                "General", "Enabled", true,
                "Inject the optimal-build advisor panel into the trainer window.").Value;

            AnchorInside = Config.Bind(
                "General", "AnchorInside", false,
                "Dock the advisor inside the trainer window's right edge instead of just outside it. "
                + "Enable this if the outside panel is clipped off-screen.").Value;

            PanelWidth = Config.Bind(
                "General", "PanelWidth", 330f,
                "Width in pixels of the advisor panel.").Value;

            PanelHeight = Config.Bind(
                "General", "PanelHeight", 420f,
                "Height in pixels of the advisor panel when docked outside the window.").Value;

            ClassInjector.RegisterTypeInIl2Cpp<TrainerAdvisorPanel>();

            var target = AccessTools.Method(typeof(PlayerUIView), "Bind");
            if (target == null)
            {
                Log.LogError("PlayerUIView.Bind not found; Trainer Advisor disabled.");
                return;
            }
            Harmony.Patch(target, postfix: new HarmonyMethod(typeof(TrainerBindPatch), nameof(TrainerBindPatch.Postfix)));

            Log.LogInfo("Dimraeth Trainer Advisor loaded; patched PlayerUIView.Bind.");
        }
    }
}
