using System;
using Il2CppInterop.Runtime;

namespace EquipmentStatEditor.Core
{
    /// <summary>
    /// [2026-09-27 12:23] Added: runtime lookup of the game's per-stat value ceiling.
    ///
    /// [2026-09-27 15:02] Changed: this now calls the game's OWN
    /// <c>GearLegality.TryStatValueCeiling</c> through the IL2CPP reflection API and returns the
    /// exact live ceiling, instead of replicating its formula. Why: the old formula copy
    ///   cap = 2 * CalculateFinalStatValue( CalculateStatBaseValue(stat, stars = 2), level ) + 2
    /// (level 12 primary / 5 secondary, +2 = GameConfig.ObtainableStatValueTolerance) was correct
    /// for the build the IL dump was made from, but the game updated afterwards and the hard-coded
    /// additive constant (and possibly the level constants) no longer matched. A user could not
    /// save values equal to the replicated cap, while values at exactly 2*CalculateFinalStatValue
    /// saved fine - the live addend is actually 0.05 (verified from the current GameAssembly:
    /// `addss xmm0,[rel ...]` targets 0x3D4CCCCD), not GameConfig.ObtainableStatValueTolerance (2).
    /// Reading the value from the game removes the guesswork and stays correct across updates.
    ///
    /// The formula is kept as <see cref="TryGetCapFormula"/> and used only if the native method
    /// cannot be resolved/invoked; its fallback tolerance is 0 (the conservative, save-safe value
    /// observed in-game), so a fallback never overshoots the real ceiling.
    /// </summary>
    public static class StatCapCatalog
    {
        private const int PrimaryLevel = 12;
        private const int SecondaryLevel = 5;
        // [2026-09-27 15:02] Changed: 2f -> 0f. The +2 was GameConfig.ObtainableStatValueTolerance
        // in the pre-update build; the live game rejects 2*final+2, so the fallback must not add it.
        /* [2026-09-27 15:02] Obsolete: assumed GameConfig.ObtainableStatValueTolerance = 2f.
           That field exists but is NOT the addend in TryStatValueCeiling. Reading the current
           game binary confirmed the real addend is 0.05f.
        private const float Tolerance = 2f;   // GameConfig.ObtainableStatValueTolerance
        */
        // [2026-09-27 15:10] Changed 0f -> 0.05f: the live build's addend (verified from the
        // current GameAssembly: `addss xmm0,[rel ...]` targets 0x3D4CCCCD = 0.05).
        private const float Tolerance = 0.05f; // 2*roll + 0.05 (exact fallback)
        private const int CapStars = 2;       // TryStatValueCeiling hardcodes Stars index 2

        // ---- native GearLegality.TryStatValueCeiling access ------------------------------------
        private static IntPtr _ceilingMethod;
        private static bool _ceilingResolutionAttempted;

        /// <summary>
        /// [2026-09-27 15:02] Added: resolve the game's static
        /// <c>GearLegality.TryStatValueCeiling(Runes.Stat, bool, out float)</c> method once.
        /// GearLegality is internal (not emitted to the interop assembly), so it is looked up by
        /// name through the IL2CPP class API.
        /// [2026-09-29 10:29] Clarification: the GearLegality CLASS is actually present in the
        /// current interop snapshot (verified by decompiling BepInEx/interop/Assembly-CSharp.dll);
        /// it is the TryStatValueCeiling METHOD that is private, which is why this IL2CPP route is
        /// used here.
        /// [2026-09-30 02:47] Changed: the public GearLegality.Inspect patch (item-destruction
        /// bypass) is no longer in this mod - it moved to the standalone AntiCheatBypassMod
        /// (GearLegalityGuard.cs removed here).
        /// /* [2026-09-30 02:47] Obsolete: "Core/GearLegalityGuard.cs patches the public Inspect via
        /// Harmony instead." - superseded by the standalone AntiCheatBypassMod. */
        /// </summary>
        private static IntPtr ResolveCeilingMethod()
        {
            if (_ceilingResolutionAttempted) return _ceilingMethod;
            _ceilingResolutionAttempted = true;
            try
            {
                IntPtr klass = IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "", "GearLegality");
                if (klass == IntPtr.Zero) return _ceilingMethod = IntPtr.Zero;
                try { IL2CPP.il2cpp_runtime_class_init(klass); } catch { }
                _ceilingMethod = IL2CPP.il2cpp_class_get_method_from_name(klass, "TryStatValueCeiling", 3);
            }
            catch { _ceilingMethod = IntPtr.Zero; }
            return _ceilingMethod;
        }

        /// <summary>
        /// [2026-09-27 15:02] Added: invoke the native ceiling method. Returns false on any failure
        /// (method unavailable, managed exception) so the caller can fall back to the formula.
        /// <paramref name="ceiling"/> is 0 when the game has no ceiling for the stat.
        /// </summary>
        private static unsafe bool TryInvokeCeiling(Runes.Stat stat, bool isPrimary, out float ceiling)
        {
            ceiling = 0f;
            IntPtr method = ResolveCeilingMethod();
            if (method == IntPtr.Zero) return false;

            int statValue = (int)stat;
            bool primary = isPrimary;
            float result = 0f;
            IntPtr* p = stackalloc IntPtr[3];
            p[0] = (IntPtr)(&statValue);
            p[1] = (IntPtr)(&primary);
            p[2] = (IntPtr)(&result);

            IntPtr exc = IntPtr.Zero;
            IL2CPP.il2cpp_runtime_invoke(method, IntPtr.Zero, (void**)p, ref exc);
            if (exc != IntPtr.Zero) return false;

            ceiling = result;
            return true;
        }

        public static bool TryGetCap(Runes.Stat stat, bool isPrimary, out float cap)
        {
            cap = 0f;

            // [2026-09-27 15:02] Added: prefer the game's own answer. A positive ceiling is the
            // real cap; 0 means the game caps nothing for this stat (fall through to the formula,
            // which returns false for those stats too).
            try
            {
                if (TryInvokeCeiling(stat, isPrimary, out float nativeCap))
                {
                    if (nativeCap > 0f && nativeCap < 100000f &&
                        !float.IsNaN(nativeCap) && !float.IsInfinity(nativeCap))
                    {
                        cap = nativeCap;
                        return true;
                    }
                }
            }
            catch { }

            return TryGetCapFormula(stat, isPrimary, out cap);
        }

        // ---- [2026-09-29 10:29] Added: raised-cap ("Bypass") mode support ----------------------
        // Design approved 2026-09-29: the raised mode multiplies the ceiling by 99x the PER-STAT
        // vanilla ceiling (proportional across stats - HP keeps its large scale, Crit keeps its
        // small scale). Stats the game leaves uncapped stay free input in both modes.
        // TryGetCap stays vanilla on purpose: StatRollTableDump records the reference table and
        // must not change with the mode. UI + Apply validation call TryGetEffectiveCap instead.
        // [2026-09-30 02:47] Renamed: Safe/Bypass -> Vanilla/Modded caps. The anti-cheat bypass
        // (game item destruction) is no longer this mod's job - it is owned unconditionally by the
        // separate standalone AntiCheatBypassMod - so this flag now ONLY chooses the ceiling.
        // /* [2026-09-29 10:29] Obsolete: Bypass-mode naming (superseded by Modded caps).
        // /// <summary>Multiplier applied to the vanilla ceiling while Bypass mode is active.</summary>
        // public const float BypassCapMultiplier = 99f;
        // /// <summary>
        // /// Live mode flag. Owned by EquipmentStatEditorPlugin (config "General.BypassMode");
        // /// switching it also patches/unpatches GearLegalityGuard.
        // /// </summary>
        // public static bool BypassMode;
        // */

        /// <summary>Multiplier applied to the vanilla ceiling while Modded caps are active.</summary>
        public const float ModdedCapMultiplier = 99f;

        /// <summary>
        /// Live flag. Owned by EquipmentStatEditorPlugin (config "General.UseModdedCaps");
        /// false = vanilla ceilings, true = 99x vanilla ceilings.
        /// </summary>
        public static bool ModdedCaps;

        /// <summary>
        /// Cap used by the UI and Apply validation: the vanilla ceiling when Modded caps are off,
        /// vanilla x <see cref="ModdedCapMultiplier"/> when on. Returns false for stats the game
        /// leaves uncapped (free input in both modes).
        /// </summary>
        public static bool TryGetEffectiveCap(Runes.Stat stat, bool isPrimary, out float cap)
        {
            if (!TryGetCap(stat, isPrimary, out cap)) return false;
            // [2026-09-30 02:47] Changed: BypassMode/BypassCapMultiplier -> ModdedCaps/ModdedCapMultiplier.
            if (ModdedCaps) cap *= ModdedCapMultiplier;
            return true;
        }

        /// <summary>
        /// [2026-09-27 15:02] Added (was the body of TryGetCap): formula fallback used only when the
        /// native method is unavailable. Kept for safety if a future update renames the method.
        /// </summary>
        private static bool TryGetCapFormula(Runes.Stat stat, bool isPrimary, out float cap)
        {
            cap = 0f;
            try
            {
                RuneManager rm = RuneManager.Singleton;
                if (rm == null || rm.BaselineValues == null || rm.ScalingValues == null) return false;
                if (!rm.BaselineValues.ContainsKey(stat) || !rm.ScalingValues.ContainsKey(stat)) return false;

                float baseStars2 = rm.CalculateStatBaseValue(stat, (Runes.Stars)CapStars);
                if (baseStars2 <= 0f) return false;

                float roll = rm.CalculateFinalStatValue(baseStars2, isPrimary ? PrimaryLevel : SecondaryLevel);
                if (roll <= 0f || float.IsNaN(roll) || float.IsInfinity(roll)) return false;

                cap = 2f * roll + Tolerance;
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// [2026-09-27 12:37] Added: the value the game naturally gives a freshly-rolled secondary
        /// affix at <paramref name="stars"/> stars, i.e. <c>CalculateStatBaseValue(stat, stars)</c>
        /// (= baseline * (1 + 0.33*stars)).
        ///
        /// Verified against 693 real secondary rolls (EquipmentStatEditor.Yoink/chonks.cfg): the
        /// un-upgraded value is exactly this base for 626 of them, and the ~1.33 variants are the
        /// Two-star runes (star multiplier). The rarer ~4x values are the fully-upgraded rolls
        /// (StatUpgrades tracks those separately), so a new affix (tier 0) should seed at the base.
        /// Returns false for stats the game has no baseline for (the uncapped set).
        /// </summary>
        public static bool TryGetNatural(Runes.Stat stat, int stars, out float value)
        {
            value = 0f;
            try
            {
                RuneManager rm = RuneManager.Singleton;
                if (rm == null || rm.BaselineValues == null) return false;
                if (!rm.BaselineValues.ContainsKey(stat)) return false;

                if (stars < 0) stars = 0;
                if (stars > 8) stars = 8;

                float v = rm.CalculateStatBaseValue(stat, (Runes.Stars)stars);
                if (v <= 0f || float.IsNaN(v) || float.IsInfinity(v)) return false;

                value = v;
                return true;
            }
            catch { return false; }
        }
    }
}
