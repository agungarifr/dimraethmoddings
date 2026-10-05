using System;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using UnityEngine;

namespace DimraethModPack.Modules.Stats
{
    public class CustomStatModule : ModModuleBase
    {
        public static CustomStatModule Instance { get; private set; }

        public override string Category => "Stats";
        public override string Name => "Custom Stats";
        public override string Description => "Override attack speed, critical chance, spell haste, stamina, and health";

        public ConfigEntry<bool> OnlyAffectLocalPlayer;
        public ConfigEntry<float> AttackSpeed;
        public ConfigEntry<float> CritChance;
        public ConfigEntry<float> SpellHaste;
        public ConfigEntry<float> MaxStamina;
        public ConfigEntry<float> MaxConcentration;
        public ConfigEntry<float> MaxHealth;

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "Stats.CustomStats";

            Enabled = config.Bind(sec, "Enabled", false,
                "Enable Custom Stats Module (Default: false, Vanilla: false)");

            OnlyAffectLocalPlayer = config.Bind(sec, "OnlyAffectLocalPlayer", true,
                "Only apply stat overrides to local player character (Default: true, Vanilla: true)");

            AttackSpeed = config.Bind(sec, "AttackSpeed", 0f,
                "Override attack speed. 0 = vanilla game (Default: 0, Vanilla: 0)");

            CritChance = config.Bind(sec, "CritChance", 0f,
                "Override critical strike chance percentage. 0 = vanilla game (Default: 0, Vanilla: 0)");

            SpellHaste = config.Bind(sec, "SpellHaste", 0f,
                "Override cast speed / spell haste. 0 = vanilla game (Default: 0, Vanilla: 0)");

            MaxStamina = config.Bind(sec, "MaxStamina", 0f,
                "Override maximum stamina. 0 = vanilla game (Default: 0, Vanilla: 0)");

            MaxConcentration = config.Bind(sec, "MaxConcentration", 0f,
                "Override maximum concentration/mana. 0 = vanilla game (Default: 0, Vanilla: 0)");

            MaxHealth = config.Bind(sec, "MaxHealth", 0f,
                "Override maximum health. 0 = vanilla game (Default: 0, Vanilla: 0)");
        }

        public override void ApplyPatches(Harmony harmony)
        {
            harmony.PatchAll(typeof(Patches));
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;
            curY += DrawToggle(x, curY, width, "Only Local Player", OnlyAffectLocalPlayer, "Vanilla: ON", labelStyle, btnStyle);
            curY += DrawFloatSpinner(x, curY, width, "Attack Speed", AttackSpeed, 0.2f, 0f, 20f, "Vanilla: 0", labelStyle, btnStyle, "0.##");
            curY += DrawFloatSpinner(x, curY, width, "Crit Chance %", CritChance, 5f, 0f, 100f, "Vanilla: 0", labelStyle, btnStyle, "0.#");
            curY += DrawFloatDualSpinner(x, curY, width, "Spell Haste", SpellHaste, 0f, 500f, "Vanilla: 0", labelStyle, btnStyle, "0.#");
            curY += DrawFloatSpinner(x, curY, width, "Max Stamina", MaxStamina, 50f, 0f, 10000f, "Vanilla: 0", labelStyle, btnStyle, "0");
            curY += DrawFloatSpinner(x, curY, width, "Max Concentration", MaxConcentration, 50f, 0f, 10000f, "Vanilla: 0", labelStyle, btnStyle, "0");
            curY += DrawFloatSpinner(x, curY, width, "Max Health", MaxHealth, 100f, 0f, 50000f, "Vanilla: 0", labelStyle, btnStyle, "0");
            return curY - y;
        }

        // [2026-09-28 10:35] OBSOLETE - this body dereferenced the candidate
        // (`obj.IsPlayer` / `obj.TryCast<Player>()`) before validating its native pointer.
        // During the character-loading transition the game calls CalculateBaseSpellHaste
        // with a Player whose native pointer is null/stale, and that field read raises an
        // AccessViolationException. AVs are NOT catchable in .NET, so this try/catch never
        // ran and the process hard-crashed (BepInEx/ErrorLog.log 2026-09-28:
        // ObjectsCommon.get_IsPlayer() <- IsTargetPlayer <- Postfix_CalculateBaseSpellHaste).
        // Reason for replacement: never invoke anything on an unverified pointer; match the
        // candidate against the live local player instead (DimraethModPack.Core.PlayerIdentity).
        /*
        public static bool IsTargetPlayer(ObjectsCommon obj)
        {
            if (obj == null) return false;
            try
            {
                if (!obj.IsPlayer) return false;
                var player = obj.TryCast<Player>();
                if (player == null) return false;

                if (Instance.OnlyAffectLocalPlayer != null && !Instance.OnlyAffectLocalPlayer.Value)
                    return true;

                if (player.IsOwner) return true;

                if (DataStorage.Singleton != null && DataStorage.Singleton.Player == player)
                    return true;
            }
            catch { }

            return false;
        }
        */

        /// <summary>
        /// [2026-09-28 10:35] Crash-safe player test. The candidate is only ever matched by
        /// its raw pointer against the live local player, so nothing is invoked on a
        /// possibly-null/destroyed object. "Affect all players" is opt-in; only then do we
        /// fall back to the (dereferencing) identity check the user explicitly asked for.
        /// </summary>
        public static bool IsTargetPlayer(ObjectsCommon obj)
        {
            if (obj == null) return false;
            try
            {
                if (PlayerIdentity.IsLocalPlayer(obj)) return true;

                if (Instance.OnlyAffectLocalPlayer != null && Instance.OnlyAffectLocalPlayer.Value)
                    return false; // only the local player is in scope

                if (!obj.IsPlayer) return false; // opt-in path: any player
                var player = obj.TryCast<Player>();
                return player != null;
            }
            catch { }

            return false;
        }

        public static class Patches
        {
            [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateAttackSpeed))]
            [HarmonyPrefix]
            public static void Prefix_CalculateAttackSpeed(ObjectsCommon obj, ref float baseAttackSpeed, Attributes attributes)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                if (!IsTargetPlayer(obj)) return;
                float val = Instance.AttackSpeed.Value;
                if (val > 0f) baseAttackSpeed = val;
            }

            [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateAttackSpeed))]
            [HarmonyPostfix]
            public static void Postfix_CalculateAttackSpeed(ObjectsCommon obj, ref float __result)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                if (!IsTargetPlayer(obj)) return;
                float val = Instance.AttackSpeed.Value;
                if (val > 0f) __result = val;
            }

            [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateBaseAttackSpeed))]
            [HarmonyPostfix]
            public static void Postfix_CalculateBaseAttackSpeed(Player player, ref float __result)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                if (!IsTargetPlayer(player)) return;
                float val = Instance.AttackSpeed.Value;
                if (val > 0f) __result = val;
            }

            [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateCriticalRate))]
            [HarmonyPostfix]
            public static void Postfix_CalculateCriticalRate(ObjectsCommon obj, ref float __result)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                if (!IsTargetPlayer(obj)) return;
                float val = Instance.CritChance.Value;
                if (val > 0f) __result = val;
            }

            [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateSpellHaste))]
            [HarmonyPostfix]
            public static void Postfix_CalculateSpellHaste(ObjectsCommon obj, ref float __result)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                if (!IsTargetPlayer(obj)) return;
                float val = Instance.SpellHaste.Value;
                if (val > 0f) __result = val;
            }

            [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateBaseSpellHaste))]
            [HarmonyPostfix]
            public static void Postfix_CalculateBaseSpellHaste(Player player, ref float __result)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                if (!IsTargetPlayer(player)) return;
                float val = Instance.SpellHaste.Value;
                if (val > 0f) __result = val;
            }

            [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateMaxStamina))]
            [HarmonyPostfix]
            public static void Postfix_CalculateMaxStamina(ObjectsCommon obj, ref float __result)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                if (!IsTargetPlayer(obj)) return;
                float val = Instance.MaxStamina.Value;
                if (val > 0f) __result = val;
            }

            [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateBaseMaxStamina))]
            [HarmonyPostfix]
            public static void Postfix_CalculateBaseMaxStamina(Player player, ref float __result)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                if (!IsTargetPlayer(player)) return;
                float val = Instance.MaxStamina.Value;
                if (val > 0f) __result = val;
            }

            [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateMaxConcentration))]
            [HarmonyPostfix]
            public static void Postfix_CalculateMaxConcentration(ObjectsCommon obj, ref float __result)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                if (!IsTargetPlayer(obj)) return;
                float val = Instance.MaxConcentration.Value;
                if (val > 0f) __result = val;
            }

            [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateBaseMaxConcentration))]
            [HarmonyPostfix]
            public static void Postfix_CalculateBaseMaxConcentration(Player player, ref float __result)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                if (!IsTargetPlayer(player)) return;
                float val = Instance.MaxConcentration.Value;
                if (val > 0f) __result = val;
            }

            [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateMaxHealth))]
            [HarmonyPostfix]
            public static void Postfix_CalculateMaxHealth(ObjectsCommon obj, ref float __result)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                if (!IsTargetPlayer(obj)) return;
                float val = Instance.MaxHealth.Value;
                if (val > 0f) __result = val;
            }

            [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateBaseMaxHealth))]
            [HarmonyPostfix]
            public static void Postfix_CalculateBaseMaxHealth(Player player, ref float __result)
            {
                if (Instance == null || !Instance.IsEnabled) return;
                if (!IsTargetPlayer(player)) return;
                float val = Instance.MaxHealth.Value;
                if (val > 0f) __result = val;
            }
        }
    }
}
