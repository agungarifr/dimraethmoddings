using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using UnityEngine;

namespace DimraethModPack.Modules.Pets
{
    /// <summary>
    /// Diagnostic module for the pet cargo (backpack slots 150..161).
    ///
    /// Background: the user reports the pet can only be *given* gear (runes/equipment) and not
    /// generic cargo. Static analysis of the IL2CPP dump found no such type gate:
    ///   * Inventory.TransferPetBoundary        -> no type predicate (any entry kind)
    ///   * Inventory.SendItemToPetStorage       -> no type predicate
    ///   * Inventory.SendHalfAcrossPetBoundary  -> requires InventoryEntry.IsItemEntry
    ///                                             (true for every item, false for runes/pets)
    ///   * Inventory.CanSendToPetStorage        -> only a destination/empty-slot/mode check
    ///   * PetManager.DepositCourierEntryServerRpc -> requires IsItemEntry (runes use the rune RPC)
    ///
    /// Because the reported block could not be reproduced from code, this module logs the exact
    /// classification and decision at every acceptance point (and can dump the live cargo slots)
    /// so a single in-game reproduction pinpoints the real restriction. It is fully non-destructive
    /// and gated by [Pets.PetCargo] Enabled / VerboseLog.
    /// </summary>
    public class PetCargoModule : ModModuleBase
    {
        public static PetCargoModule Instance { get; private set; }

        // [2026-10-01 14:00] Category is "Loot" (not "Pets") because ModPackUI has a fixed
        // 4-tab layout ("Stats","Gameplay","Loot","System"); a new tab would never render.
        public override string Category => "Loot";
        public override string Name => "Pet Cargo Inspector";
        public override string Description =>
            "Logs every pet-cargo transfer/deposit decision and item classification so the real " +
            "'gear only' restriction can be located. Non-destructive diagnostics.";

        public ConfigEntry<bool> VerboseLog;
        public ConfigEntry<bool> LogStorageRecords;

        // Captured from any pet-cargo patch so the UI can dump the live slots.
        internal static Inventory LastInventory;

        private string _status = "";

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            const string sec = "Pets.PetCargo";

            Enabled = config.Bind(sec, "Enabled", true,
                "Enable the pet-cargo diagnostic module (Default: true)");

            VerboseLog = config.Bind(sec, "VerboseLog", true,
                "Log each pet-cargo transfer/deposit decision and entry classification (Default: true)");

            LogStorageRecords = config.Bind(sec, "LogStorageRecords", true,
                "Log chest StorageData/record identity + slot counts around pet-courier delivery and chest open " +
                "(for the item-loss-after-restart investigation; Default: true)");
        }

        public override void ApplyPatches(Harmony harmony)
        {
            harmony.PatchAll(typeof(PetCargoPatches));
            DiagnosticsManager.Log("PetCargo", "Pet Cargo Inspector patches applied.");
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;
            curY += DrawToggle(x, curY, width, "Pet Cargo Diagnostics", Enabled, "logs pet transfer decisions", labelStyle, btnStyle);
            curY += DrawToggle(x, curY, width, "Verbose Log", VerboseLog, "log every decision", labelStyle, btnStyle);
            curY += DrawToggle(x, curY, width, "Log Storage Records", LogStorageRecords, "chest slot counts / record identity", labelStyle, btnStyle);

            if (GUI.Button(new Rect(x, curY, 240f, 24f), "Dump current pet cargo (log)", btnStyle))
            {
                _status = DumpPetCargo();
            }
            GUI.Label(new Rect(x + 248f, curY, width - 248f, 24f), _status, labelStyle);
            curY += 28f;

            GUI.Label(new Rect(x, curY, width, 20f),
                "<color=#888888>Reproduce: open the pet cargo and try to give it a non-gear item; the log will show which path rejected it.</color>",
                labelStyle);
            curY += 24f;
            return curY - y;
        }

        internal static string Describe(InventoryEntry e)
        {
            try
            {
                if (e.IsEmpty) return "empty";
                if (e.IsItemEntry) return $"ITEM kind={e.Kind} sub={e.Subkind} item={e.Item} amt={e.Amount}";
                if (e.IsRune) return $"RUNE amt={e.Amount} uuid={e.RuneUUID}";
                if (e.IsPet) return $"PET id={e.PetID}";
                return $"OTHER kind={e.Kind} sub={e.Subkind} item={e.Item} amt={e.Amount}";
            }
            catch
            {
                return "?";
            }
        }

        internal static void LogDecision(string method, int slot, InventoryEntry e, string extra = null)
        {
            if (Instance == null || !Instance.IsEnabled) return;
            if (Instance.VerboseLog == null || !Instance.VerboseLog.Value) return;

            string suffix = string.IsNullOrEmpty(extra) ? string.Empty : " " + extra;
            DiagnosticsManager.Log("PetCargo", $"{method}(slot={slot}) entry={Describe(e)}{suffix}");
        }

        internal static void LogRaw(string message)
        {
            if (Instance == null || !Instance.IsEnabled) return;
            DiagnosticsManager.Log("PetCargo", message);
        }

        // ---- item-loss-after-restart diagnostics (see ItemLoss_ForensicReport_2026-10-01.md) ----
        // These answer the open question: is the chest record the courier deposits into the SAME
        // object that is later saved/loaded, and do Entries/StorageSize stay in lockstep with the
        // live Storage.Inventory?

        private static bool StorageLogEnabled =>
            Instance != null && Instance.IsEnabled &&
            Instance.LogStorageRecords != null && Instance.LogStorageRecords.Value;

        internal static string DescribeStorageData(StorageData d)
        {
            if (d == null) return "<null>";
            try
            {
                return $"{{name={d.StorageName} size={d.StorageSize} entries={d.Entries?.Count ?? -1} " +
                       $"runes={d.Runes?.Count ?? -1} players={d.PlayersOpened?.Count ?? -1}}}";
            }
            catch (Exception ex)
            {
                return $"<err {ex.Message}>";
            }
        }

        internal static void LogStorageData(string context, StorageData d)
        {
            if (!StorageLogEnabled) return;
            DiagnosticsManager.Log("PetCargo", $"[{context}] {DescribeStorageData(d)}");
        }

        internal static void LogLiveStorage(string context, Storage live)
        {
            if (!StorageLogEnabled) return;
            string inv = "<null>", runes = "<null>";
            try { if (live?.Inventory != null) inv = live.Inventory.Count.ToString(); } catch { }
            try { if (live?.Runes != null) runes = live.Runes.Count.ToString(); } catch { }
            DiagnosticsManager.Log("PetCargo",
                $"[{context}] liveStorage data={DescribeStorageData(live?.StorageData)} inventory.Count={inv} runes.Count={runes}");
        }

        internal static void LogDeliverable(bool result, StorageData record, Storage live, int slotLimit)
        {
            if (!StorageLogEnabled) return;
            DiagnosticsManager.Log("PetCargo",
                $"[TryResolveDeliverable result={result} slotLimit={slotLimit}] record={DescribeStorageData(record)} " +
                $"liveData={DescribeStorageData(live?.StorageData)} sameObject={ReferenceEquals(record, live?.StorageData)}");
        }

        private string DumpPetCargo()
        {
            var inv = LastInventory;
            if (inv == null)
                return "<color=#FFAA55>No Inventory yet (open the backpack/pet cargo first).</color>";

            try
            {
                DiagnosticsManager.Log("PetCargo", "--- pet cargo dump: slots 150-161 ---");
                int nonEmpty = 0;
                for (int s = 150; s < 162; s++)
                {
                    var e = inv.GetInventoryEntryForGlobalSlot(s);
                    bool empty = e.IsEmpty;
                    if (!empty) nonEmpty++;
                    DiagnosticsManager.Log("PetCargo", $"slot {s}: {Describe(e)}");
                }
                return $"<color=#55FF55>Dumped 12 slots ({nonEmpty} non-empty) to log.</color>";
            }
            catch (Exception ex)
            {
                return $"<color=#FF5555>Dump failed: {ex.Message}</color>";
            }
        }
    }

    internal static class PetCargoPatches
    {
        [HarmonyPatch(typeof(Inventory), "SendHalfAcrossPetBoundary")]
        [HarmonyPrefix]
        private static void Pre_SendHalfAcrossPetBoundary(Inventory __instance, int sourceSlot)
        {
            PetCargoModule.LastInventory = __instance;
            InventoryEntry e = default;
            try { e = __instance.GetInventoryEntryForGlobalSlot(sourceSlot); } catch { }
            // Vanilla proceeds only when IsItemEntry && Amount/2 >= 1; log so we can see the decision.
            PetCargoModule.LogDecision("SendHalfAcrossPetBoundary", sourceSlot, e,
                $"half={e.Amount / 2} passesIsItemEntry={e.IsItemEntry}");
        }

        [HarmonyPatch(typeof(Inventory), "SendItemToPetStorage")]
        [HarmonyPrefix]
        private static void Pre_SendItemToPetStorage(Inventory __instance, int sourceSlot)
        {
            PetCargoModule.LastInventory = __instance;
            InventoryEntry e = default;
            try { e = __instance.GetInventoryEntryForGlobalSlot(sourceSlot); } catch { }
            // Vanilla has no type gate here; it just swaps into an empty pet slot.
            PetCargoModule.LogDecision("SendItemToPetStorage", sourceSlot, e);
        }

        [HarmonyPatch(typeof(Inventory), "TransferPetBoundary")]
        [HarmonyPrefix]
        private static void Pre_TransferPetBoundary(Inventory __instance, int sourceOverride, bool singleUnit)
        {
            PetCargoModule.LastInventory = __instance;
            InventoryEntry e = default;
            if (sourceOverride >= 0)
            {
                try { e = __instance.GetInventoryEntryForGlobalSlot(sourceOverride); } catch { }
            }
            PetCargoModule.LogDecision("TransferPetBoundary", sourceOverride, e, $"singleUnit={singleUnit}");
        }

        [HarmonyPatch(typeof(Inventory), "CanSendToPetStorage")]
        [HarmonyPostfix]
        private static void Post_CanSendToPetStorage(Inventory __instance, ref bool __result)
        {
            PetCargoModule.LastInventory = __instance;
            PetCargoModule.LogRaw($"CanSendToPetStorage -> {__result}");
        }

        [HarmonyPatch(typeof(Inventory), "SendPetItemsToStorage")]
        [HarmonyPostfix]
        private static void Post_SendPetItemsToStorage(Inventory __instance, ref int __result)
        {
            PetCargoModule.LogRaw($"SendPetItemsToStorage -> deposited {__result} entr(y/ies).");
        }

        [HarmonyPatch(typeof(PetManager), "DepositCourierEntryServerRpc")]
        [HarmonyPrefix]
        private static void Pre_DepositCourierEntry(PetManager __instance, InventoryEntry entry)
        {
            if (entry.IsEmpty) return;
            // This is the one place vanilla silently drops non-item cargo (no return-to-sender).
            if (!entry.IsItemEntry)
                PetCargoModule.LogRaw($"WARNING DepositCourierEntryServerRpc got a non-item entry; vanilla will silently DROP it -> {PetCargoModule.Describe(entry)}");
            else
                PetCargoModule.LogRaw($"DepositCourierEntryServerRpc -> {PetCargoModule.Describe(entry)}");
        }

        [HarmonyPatch(typeof(PetManager), "DepositCourierRuneServerRpc")]
        [HarmonyPrefix]
        private static void Pre_DepositCourierRune(PetManager __instance, Rune rune)
        {
            PetCargoModule.LogRaw($"DepositCourierRuneServerRpc -> rune empty={rune.IsEmpty()}");
        }

        // ---- item-loss-after-restart diagnostics --------------------------------------------

        // Couriers deposit through StorageDirectory.TryResolveDeliverable(key, playerHash, out record,
        // out live, out slotLimit). log the resolved record + live storage and whether they are the
        // same object, so we can tell if deposits land in the record that is later persisted.
        [HarmonyPatch(typeof(StorageDirectory), "TryResolveDeliverable")]
        [HarmonyPostfix]
        private static void Post_TryResolveDeliverable(bool __result, StorageData record, Storage live, ref int slotLimit)
        {
            PetCargoModule.LogDeliverable(__result, record, live, slotLimit);
        }

        // The pet panel's target list. Log each row's authored vs live slot state so we can see
        // whether the displayed capacity matches the backing record.
        [HarmonyPatch(typeof(StorageDirectory), "BuildFor")]
        [HarmonyPostfix]
        // [2026-10-01 14:40] __result MUST be the IL2CPP generic list type; using the managed
        // System.Collections.Generic.List<ChestDirectoryEntry> makes HarmonyX fail to emit the patch
        // ("Cannot assign method return type Il2CppSystem... to __result type System.Collections...")
        // and throws out of PatchAll, killing every patch in this class and logging a red error on load.
        private static void Post_BuildFor(Il2CppSystem.Collections.Generic.List<ChestDirectoryEntry> __result)
        {
            if (__result == null) return;
            try
            {
                for (int i = 0; i < __result.Count; i++)
                {
                    var row = __result[i];
                    Storage live = null;
                    try { live = StorageDirectory.FindLive(row.StorageName); } catch { }
                    PetCargoModule.LogStorageData(
                        $"BuildFor[{i}] name={row.StorageName} used={row.UsedSlots}/{row.TotalSlots}",
                        live?.StorageData);
                }
            }
            catch { }
        }

        // Opening a chest. Log the record/live invariant right before the UI is populated.
        [HarmonyPatch(typeof(Storage), "OpenChest")]
        [HarmonyPrefix]
        private static void Pre_OpenChest(Storage __instance)
        {
            if (__instance == null) return;
            PetCargoModule.LogStorageData($"OpenChest name={__instance.StorageName?.Value}", __instance.StorageData);
            PetCargoModule.LogLiveStorage("OpenChest", __instance);
        }
    }
}
