using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace AttackSpeedMod
{
    [BepInPlugin("com.custom.statmod", "CustomStatMod", "2.0.0")]
    public class CustomStatModPlugin : BasePlugin
    {
        internal static new ManualLogSource Log;

        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<bool> OnlyAffectLocalPlayer;

        // Attack Speed
        public static ConfigEntry<float> AttackSpeed;
        // Critical Chance
        public static ConfigEntry<float> CritChance;
        // Spell Haste
        public static ConfigEntry<float> SpellHaste;
        // Max Stamina
        public static ConfigEntry<float> MaxStamina;
        // Max Concentration
        public static ConfigEntry<float> MaxConcentration;
        // Max Health
        public static ConfigEntry<float> MaxHealth;

        public override void Load()
        {
            Log = base.Log;

            ModEnabled = Config.Bind("General", "ModEnabled", true,
                "Enable or disable the entire mod (Default: true)");

            OnlyAffectLocalPlayer = Config.Bind("General", "OnlyAffectLocalPlayer", true,
                "If true, stat modifications ONLY apply to the local player. Monsters and other players are untouched (Default: true)");

            AttackSpeed = Config.Bind("Stats", "AttackSpeed", 0f,
                "Override attack speed. 0 = vanilla (Default: 0)");

            CritChance = Config.Bind("Stats", "CritChance", 0f,
                "Override critical chance. 0 = vanilla (Default: 0)");

            SpellHaste = Config.Bind("Stats", "SpellHaste", 0f,
                "Override spell haste. 0 = vanilla (Default: 0)");

            MaxStamina = Config.Bind("Stats", "MaxStamina", 0f,
                "Override max stamina. 0 = vanilla (Default: 0)");

            MaxConcentration = Config.Bind("Stats", "MaxConcentration", 0f,
                "Override max concentration. 0 = vanilla (Default: 0)");

            MaxHealth = Config.Bind("Stats", "MaxHealth", 0f,
                "Override max health. 0 = vanilla (Default: 0)");

            var harmony = new Harmony("com.custom.statmod");
            harmony.PatchAll(typeof(Patch_CalculateAttackSpeed));
            harmony.PatchAll(typeof(Patch_CalculateBaseAttackSpeed));
            harmony.PatchAll(typeof(Patch_CalculateCriticalRate));
            harmony.PatchAll(typeof(Patch_CalculateSpellHaste));
            harmony.PatchAll(typeof(Patch_CalculateBaseSpellHaste));
            harmony.PatchAll(typeof(Patch_CalculateMaxStamina));
            harmony.PatchAll(typeof(Patch_CalculateBaseMaxStamina));
            harmony.PatchAll(typeof(Patch_CalculateMaxConcentration));
            harmony.PatchAll(typeof(Patch_CalculateBaseMaxConcentration));
            harmony.PatchAll(typeof(Patch_CalculateMaxHealth));
            harmony.PatchAll(typeof(Patch_CalculateBaseMaxHealth));

            Log.LogInfo("=================================================");
            Log.LogInfo("CustomStatMod v2.0.0 (BepInEx 6) Initialized!");
            Log.LogInfo($"Mod Enabled: {ModEnabled.Value}");
            Log.LogInfo($"Only Local Player: {OnlyAffectLocalPlayer.Value}");
            Log.LogInfo($"Attack Speed: {(AttackSpeed.Value == 0f ? "vanilla" : AttackSpeed.Value.ToString())}");
            Log.LogInfo($"Crit Chance: {(CritChance.Value == 0f ? "vanilla" : CritChance.Value.ToString())}");
            Log.LogInfo($"Spell Haste: {(SpellHaste.Value == 0f ? "vanilla" : SpellHaste.Value.ToString())}");
            Log.LogInfo($"Max Stamina: {(MaxStamina.Value == 0f ? "vanilla" : MaxStamina.Value.ToString())}");
            Log.LogInfo($"Max Concentration: {(MaxConcentration.Value == 0f ? "vanilla" : MaxConcentration.Value.ToString())}");
            Log.LogInfo($"Max Health: {(MaxHealth.Value == 0f ? "vanilla" : MaxHealth.Value.ToString())}");
            Log.LogInfo("=================================================");
        }

        public static bool IsTargetPlayer(ObjectsCommon obj)
        {
            if (obj == null) return false;

            var player = obj.TryCast<Player>();
            if (player == null) return false;

            if (OnlyAffectLocalPlayer != null && !OnlyAffectLocalPlayer.Value)
                return true;

            try { if (player.IsOwner) return true; } catch { }

            try
            {
                if (DataStorage.Singleton != null && DataStorage.Singleton.Player == player)
                    return true;
            }
            catch { }

            return false;
        }
    }

    // ============================================================
    // ATTACK SPEED
    // ============================================================

    [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateAttackSpeed))]
    public static class Patch_CalculateAttackSpeed
    {
        public static void Prefix(ObjectsCommon obj, ref float baseAttackSpeed, Attributes attributes)
        {
            try
            {
                if (CustomStatModPlugin.ModEnabled == null || !CustomStatModPlugin.ModEnabled.Value) return;
                if (!CustomStatModPlugin.IsTargetPlayer(obj)) return;
                float val = CustomStatModPlugin.AttackSpeed?.Value ?? 0f;
                if (val != 0f) baseAttackSpeed = val;
            }
            catch (Exception ex) { CustomStatModPlugin.Log?.LogError($"[Patch_CalculateAttackSpeed] {ex}"); }
        }
    }

    [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateBaseAttackSpeed))]
    public static class Patch_CalculateBaseAttackSpeed
    {
        public static void Postfix(Player player, ref float __result)
        {
            try
            {
                if (CustomStatModPlugin.ModEnabled == null || !CustomStatModPlugin.ModEnabled.Value) return;
                if (!CustomStatModPlugin.IsTargetPlayer(player)) return;
                float val = CustomStatModPlugin.AttackSpeed?.Value ?? 0f;
                if (val != 0f) __result = val;
            }
            catch (Exception ex) { CustomStatModPlugin.Log?.LogError($"[Patch_CalculateBaseAttackSpeed] {ex}"); }
        }
    }

    // ============================================================
    // CRITICAL CHANCE
    // ============================================================

    [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateCriticalRate))]
    public static class Patch_CalculateCriticalRate
    {
        public static void Postfix(ObjectsCommon obj, ref float __result)
        {
            try
            {
                if (CustomStatModPlugin.ModEnabled == null || !CustomStatModPlugin.ModEnabled.Value) return;
                if (!CustomStatModPlugin.IsTargetPlayer(obj)) return;
                float val = CustomStatModPlugin.CritChance?.Value ?? 0f;
                if (val != 0f) __result = val;
            }
            catch (Exception ex) { CustomStatModPlugin.Log?.LogError($"[Patch_CalculateCriticalRate] {ex}"); }
        }
    }

    // ============================================================
    // SPELL HASTE
    // ============================================================

    [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateSpellHaste))]
    public static class Patch_CalculateSpellHaste
    {
        public static void Postfix(ObjectsCommon obj, ref float __result)
        {
            try
            {
                if (CustomStatModPlugin.ModEnabled == null || !CustomStatModPlugin.ModEnabled.Value) return;
                if (!CustomStatModPlugin.IsTargetPlayer(obj)) return;
                float val = CustomStatModPlugin.SpellHaste?.Value ?? 0f;
                if (val != 0f) __result = val;
            }
            catch (Exception ex) { CustomStatModPlugin.Log?.LogError($"[Patch_CalculateSpellHaste] {ex}"); }
        }
    }

    [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateBaseSpellHaste))]
    public static class Patch_CalculateBaseSpellHaste
    {
        public static void Postfix(Player player, ref float __result)
        {
            try
            {
                if (CustomStatModPlugin.ModEnabled == null || !CustomStatModPlugin.ModEnabled.Value) return;
                if (!CustomStatModPlugin.IsTargetPlayer(player)) return;
                float val = CustomStatModPlugin.SpellHaste?.Value ?? 0f;
                if (val != 0f) __result = val;
            }
            catch (Exception ex) { CustomStatModPlugin.Log?.LogError($"[Patch_CalculateBaseSpellHaste] {ex}"); }
        }
    }

    // ============================================================
    // MAX STAMINA
    // ============================================================

    [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateMaxStamina))]
    public static class Patch_CalculateMaxStamina
    {
        public static void Postfix(ObjectsCommon obj, ref float __result)
        {
            try
            {
                if (CustomStatModPlugin.ModEnabled == null || !CustomStatModPlugin.ModEnabled.Value) return;
                if (!CustomStatModPlugin.IsTargetPlayer(obj)) return;
                float val = CustomStatModPlugin.MaxStamina?.Value ?? 0f;
                if (val != 0f) __result = val;
            }
            catch (Exception ex) { CustomStatModPlugin.Log?.LogError($"[Patch_CalculateMaxStamina] {ex}"); }
        }
    }

    [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateBaseMaxStamina))]
    public static class Patch_CalculateBaseMaxStamina
    {
        public static void Postfix(Player player, ref float __result)
        {
            try
            {
                if (CustomStatModPlugin.ModEnabled == null || !CustomStatModPlugin.ModEnabled.Value) return;
                if (!CustomStatModPlugin.IsTargetPlayer(player)) return;
                float val = CustomStatModPlugin.MaxStamina?.Value ?? 0f;
                if (val != 0f) __result = val;
            }
            catch (Exception ex) { CustomStatModPlugin.Log?.LogError($"[Patch_CalculateBaseMaxStamina] {ex}"); }
        }
    }

    // ============================================================
    // MAX CONCENTRATION
    // ============================================================

    [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateMaxConcentration))]
    public static class Patch_CalculateMaxConcentration
    {
        public static void Postfix(ObjectsCommon obj, ref float __result)
        {
            try
            {
                if (CustomStatModPlugin.ModEnabled == null || !CustomStatModPlugin.ModEnabled.Value) return;
                if (!CustomStatModPlugin.IsTargetPlayer(obj)) return;
                float val = CustomStatModPlugin.MaxConcentration?.Value ?? 0f;
                if (val != 0f) __result = val;
            }
            catch (Exception ex) { CustomStatModPlugin.Log?.LogError($"[Patch_CalculateMaxConcentration] {ex}"); }
        }
    }

    [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateBaseMaxConcentration))]
    public static class Patch_CalculateBaseMaxConcentration
    {
        public static void Postfix(Player player, ref float __result)
        {
            try
            {
                if (CustomStatModPlugin.ModEnabled == null || !CustomStatModPlugin.ModEnabled.Value) return;
                if (!CustomStatModPlugin.IsTargetPlayer(player)) return;
                float val = CustomStatModPlugin.MaxConcentration?.Value ?? 0f;
                if (val != 0f) __result = val;
            }
            catch (Exception ex) { CustomStatModPlugin.Log?.LogError($"[Patch_CalculateBaseMaxConcentration] {ex}"); }
        }
    }

    // ============================================================
    // MAX HEALTH
    // ============================================================

    [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateMaxHealth))]
    public static class Patch_CalculateMaxHealth
    {
        public static void Postfix(ObjectsCommon obj, ref float __result)
        {
            try
            {
                if (CustomStatModPlugin.ModEnabled == null || !CustomStatModPlugin.ModEnabled.Value) return;
                if (!CustomStatModPlugin.IsTargetPlayer(obj)) return;
                float val = CustomStatModPlugin.MaxHealth?.Value ?? 0f;
                if (val != 0f) __result = val;
            }
            catch (Exception ex) { CustomStatModPlugin.Log?.LogError($"[Patch_CalculateMaxHealth] {ex}"); }
        }
    }

    [HarmonyPatch(typeof(Formulas), nameof(Formulas.CalculateBaseMaxHealth))]
    public static class Patch_CalculateBaseMaxHealth
    {
        public static void Postfix(Player player, ref float __result)
        {
            try
            {
                if (CustomStatModPlugin.ModEnabled == null || !CustomStatModPlugin.ModEnabled.Value) return;
                if (!CustomStatModPlugin.IsTargetPlayer(player)) return;
                float val = CustomStatModPlugin.MaxHealth?.Value ?? 0f;
                if (val != 0f) __result = val;
            }
            catch (Exception ex) { CustomStatModPlugin.Log?.LogError($"[Patch_CalculateBaseMaxHealth] {ex}"); }
        }
    }
}
