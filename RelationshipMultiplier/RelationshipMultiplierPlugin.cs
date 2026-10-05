using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace Dimraeth.RelationshipMultiplier
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class RelationshipMultiplierPlugin : BasePlugin
    {
        internal static ManualLogSource LogSrc = null;
        internal static ConfigEntry<float> GainMultiplier = null;
        internal static ConfigEntry<float> TotalGainMultiplier = null;

        public override void Load()
        {
            LogSrc = Log;

            // ---- Konfigurasi (dapat diubah lewat BepInEx/config) ----
            const string section = "General";
            GainMultiplier = Config.Bind(
                section,
                "RelationshipGainMultiplier",
                2.0f,
                "Pengali tiap komponen relasi (Opinion/Friendship/Romance/ValueAlignment). 1.0 = normal, 2.0 = 2x.");
            TotalGainMultiplier = Config.Bind(
                section,
                "TotalRelationshipGainMultiplier",
                1.0f,
                "Pengali tambahan pada total relationship gain. 1.0 = tidak mengubah.");

            // ---- Pasang semua [HarmonyPatch] dalam assembly ini ----
            var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            Log.LogInfo($"{PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION} dimuat. " +
                        $"Gain x{GainMultiplier.Value} / Total x{TotalGainMultiplier.Value}");
        }
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "dimraeth.relationshipmultiplier";
        public const string PLUGIN_NAME = "Relationship Multiplier (Dimraeth)";
        public const string PLUGIN_VERSION = "0.1.0";
    }
}
