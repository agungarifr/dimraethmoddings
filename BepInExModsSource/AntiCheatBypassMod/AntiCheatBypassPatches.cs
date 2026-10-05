namespace AntiCheatBypassMod
{
    /// <summary>
    /// [2026-09-29 21:36] New: Harmony prefixes for the standalone Anti-Cheat Bypass mod.
    ///
    /// Every prefix here is deliberately unconditional (no config, no enable flag): the mod's
    /// whole purpose is to make the game's anti-cheat a no-op, and it is deleted to undo it.
    ///
    /// Return-value polarity was verified against the current (2026-09-26) interop:
    ///   * ValidateXPState / ValidateXPInvariants / ValidateRuneState return <b>true = clean</b>
    ///     (the callers branch to KickForCheat only on false).
    ///   * IsImplausible / RejectsJoiningCharacter / IsBanned are already benign at <b>false</b>.
    ///   * CharacterIdentityFromSkillTree.Reconcile returns bool; false = "nothing reconciled".
    /// </summary>
    internal static class AntiCheatBypassPatches
    {
        // -------------------------------------------------------------------------------------
        // Gear / items (docs/anticheat/02): every item-purge path funnels through Inspect, so
        // forcing Violation.None here is the single choke point that stops item destruction.
        // Typed reference is fine because GearLegality is present in the interop snapshot.
        // -------------------------------------------------------------------------------------
        public static bool InspectPrefix(ref GearLegality.Violation __result)
        {
            __result = GearLegality.Violation.None;
            return false;
        }

        // [2026-10-05] OBSOLETE (v1.3.0): QARune.ContrabandPreflight / QARune.Corrupt are the QA
        // developer spawner that CREATES contraband runes for testing the corrector - they are
        // not the correction engine. Targeting them was a wrong guess (both logged "not found"
        // on GearLegality). Kept as a comment to preserve the trail; the real mutator is
        // GearLegality.CorrectIfOverstated / CorrectOverstated (below).
        // public static bool CorruptPrefix(Rune __0, ref Rune __result)
        // {
        //     __result = __0;
        //     return false;
        // }

        // [2026-10-05] New (v1.3.0): the 2026-10-05 hotfix's real "inflated numbers are corrected"
        // path is GearLegality.InspectStats (the private core check that both Inspect and the
        // corrector call) plus GearLegality.CorrectIfOverstated(ref Rune,...) and
        // CorrectOverstated(List<Rune>,...). The old Inspect-only bypass never reached them, which
        // is why a successful EquipmentStatEditor write showed vanilla stats again.
        // CorrectOverstated returns the number of runes it rewrote -> force 0.
        public static bool IntZeroResultPrefix(ref int __result)
        {
            __result = 0;
            return false;
        }

        // CollectContraband gathers offenders for the correction/destruction pass; return an empty
        // list so the pass has nothing to act on even if it runs off a path we do not patch.
        public static bool EmptyRuneListPrefix(ref Il2CppSystem.Collections.Generic.List<Rune> __result)
        {
            __result = new Il2CppSystem.Collections.Generic.List<Rune>();
            return false;
        }

        // -------------------------------------------------------------------------------------
        // Character plausibility (docs/anticheat/05)
        // -------------------------------------------------------------------------------------
        // FilterImplausible is the only place the game hides saves from the character list,
        // so returning the input list untouched keeps every character visible.
        public static bool FilterImplausiblePrefix(
            Il2CppSystem.Collections.Generic.List<PlayerSaveFile> __0,
            ref Il2CppSystem.Collections.Generic.List<PlayerSaveFile> __result)
        {
            __result = __0;
            return false;
        }

        // Shared by IsImplausible / RejectsJoiningCharacter / FailsIdentityCheck (all have an
        // `out string reason` and a bool result).
        public static bool FalseReasonPrefix(out string reason, ref bool __result)
        {
            reason = null;
            __result = false;
            return false;
        }

        // -------------------------------------------------------------------------------------
        // Character identity (docs/anticheat/04)
        // -------------------------------------------------------------------------------------
        // The current (2026-09-26) build replaced CharacterIdentityIntegrity (FailsIdentityCheck /
        // Stamp / IdentityVerdict) with CharacterIdentityFromSkillTree, whose Reconcile recomputes
        // and repairs a character's skill-point allocation from its skill-tree nodes. Skipping it
        // leaves cheated allocations untouched; false = "nothing reconciled".
        // [2026-09-30] Retargeted from the removed CharacterIdentityIntegrity.Reconcile (whose
        // benign result was IdentityVerdict.Intact == 0) after checking global-metadata.dat.
        public static bool ReconcilePrefix(out string note, ref bool __result)
        {
            note = null;
            __result = false;
            return false;
        }

        // -------------------------------------------------------------------------------------
        // Player XP / rune / attribute validation (docs/anticheat/03)
        // -------------------------------------------------------------------------------------
        // true = "clean" for the three validators (see class notes on polarity).
        public static bool TrueResultPrefix(ref bool __result)
        {
            __result = true;
            return false;
        }

        // false = benign for the predicates; for coroutine MoveNext it means "finished" so the
        // enforcement loop stops after its first step.
        public static bool FalseResultPrefix(ref bool __result)
        {
            __result = false;
            return false;
        }

        // Void enforcement methods: skipping them removes the kick / save-block / telemetry call.
        public static bool SkipPrefix()
        {
            return false;
        }

        // SkillTree.TryRepairOverGrantedSkillPoints: report "nothing to repair" so cheated
        // skill points are never clawed back.
        public static bool TryRepairPrefix(out int before, out int after, ref bool __result)
        {
            before = 0;
            after = 0;
            __result = false;
            return false;
        }
    }
}
