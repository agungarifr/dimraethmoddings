using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Unity.Collections;
using Unity.Netcode;

namespace EquippedStatModifier.Core
{
    public class RuneSnapshot
    {
        public string Uuid = "";
        public Runes.RuneSet Set;
        public Runes.SlotType SlotType;
        public Runes.Rarity Rarity;
        public Runes.Stars Stars;
        public int Level;
        public Runes.Stat PrimaryStat;
        public List<Runes.Stat> SecondaryStats = new();
        public List<float> StatValues = new();
        public List<int> StatUpgrades = new();
        public bool Empty = true;
    }

    public static unsafe class RuneMemory
    {
        // Default offsets for boxed Il2CppObject pointer (header = 0x10)
        private static int _offSet = 0x10;
        private static int _offSlotType = 0x14;
        private static int _offRarity = 0x18;
        private static int _offStars = 0x1C;
        private static int _offUUID = 0x20;
        private static int _offLevel = 0x60;
        private static int _offOriginalOwner = 0x68;
        private static int _offPrimaryStat = 0x70;
        private static int _offSecStats = 0x78;
        private static int _offStatValues = 0x98;
        private static int _offStatUpgrades = 0xB8;
        private static int _offSecCount = 0xD8;
        private static int _offUpgCount = 0xDC;
        private static int _offValCount = 0xE0;
        private static int _offFormula = 0xE4;

        private const int FixedListDataOffset = 4;
        public const int FixedListCapacity = 7;

        private static bool _offsetsReady;
        private static IntPtr _runeClass;

        public static Action<string> LogInfo = _ => { };
        public static Action<string> LogWarn = _ => { };
        public static Action<string> LogError = _ => { };

        public static void EnsureOffsets()
        {
            if (_offsetsReady) return;

            try
            {
                RuntimeHelpers.RunClassConstructor(typeof(Rune).TypeHandle);
                _runeClass = Il2CppClassPointerStore<Rune>.NativeClassPtr;
                LogInfo($"[RuneMemory] Initialized Rune struct interop. ClassPtr: {_runeClass:X}");

                if (_runeClass != IntPtr.Zero)
                {
                    _offSet = ResolveFieldOffset("Set", _offSet);
                    _offSlotType = ResolveFieldOffset("SlotType", _offSlotType);
                    _offRarity = ResolveFieldOffset("Rarity", _offRarity);
                    _offStars = ResolveFieldOffset("Stars", _offStars);
                    _offUUID = ResolveFieldOffset("UUID", _offUUID);
                    _offLevel = ResolveFieldOffset("Level", _offLevel);
                    _offOriginalOwner = ResolveFieldOffset("OriginalOwner", _offOriginalOwner);
                    _offPrimaryStat = ResolveFieldOffset("PrimaryStat", _offPrimaryStat);
                    _offSecStats = ResolveFieldOffset("SecondaryStats", _offSecStats);
                    _offStatValues = ResolveFieldOffset("StatValues", _offStatValues);
                    _offStatUpgrades = ResolveFieldOffset("StatUpgrades", _offStatUpgrades);
                    _offSecCount = ResolveFieldOffset("SecondaryStatsCount", _offSecCount);
                    _offUpgCount = ResolveFieldOffset("StatUpgradesCount", _offUpgCount);
                    _offValCount = ResolveFieldOffset("StatValuesCount", _offValCount);
                    _offFormula = ResolveFieldOffset("FormulaVersion", _offFormula);

                    LogInfo($"[RuneMemory] Resolved field offsets: UUID=0x{_offUUID:X}, SecStats=0x{_offSecStats:X}, StatValues=0x{_offStatValues:X}, SecCount=0x{_offSecCount:X}");
                }
            }
            catch (Exception ex)
            {
                LogWarn($"[RuneMemory] Could not reflect Rune struct: {ex.Message}");
            }

            _offsetsReady = true;
        }

        private static int ResolveFieldOffset(string fieldName, int fallback)
        {
            try
            {
                if (_runeClass == IntPtr.Zero) return fallback;
                IntPtr field = IL2CPP.GetIl2CppField(_runeClass, fieldName);
                if (field != IntPtr.Zero)
                {
                    int off = (int)IL2CPP.il2cpp_field_get_offset(field);
                    if (off > 0) return off;
                }
            }
            catch { }
            return fallback;
        }

        public static Player GetLocalPlayer()
        {
            try
            {
                if (DataStorage.Singleton != null && DataStorage.Singleton.Player != null)
                    return DataStorage.Singleton.Player;
            }
            catch { }

            try
            {
                var p = Utility.ReturnOwnerOfClient();
                if (p != null) return p;
            }
            catch { }

            try
            {
                var players = UnityEngine.Object.FindObjectsOfType<Player>();
                if (players != null)
                {
                    foreach (var p in players)
                    {
                        if (p != null && p.IsOwner) return p;
                    }
                    if (players.Length > 0) return players[0];
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Reliably checks if a Rune object represents an equipped item.
        /// </summary>
        public static bool IsRuneEquipped(Rune rune)
        {
            if (ReferenceEquals(rune, null)) return false;

            try
            {
                // Native game method: Rune.IsEmpty()
                if (rune.IsEmpty()) return false;

                // Non-empty UUID or active stats confirm item presence
                string uuid = rune.UUID.ToString();
                if (!string.IsNullOrWhiteSpace(uuid) && uuid != "00000000-0000-0000-0000-000000000000")
                    return true;

                if (rune.Level > 0 || rune.SecondaryStatsCount > 0 || rune.PrimaryStat != Runes.Stat.None)
                    return true;
            }
            catch { }

            // Pointer fallback
            try
            {
                IntPtr p = IL2CPP.Il2CppObjectBaseToPtrNotNull(rune);
                if (p != IntPtr.Zero)
                {
                    EnsureOffsets();
                    ushort uuidLen = *(ushort*)(p + _offUUID);
                    if (uuidLen > 0 && uuidLen <= 60) return true;
                    int secCount = *(int*)(p + _offSecCount);
                    if (secCount > 0 && secCount <= FixedListCapacity) return true;
                }
            }
            catch { }

            return false;
        }

        public static IntPtr DataPtr(Rune rune)
        {
            if (ReferenceEquals(rune, null)) return IntPtr.Zero;
            EnsureOffsets();

            IntPtr p = IL2CPP.Il2CppObjectBaseToPtrNotNull(rune);
            return p;
        }

        public static bool IsEmptyRune(IntPtr data)
        {
            if (data == IntPtr.Zero) return true;
            EnsureOffsets();
            try
            {
                ushort len = *(ushort*)(data + _offUUID);
                if (len > 0 && len <= 60) return false;
                int secCount = *(int*)(data + _offSecCount);
                if (secCount > 0 && secCount <= FixedListCapacity) return false;
            }
            catch { }
            return true;
        }

        public static string ReadUuid(IntPtr data)
        {
            if (data == IntPtr.Zero) return "";
            EnsureOffsets();
            try
            {
                FixedString64Bytes uuid = *(FixedString64Bytes*)(data + _offUUID);
                return uuid.ToString();
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Reads a full snapshot of an equipped Rune.
        /// </summary>
        public static RuneSnapshot Read(Rune rune)
        {
            if (ReferenceEquals(rune, null)) return new RuneSnapshot { Empty = true };
            EnsureOffsets();

            if (!IsRuneEquipped(rune)) return new RuneSnapshot { Empty = true };

            var snap = new RuneSnapshot { Empty = false };

            try
            {
                snap.Uuid = rune.UUID.ToString();
            }
            catch
            {
                snap.Uuid = "";
            }

            try
            {
                snap.Set = rune.Set;
                snap.SlotType = rune.SlotType;
                snap.Rarity = rune.Rarity;
                snap.Stars = rune.Stars;
                snap.Level = rune.Level;
                snap.PrimaryStat = rune.PrimaryStat;
            }
            catch (Exception ex)
            {
                LogWarn($"[RuneMemory] Error reading standard fields: {ex.Message}");
            }

            // Read secondary stats
            bool readStatsSuccess = false;
            try
            {
                var sec = rune.SecondaryStats;
                var vals = rune.StatValues;
                var upgs = rune.StatUpgrades;
                int count = rune.SecondaryStatsCount;
                if (count <= 0 && sec != null) count = sec.Length;
                if (count > FixedListCapacity) count = FixedListCapacity;

                for (int i = 0; i < count; i++)
                {
                    if (sec != null && i < sec.Length)
                        snap.SecondaryStats.Add(sec[i]);
                    if (vals != null && i < vals.Length)
                        snap.StatValues.Add(vals[i]);
                    else
                        snap.StatValues.Add(0f);
                    if (upgs != null && i < upgs.Length)
                        snap.StatUpgrades.Add(upgs[i]);
                    else
                        snap.StatUpgrades.Add(0);
                }
                readStatsSuccess = (snap.SecondaryStats.Count == count);
            }
            catch (Exception ex)
            {
                LogWarn($"[RuneMemory] Reading via properties: {ex.Message}");
            }

            // Direct memory fallback if needed
            if (!readStatsSuccess || snap.SecondaryStats.Count == 0)
            {
                try
                {
                    IntPtr p = IL2CPP.Il2CppObjectBaseToPtrNotNull(rune);
                    if (p != IntPtr.Zero)
                    {
                        snap.SecondaryStats.Clear();
                        snap.StatValues.Clear();
                        snap.StatUpgrades.Clear();

                        int secLen = ReadListLen(p + _offSecStats);
                        int valLen = ReadListLen(p + _offStatValues);
                        int upgLen = ReadListLen(p + _offStatUpgrades);
                        int count = Math.Max(secLen, Math.Max(valLen, *(int*)(p + _offSecCount)));
                        if (count > FixedListCapacity) count = FixedListCapacity;

                        for (int i = 0; i < count; i++)
                        {
                            int statId = *(int*)(p + _offSecStats + FixedListDataOffset + i * 4);
                            snap.SecondaryStats.Add((Runes.Stat)statId);
                            float val = *(float*)(p + _offStatValues + FixedListDataOffset + i * 4);
                            snap.StatValues.Add(val);
                            int upg = *(int*)(p + _offStatUpgrades + FixedListDataOffset + i * 4);
                            snap.StatUpgrades.Add(upg);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogWarn($"[RuneMemory] Memory fallback read exception: {ex.Message}");
                }
            }

            return snap;
        }

        public static RuneSnapshot Read(IntPtr data)
        {
            if (data == IntPtr.Zero) return new RuneSnapshot { Empty = true };
            EnsureOffsets();

            var snap = new RuneSnapshot
            {
                Uuid = ReadUuid(data),
                Set = (Runes.RuneSet)(*(int*)(data + _offSet)),
                SlotType = (Runes.SlotType)(*(int*)(data + _offSlotType)),
                Rarity = (Runes.Rarity)(*(int*)(data + _offRarity)),
                Stars = (Runes.Stars)(*(int*)(data + _offStars)),
                Level = *(int*)(data + _offLevel),
                PrimaryStat = (Runes.Stat)(*(int*)(data + _offPrimaryStat)),
                Empty = IsEmptyRune(data)
            };

            int secLen = ReadListLen(data + _offSecStats);
            int valLen = ReadListLen(data + _offStatValues);
            int upgLen = ReadListLen(data + _offStatUpgrades);
            int count = Math.Max(secLen, Math.Max(valLen, *(int*)(data + _offSecCount)));
            if (count > FixedListCapacity) count = FixedListCapacity;

            for (int i = 0; i < count; i++)
            {
                int statId = *(int*)(data + _offSecStats + FixedListDataOffset + i * 4);
                snap.SecondaryStats.Add((Runes.Stat)statId);
                float val = *(float*)(data + _offStatValues + FixedListDataOffset + i * 4);
                snap.StatValues.Add(val);
                int upg = *(int*)(data + _offStatUpgrades + FixedListDataOffset + i * 4);
                snap.StatUpgrades.Add(upg);
            }

            return snap;
        }

        private static int ReadListLen(IntPtr listAddr)
        {
            int len = *(ushort*)listAddr;
            if (len < 0) len = 0;
            if (len > FixedListCapacity) len = FixedListCapacity;
            return len;
        }

        private static readonly Dictionary<Type, IntPtr> SetItemCache = new();

        private static IntPtr ResolveSetItemMethod(Type collectionType)
        {
            if (SetItemCache.TryGetValue(collectionType, out var cached))
                return cached;

            RuntimeHelpers.RunClassConstructor(collectionType.TypeHandle);
            IntPtr found = IntPtr.Zero;
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var f in collectionType.GetFields(flags))
            {
                if (f.FieldType != typeof(IntPtr)) continue;
                if (f.Name.IndexOf("set_Item", StringComparison.Ordinal) >= 0 &&
                    f.Name.IndexOf("Int32", StringComparison.Ordinal) >= 0)
                {
                    found = (IntPtr)f.GetValue(null);
                    break;
                }
            }
            SetItemCache[collectionType] = found;
            return found;
        }

        public static void SetElementRaw(Il2CppObjectBase collection, Type collectionType, int index, IntPtr valueData)
        {
            IntPtr method = ResolveSetItemMethod(collectionType);
            if (method == IntPtr.Zero)
                throw new InvalidOperationException("Could not resolve set_Item on " + collectionType.Name);

            IntPtr* p = stackalloc IntPtr[2];
            p[0] = (IntPtr)(&index);
            p[1] = valueData;
            IntPtr exc = IntPtr.Zero;
            IL2CPP.il2cpp_runtime_invoke(method, IL2CPP.Il2CppObjectBaseToPtrNotNull(collection), (void**)p, ref exc);
            Il2CppException.RaiseExceptionIfNecessary(exc);
        }

        public static bool ApplySecondaryStats(
            Player player,
            int slotIndex,
            List<Runes.Stat> newStats,
            List<float> newValues,
            List<int> newUpgrades,
            out string error)
        {
            error = "";
            if (player == null || player.EquipmentWorn == null)
            {
                error = "Player or EquipmentWorn is not available.";
                return false;
            }

            if (slotIndex < 0 || slotIndex >= player.EquipmentWorn.Count)
            {
                error = $"Slot index {slotIndex} is out of bounds (count={player.EquipmentWorn.Count}).";
                return false;
            }

            Rune rune;
            try
            {
                rune = player.EquipmentWorn[slotIndex];
            }
            catch (Exception ex)
            {
                error = $"Failed reading slot {slotIndex}: {ex.Message}";
                return false;
            }

            if (!IsRuneEquipped(rune))
            {
                error = $"No item equipped in slot {slotIndex}.";
                return false;
            }

            int n = newStats?.Count ?? 0;
            if (n > FixedListCapacity)
            {
                error = $"Max secondary stat capacity is {FixedListCapacity}, requested {n}.";
                return false;
            }

            if (newValues == null || newValues.Count != n)
            {
                error = $"Stat values count ({newValues?.Count ?? 0}) does not match stat count ({n}).";
                return false;
            }

            List<int> upgrades = newUpgrades;
            if (upgrades == null || upgrades.Count != n)
            {
                upgrades = new List<int>(n);
                for (int i = 0; i < n; i++)
                    upgrades.Add(newUpgrades != null && i < newUpgrades.Count ? newUpgrades[i] : 0);
            }

            EnsureOffsets();

            try
            {
                IntPtr p = IL2CPP.Il2CppObjectBaseToPtrNotNull(rune);
                if (p == IntPtr.Zero)
                {
                    error = "Failed to obtain native pointer to rune object.";
                    return false;
                }

                // 1. SecondaryStats list (ushort length at offset 0, elements at offset 4)
                *(ushort*)(p + _offSecStats) = (ushort)n;
                int* statsPtr = (int*)(p + _offSecStats + FixedListDataOffset);
                for (int i = 0; i < n; i++) statsPtr[i] = (int)newStats[i];
                for (int i = n; i < FixedListCapacity; i++) statsPtr[i] = 0;

                // 2. StatValues list
                *(ushort*)(p + _offStatValues) = (ushort)n;
                float* valsPtr = (float*)(p + _offStatValues + FixedListDataOffset);
                for (int i = 0; i < n; i++) valsPtr[i] = newValues[i];
                for (int i = n; i < FixedListCapacity; i++) valsPtr[i] = 0f;

                // 3. StatUpgrades list
                *(ushort*)(p + _offStatUpgrades) = (ushort)n;
                int* upgPtr = (int*)(p + _offStatUpgrades + FixedListDataOffset);
                for (int i = 0; i < n; i++) upgPtr[i] = upgrades[i];
                for (int i = n; i < FixedListCapacity; i++) upgPtr[i] = 0;

                // 4. Update count fields & formula version
                *(int*)(p + _offSecCount) = n;
                *(int*)(p + _offValCount) = n;
                *(int*)(p + _offUpgCount) = n;
                *(int*)(p + _offFormula) = SaveSystem.CurrentRuneFormulaVersion;

                // 5. Update NetworkList slot so events trigger (toggle with Empty to bypass UUID-only Equals check)
                try
                {
                    player.EquipmentWorn[slotIndex] = Rune.Empty;
                    player.EquipmentWorn[slotIndex] = rune;
                }
                catch
                {
                    IntPtr emptyUnboxed = IL2CPP.il2cpp_object_unbox(IL2CPP.Il2CppObjectBaseToPtrNotNull(Rune.Empty));
                    SetElementRaw(player.EquipmentWorn, typeof(NetworkList<Rune>), slotIndex, emptyUnboxed);
                    
                    IntPtr unboxed = IL2CPP.il2cpp_object_unbox(p);
                    SetElementRaw(player.EquipmentWorn, typeof(NetworkList<Rune>), slotIndex, unboxed);
                }

                // 6. Anti-cheat / signature re-baseline
                Rebaseline(player);

                // 7. Recalculate stats immediately
                RecalcBonuses(player);

                LogInfo($"[RuneMemory] Successfully modified slot {slotIndex} with {n} secondary stats.");
                return true;
            }
            catch (Exception ex)
            {
                error = $"Exception while writing stats: {ex.Message}";
                LogError($"[RuneMemory] {error}\n{ex}");
                return false;
            }
        }

        public static void Rebaseline(Player player)
        {
            try
            {
                if (player._runeSignatureStore != null)
                {
                    player._runeSignatureStore.Clear();
                }
                player.RefreshAllRuneSignatures();
                player.ValidateRuneState();
            }
            catch (Exception ex)
            {
                LogError($"[RuneMemory] Re-baseline failed: {ex.Message}");
            }
        }

        public static void RecalcBonuses(Player player)
        {
            try
            {
                if (player.Runes != null)
                    player.Runes.UpdateRuneBonuses();
            }
            catch (Exception ex)
            {
                LogError($"[RuneMemory] UpdateRuneBonuses failed: {ex.Message}");
            }
        }

        public static bool TriggerSave(Player player, out string msg)
        {
            try
            {
                if (player == null)
                {
                    msg = "Player is null.";
                    return false;
                }
                player.SaveGame();
                msg = "Game saved successfully!";
                return true;
            }
            catch (Exception ex)
            {
                msg = $"SaveGame failed: {ex.Message}";
                return false;
            }
        }
    }
}
