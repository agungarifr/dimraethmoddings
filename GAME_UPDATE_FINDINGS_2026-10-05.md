# Dimraeth Game Update Findings — 2026-10-05 Hotfix

**Investigation Date:** 2026-10-05  
**Scope:** Anticheat system changes + Equipment/Runes sanity checks  
**Source:** `modding/BepInExModsSource` (CodeGraph-indexed mod source)

---

## Executive Summary

The **2026-10-05 hotfix** introduced a **second, independent rune-correction engine** that runs alongside the original `GearLegality.Inspect` path. This engine silently reverts edited/over-capped gear to vanilla values and destroys "contraband" runes. The standalone `AntiCheatBypassMod` has been iterated from **v1.0.0 → v1.4.0** in response, with v1.4.0 being the stable version that neutralizes the new engine without crashing.

---

## 1. Anticheat Changes (2026-10-05 Hotfix)

### 1.1 New "Inflated Numbers Corrected" Engine

| Component | New Methods/Types | Purpose |
|-----------|-------------------|---------|
| **GearLegality** | `InspectStats` (private core check), `ExceedsValueCeiling`, `IsStatAllowedOnSlot`, `IsContraband`, `IsChargeContraband`, `CorrectIfOverstated`, `CorrectOverstated`, `CollectContraband`, `DestroyIfUnobtainable`, `DestroyIfChargesUnobtainable` | Core validation + correction/destruction pipeline |
| **QARune** (QA spawner) | `ContrabandPreflight`, `Corrupt` | Developer tool that *creates* contraband for testing — **not** the correction engine |
| **SaveSystem** | `RepairRuneCounts` | Repairs rune secondary-stat counts on load |
| **Player** | `PeriodicRuneIntegrityCheck` (coroutine), `PeriodicAntiCheatCheck` (coroutine) | Periodic detection → correction/destruction loops |
| **Player** (bootstrap) | `InitializeAntiCheat`, `SubscribeAntiCheatValueChangedHooks`, `InitializeOwnerAntiCheatClientRpc` | Arms the new engine's coroutines/value tripwires |

### 1.2 How the New Engine Works

```
GearLegality.InspectStats (core check)
    ├─ ExceedsValueCeiling(stat, value) → bool  (true = over cap)
    ├─ IsStatAllowedOnSlot(stat, slot) → bool   (false = disallowed)
    ├─ IsContraband(rune) → bool                (true = contraband)
    └─ IsChargeContraband(rune) → bool          (true = charge contraband)
         │
         ▼
CorrectIfOverstated(ref Rune, ...) / CorrectOverstated(List<Rune>, ...)
         │
         ▼
CollectContraband → DestroyIfUnobtainable / DestroyIfChargesUnobtainable
```

**Key insight:** `InspectStats` is the **single choke point** — both the public `Inspect` and the corrector call it. Forcing it to `Violation.None` stops the rewrite at its source.

---

## 2. AntiCheatBypassMod Evolution (v1.0.0 → v1.4.0)

| Version | Date | Changes | Stability |
|---------|------|---------|-----------|
| **v1.0.0** | 2026-09-29 | Basic bypass: `GearLegality.Inspect`, `CharacterPlausibility`, `Player` validators, kick/save-block/telemetry | ✅ Stable |
| **v1.1.0** | 2026-10-05 | Added bypass for hotfix engine: `ContrabandPreflight`, `Corrupt`, `RepairRuneCounts`, `PeriodicRuneIntegrityCheck`, `PeriodicAntiCheatCheck` | ✅ Stable |
| **v1.2.0** | 2026-10-05 | Neutralized bootstrap: `InitializeAntiCheat`, `SubscribeAntiCheatValueChangedHooks`, `InitializeOwnerAntiCheatClientRpc` | ✅ Stable |
| **v1.3.0** | 2026-10-05 | **Retargeted to REAL corrector**: `GearLegality.InspectStats`, `CorrectIfOverstated`, `CorrectOverstated`, `CollectContraband`; dropped wrong QARune targets; re-enabled `InitializeAntiCheat` (for `_runeSignatureStore`) | ❌ **Hard crash in coreclr.dll** (0xc0000005) right after player spawn |
| **v1.4.0** | 2026-10-05 | **Dropped all byref-Rune targets** (`CorrectIfOverstated`, `CorrectOverstated`, `CollectContraband`, `DestroyIfUnobtainable`, `DestroyIfChargesUnobtainable`); neutralized via **by-value checks only**: `InspectStats→None`, `ExceedsValueCeiling→false`, `IsStatAllowedOnSlot→true`, `IsContraband→false`, `IsChargeContraband→false`; **re-skipped `InitializeAntiCheat`** | ✅ **Stable** |

### 2.3 Current v1.4.0 Patch Table (Active)

```csharp
// GearLegality — item destruction + correction engine
PatchByName("GearLegality", "Inspect",           InspectPrefix);           // Violation.None
PatchByName("GearLegality", "InspectStats",      InspectPrefix);           // Violation.None (CORE)
PatchByName("GearLegality", "ExceedsValueCeiling", FalseResultPrefix);     // false = not over cap
PatchByName("GearLegality", "IsStatAllowedOnSlot", TrueResultPrefix);      // true = allowed
PatchByName("GearLegality", "IsContraband",       FalseResultPrefix);      // false = not contraband
PatchByName("GearLegality", "IsChargeContraband", FalseResultPrefix);      // false = not charge contraband
// DROPPED (byref, crash trigger): CorrectIfOverstated, CorrectOverstated, CollectContraband, DestroyIfUnobtainable, DestroyIfChargesUnobtainable

// CharacterPlausibility — hide saves + multiplayer join gate
PatchByName("CharacterPlausibility", "FilterImplausible",      FilterImplausiblePrefix);   // return input list untouched
PatchByName("CharacterPlausibility", "IsImplausible",          FalseReasonPrefix);         // false = plausible
PatchByName("CharacterPlausibility", "RejectsJoiningCharacter", FalseReasonPrefix);        // false = allow join

// CharacterIdentityFromSkillTree — skill point reconciliation
PatchByName("CharacterIdentityFromSkillTree", "Reconcile", ReconcilePrefix);   // false = nothing reconciled

// Player validators — always "clean"
PatchByName("Player", "ValidateXPState",                   TrueResultPrefix);
PatchByName("Player", "ValidateXPInvariants",              TrueResultPrefix);
PatchByName("Player", "ValidateRuneState",                 TrueResultPrefix);
PatchByName("Player", "ValidateAttributeXPConsistency",    SkipPrefix);

// Enforcement — no kick, no save-block, no telemetry
PatchByName("Player", "KickForCheat",                      SkipPrefix);
PatchByName("Player", "BlockSaveDueToCheatClientRpc",      SkipPrefix);
PatchByName("Player", "ReportLocalCheatServerRpc",         SkipPrefix);

// Skill points — never claw back
PatchByName("SkillTree", "TryRepairOverGrantedSkillPoints", TryRepairPrefix);

// Ban list
PatchByName("BanListManager", "IsBanned",                  FalseResultPrefix);
PatchByName("BanListData", "IsBanned",                     FalseResultPrefix);

// Telemetry
PatchByName("GameAnalyticsManager", "OnCheatDetected",     SkipPrefix);
PatchByName("GameAnalyticsManager", "CheatAttempted",      SkipPrefix);

// Obfuscated network values
PatchByName("ObfuscatedNetworkInt", "RaiseIntegrityViolation", SkipPrefix);
PatchByName("ObfuscatedNetworkInt", "RepairBaseValue",          SkipPrefix);
PatchByName("ObfuscatedNetworkFloat", "RepairBaseValue",        SkipPrefix);

// Speedhack + periodic coroutines
PatchCoroutineMoveNext("Player", "SpeedHackDetectionRoutine",     FalseResultPrefix);
PatchCoroutineMoveNext("Player", "PeriodicRuneIntegrityCheck",    FalseResultPrefix);
PatchCoroutineMoveNext("Player", "PeriodicAntiCheatCheck",        FalseResultPrefix);

// Bootstrap — skipped again (v1.2.0 style) to avoid coreclr crash
PatchByName("Player", "InitializeAntiCheat",                    SkipPrefix);
PatchByName("Player", "SubscribeAntiCheatValueChangedHooks",    SkipPrefix);
PatchByName("Player", "InitializeOwnerAntiCheatClientRpc",      SkipPrefix);
```

---

## 3. Equipment / Runes Sanity Checks

### 3.1 GearLegality (Item Destruction & Correction)

- **Choke point:** `GearLegality.Inspect(Rune) → Violation` — **every** destruction path funnels here:
  - `Inventory.LoadInventory()` → `CollectContraband` (character/inventory load)
  - `Inventory.PurgeUnobtainableRunes` → `CollectContraband` (netcode RPC purge)
  - `Runes.Update()` → `UpdateRuneBonuses()` → `PurgeEquipped` (every tick, worn runes)
  - `Storage.SetStorageData()` → `PurgeContainer` (storage opened/set)
- **Destruction:** Anything `Inspect` flags `!= Violation.None` is destroyed and logged locally (`ClientDiagnostics.NoteContrabandDestroyed`).
- **Charge path:** `IsChargeContraband` / `DestroyIfChargesUnobtainable` — separate path for item charges (unedited by stat editors, so stays clean).

### 3.2 Stat Value Ceilings (The Real Sanity Check)

| Mechanism | Details |
|-----------|---------|
| **Native source** | `GearLegality.TryStatValueCeiling(Runes.Stat, bool isPrimary, out float)` — **private static method**, resolved via IL2CPP reflection |
| **Formula (fallback)** | `cap = 2 * CalculateFinalStatValue(CalculateStatBaseValue(stat, Stars.Two), level) + 0.05`<br>• Primary level = 12, Secondary level = 5<br>• Stars hardcoded to index 2 (Two-star)<br>• Tolerance = **0.05** (not `GameConfig.ObtainableStatValueTolerance` = 2) — verified from `GameAssembly.dll` (`addss xmm0, [rel 0x3D4CCCCD]`) |
| **Uncapped stats** | Stats not in `RuneManager.BaselineValues`/`ScalingValues` have no ceiling (free input) |

### 3.3 CharacterPlausibility (Character-Level Sanity)

| Ceiling | Value | Source |
|---------|-------|--------|
| Max Level | 37 | `CharacterPlausibility.ExceedsPlausibleCaps` |
| Max Attribute Total | 84 | Sum of 8 attributes |
| Max Skill Points Spent in Tree | 126 | Skill tree allocation |

**Effect:** Characters exceeding these are hidden from the character-select list (`FilterImplausible`) and rejected from multiplayer joins (`RejectsJoiningCharacter`).

### 3.4 SaveSystem Migration Guard

- **`SaveSystem.CurrentRuneFormulaVersion`** — must match the rune's `FormulaVersion` field, otherwise `SaveSystem.MigrateRuneFormulas` recomputes `StatValues` from the formula and **wipes custom magnitudes**.
- All editors (`EquippedStatModifier`, `EquipmentStatEditor`, `DimraethModPack`'s module) **force-write** `FormulaVersion = SaveSystem.CurrentRuneFormulaVersion` on every edit.
- `RepairRuneCounts` (new in hotfix) re-derives secondary-stat counts from `FixedList` lengths — same job as the migration.

### 3.5 EquipmentStatEditor / EquippedStatModifier — Cap Modes

| Mode | Ceiling | Item Destruction | Config Key |
|------|---------|------------------|------------|
| **Vanilla (Safe)** | `TryGetCap` (game native) | **Active** (patched by AntiCheatBypassMod) | `UseModdedCaps = false` |
| **Modded (Bypass)** | `TryGetCap * 99` (proportional: HP keeps large scale, Crit keeps small) | **Disabled** (patched by AntiCheatBypassMod) | `UseModdedCaps = true` |

> **Warning:** Items created above vanilla caps are destroyed by the game when the mod is removed or mode switches back to Vanilla. They only survive while `AntiCheatBypassMod` runs (and on your own hosted server).

---

## 4. Critical Files & Locations

| File | Purpose |
|------|---------|
| `AntiCheatBypassMod/AntiCheatBypassModPlugin.cs` | Main plugin, patch orchestration, version history |
| `AntiCheatBypassMod/AntiCheatBypassPatches.cs` | All Harmony prefix implementations |
| `EquipmentStatEditor/Core/StatCapCatalog.cs` | Native ceiling lookup (`TryStatValueCeiling`) + formula fallback + Modded caps (99×) |
| `EquipmentStatEditor/Core/GearLegalityGuard.cs` | **[OBSOLETE]** — was the editor's conditional `Inspect` patch; now owned unconditionally by `AntiCheatBypassMod` |
| `EquippedStatModifier/Core/RuneMemory.cs` | Raw memory rune read/write, `FormulaVersion` enforcement, `Rebaseline` (HMAC re-sign) |
| `DimraethModPack/Modules/System/AntiCheatBypassModule.cs` | **[OBSOLETE]** — modpack's old CharacterPlausibility-only bypass; replaced by standalone mod |
| `DimraethModPack/CHANGELOG.md` | Human-readable change log with dates |

---

## 5. Action Items / Verification Checklist

- [ ] Confirm `AntiCheatBypassMod` v1.4.0 is deployed to `BepInEx/plugins/`
- [ ] Verify `LogOutput.log` shows: `[AntiCheatBypass] Patched GearLegality.InspectStats.` and all other patches
- [ ] Verify character-select shows previously hidden (over-cap) characters
- [ ] Verify multiplayer join works with edited characters (host must run the mod)
- [ ] Test `EquipmentStatEditor` in **Modded caps** mode: write > vanilla cap, confirm item persists
- [ ] Test `EquipmentStatEditor` in **Vanilla caps** mode: write at vanilla cap, confirm item persists
- [ ] Confirm `SaveSystem.CurrentRuneFormulaVersion` is enforced on every edit (check logs for `Rebaseline`)

---

## 6. Notes for Future Updates

1. **Game updates may rename/remove `GearLegality` methods** — `PatchByName` logs warnings instead of aborting; check `LogOutput.log` for "not found" entries.
2. **`CharacterPlausibility` caps may change** — current ceilings (37/84/126) are hardcoded in the game; if they change, the bypass still works (it skips the check entirely).
3. **`SaveSystem.CurrentRuneFormulaVersion` may increment** — editors auto-read it at runtime, so no code change needed.
4. **If `coreclr.dll` crashes reappear** — the pattern is: byref-Rune Harmony prefixes on `GearLegality` corrector methods. Avoid them; use by-value predicate patches instead.

---

*Generated by CodeGraph exploration of `modding/BepInExModsSource` on 2026-10-05.*