using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Unity.Collections;
using Unity.Netcode;

namespace EquipmentStatEditor.Core
{
    /// <summary>
    /// Snapshot of one Rune struct read straight from il2cpp memory.
    /// </summary>
    public unsafe class RuneSnapshot
    {
        public string Uuid = "";
        public int Set;
        public int SlotType;
        public int Rarity;
        public int Stars;
        public int Level;
        public int PrimaryStat;
        public int[] Stats = Array.Empty<int>();
        public float[] Values = Array.Empty<float>();
        public int[] Upgrades = Array.Empty<int>();
        public bool Empty = true;
    }

    /// <summary>
    /// RAW INTEROP LAYER.
    ///
    /// The Il2CppInterop assembly emits `Rune` (and `FixedList32Bytes&lt;T&gt;`) as a
    /// CLASS deriving from Il2CppSystem.ValueType. For a boxed value type:
    ///   * Native METHOD calls are correct (the stub passes il2cpp_object_unbox(this.Pointer)).
    ///   * FIELD access is BROKEN (the accessor does this.Pointer + fieldOffset where
    ///     Pointer is the boxed HEADER, not the value DATA).
    /// So every field read/write here goes through the real unboxed data pointer at the
    /// true il2cpp field offsets. Collection writes use the game's native set_Item with
    /// the DATA pointer - the generated indexer passes the boxed header and corrupts
    /// elements (battle-tested in DimraethModPack/RuneInfuserModule).
    /// </summary>
    public static unsafe class RuneMemory
    {
        // cpp2il-verified Rune struct layout.
        private const int OffSet = 0x00;
        private const int OffSlotType = 0x04;
        private const int OffRarity = 0x08;
        private const int OffStars = 0x0C;
        private const int OffUUID = 0x10;
        private const int OffLevel = 0x50;
        private const int OffOriginalOwner = 0x58;
        private const int OffPrimaryStat = 0x60;
        private const int OffSecStats = 0x68;
        private const int OffStatValues = 0x88;
        private const int OffStatUpgrades = 0xA8;
        private const int OffSecCount = 0xC8;
        private const int OffUpgCount = 0xCC;
        private const int OffValCount = 0xD0;
        private const int OffFormula = 0xD4;

        // FixedList32Bytes<T> for 4-byte elements: 2-byte length + pad to 4 + elements.
        private const int FixedListDataOffset = 4;

        // (32 - 2) / 4 = 7 elements max in a FixedList32Bytes<T>.
        public const int FixedListCapacity = 7;

        // [2026-09-26 02:46] Added: the game's real layout is StatValues = secondaries + 1
        // (StatValues[0] holds the PRIMARY stat's magnitude; verified against vanilla dump
        // entries, e.g. PrimaryStat=BurningDamage / SecondaryStats=Resistance / StatValues=12,6).
        // Because that list is capped at FixedListCapacity, the safe secondary cap is 6:
        // 7 secondaries need 8 value slots and the overflow destroys the item in-game
        // (user report 2026-09-26: "if I added max 7 new stats, the item got disappeared").
        public const int MaxSecondaryStats = FixedListCapacity - 1;

        // Full byte size of the Rune struct (last field FormulaVersion at 0xD4 is an int).
        public const int RuneStructSize = 0xD8;

        private static bool _offsetsReady;
        private static IntPtr _runeClass;

        private static Action<string> _logInfo = _ => { };
        private static Action<string> _logWarn = _ => { };
        private static Action<string> _logError = _ => { };

        public static void BindLogger(Action<string> info, Action<string> warn, Action<string> error)
        {
            _logInfo = info ?? _logInfo;
            _logWarn = warn ?? _logWarn;
            _logError = error ?? _logError;
        }

        /// <summary>
        /// Sanity-check that the interop reflection offsets match the known cpp2il
        /// layout; fall back to hard-coded offsets otherwise so a bad lookup can
        /// never silently corrupt a rune.
        /// </summary>
        public static void EnsureOffsets()
        {
            if (_offsetsReady) return;

            RuntimeHelpers.RunClassConstructor(typeof(Rune).TypeHandle);
            _runeClass = Il2CppClassPointerStore<Rune>.NativeClassPtr;

            int set = OffSet, slotType = OffSlotType, rarity = OffRarity, stars = OffStars, uuid = OffUUID;
            int level = OffLevel, originalOwner = OffOriginalOwner, primary = OffPrimaryStat;
            int secStats = OffSecStats, statValues = OffStatValues, statUpgrades = OffStatUpgrades;
            int secCount = OffSecCount, upgCount = OffUpgCount, valCount = OffValCount, formula = OffFormula;

            bool ok = true;
            try
            {
                set = FieldOffset("Set");
                slotType = FieldOffset("SlotType");
                rarity = FieldOffset("Rarity");
                stars = FieldOffset("Stars");
                uuid = FieldOffset("UUID");
                level = FieldOffset("Level");
                originalOwner = FieldOffset("OriginalOwner");
                primary = FieldOffset("PrimaryStat");
                secStats = FieldOffset("SecondaryStats");
                statValues = FieldOffset("StatValues");
                statUpgrades = FieldOffset("StatUpgrades");
                secCount = FieldOffset("SecondaryStatsCount");
                upgCount = FieldOffset("StatUpgradesCount");
                valCount = FieldOffset("StatValuesCount");
                formula = FieldOffset("FormulaVersion");
            }
            catch (Exception ex)
            {
                ok = false;
                _logWarn($"[RuneMemory] could not reflect rune field offsets ({ex.Message}); using cpp2il layout.");
                set = OffSet; slotType = OffSlotType; rarity = OffRarity; stars = OffStars; uuid = OffUUID;
                level = OffLevel; originalOwner = OffOriginalOwner; primary = OffPrimaryStat;
                secStats = OffSecStats; statValues = OffStatValues; statUpgrades = OffStatUpgrades;
                secCount = OffSecCount; upgCount = OffUpgCount; valCount = OffValCount; formula = OffFormula;
            }

            ok = ok && set == OffSet && slotType == OffSlotType && rarity == OffRarity &&
                 stars == OffStars && uuid == OffUUID && level == OffLevel &&
                 originalOwner == OffOriginalOwner && primary == OffPrimaryStat &&
                 secStats == OffSecStats && statValues == OffStatValues &&
                 statUpgrades == OffStatUpgrades && secCount == OffSecCount &&
                 upgCount == OffUpgCount && valCount == OffValCount && formula == OffFormula;

            if (!ok)
            {
                _logWarn($"[RuneMemory] interop field offsets differ from cpp2il layout " +
                         $"(Set={set:X} SlotType={slotType:X} Rarity={rarity:X} Stars={stars:X} UUID={uuid:X} " +
                         $"Level={level:X} Primary={primary:X}); using verified cpp2il layout instead.");
            }
            else
            {
                _logInfo("[RuneMemory] field offsets verified against cpp2il layout.");
            }

            _offsetsReady = true;
        }

        private static int FieldOffset(string fieldName)
        {
            FieldInfo fi = typeof(Rune).GetField("NativeFieldInfoPtr_" + fieldName,
                BindingFlags.Static | BindingFlags.NonPublic);
            if (fi == null) throw new MissingFieldException("Rune." + fieldName);
            var fieldInfo = (IntPtr)fi.GetValue(null);
            return (int)IL2CPP.il2cpp_field_get_offset(fieldInfo);
        }

        /// <summary>
        /// Real address of a Rune value's DATA. Il2CppInterop wrappers are usually
        /// boxed object headers (unbox them), but some collection getters already
        /// return the raw value pointer - detect which we have via the klass slot.
        /// </summary>
        public static IntPtr DataPtr(Rune rune)
        {
            if (ReferenceEquals(rune, null)) return IntPtr.Zero;
            EnsureOffsets();
            if (_runeClass == IntPtr.Zero) return IntPtr.Zero; // can't safely classify the pointer
            IntPtr p = IL2CPP.Il2CppObjectBaseToPtrNotNull(rune);
            if (p == IntPtr.Zero) return IntPtr.Zero;
            if (IL2CPP.il2cpp_object_get_class(p) == _runeClass)
                return IL2CPP.il2cpp_object_unbox(p);
            return p;
        }

        /// <summary>
        /// Wrap raw rune data in a real boxed Rune so native methods get a correct
        /// `this`. Returns null when the class/data pointer is not usable - boxing
        /// with a null class is an uncatchable access violation.
        /// </summary>
        public static Rune FreshBox(IntPtr data)
        {
            if (data == IntPtr.Zero) return null;
            EnsureOffsets();
            if (_runeClass == IntPtr.Zero) return null;
            IntPtr boxed = IL2CPP.il2cpp_value_box(_runeClass, data);
            if (boxed == IntPtr.Zero) return null;
            return new Rune(boxed);
        }

        public static bool IsEmptyRune(IntPtr data)
        {
            if (data == IntPtr.Zero) return true;
            try
            {
                Rune boxed = FreshBox(data);
                if (boxed != null) return boxed.IsEmpty();
            }
            catch { }
            // Fallback: FixedString64Bytes.UUID length sits at the start of the UUID field.
            return *(ushort*)(data + OffUUID) == 0;
        }

        // ------------------------------------------------------------------
        // Reads
        // ------------------------------------------------------------------

        public static string ReadUuid(IntPtr data)
        {
            FixedString64Bytes uuid = *(FixedString64Bytes*)(data + OffUUID);
            return uuid.ToString();
        }

        public static RuneSnapshot Read(IntPtr data)
        {
            if (data == IntPtr.Zero) return new RuneSnapshot();
            var snap = new RuneSnapshot
            {
                Uuid = ReadUuid(data),
                Set = *(int*)(data + OffSet),
                SlotType = *(int*)(data + OffSlotType),
                Rarity = *(int*)(data + OffRarity),
                Stars = *(int*)(data + OffStars),
                Level = *(int*)(data + OffLevel),
                PrimaryStat = *(int*)(data + OffPrimaryStat),
                Empty = IsEmptyRune(data),
            };

            int secLen = ReadListLen(data + OffSecStats);
            int valLen = ReadListLen(data + OffStatValues);
            int upgLen = ReadListLen(data + OffStatUpgrades);

            snap.Stats = new int[secLen];
            for (int i = 0; i < secLen; i++)
                snap.Stats[i] = *(int*)(data + OffSecStats + FixedListDataOffset + i * 4);

            snap.Values = new float[valLen];
            for (int i = 0; i < valLen; i++)
                snap.Values[i] = *(float*)(data + OffStatValues + FixedListDataOffset + i * 4);

            snap.Upgrades = new int[upgLen];
            for (int i = 0; i < upgLen; i++)
                snap.Upgrades[i] = *(int*)(data + OffStatUpgrades + FixedListDataOffset + i * 4);

            return snap;
        }

        private static int ReadListLen(IntPtr listAddr)
        {
            int len = *(ushort*)listAddr;
            if (len < 0) len = 0;
            if (len > FixedListCapacity) len = FixedListCapacity;
            return len;
        }

        // ------------------------------------------------------------------
        // Writes
        // ------------------------------------------------------------------

        /// <summary>
        /// Replace one FixedList32Bytes&lt;T&gt; field without assuming its byte layout:
        /// box the field, drive the game's own FixedList methods, copy the finished
        /// 32-byte value back.
        /// </summary>
        public static void WriteFixedList<T>(IntPtr fieldAddr, void* src, int count) where T : new()
        {
            IntPtr classPtr = Il2CppClassPointerStore<FixedList32Bytes<T>>.NativeClassPtr;
            IntPtr boxed = IL2CPP.il2cpp_value_box(classPtr, fieldAddr);
            var list = new FixedList32Bytes<T>(boxed);
            list.Clear();
            if (src != null && count > 0)
                list.AddRange(src, count);
            uint align = 0;
            int size = IL2CPP.il2cpp_class_value_size(classPtr, ref align);
            Unsafe.CopyBlock((void*)fieldAddr,
                (void*)IL2CPP.il2cpp_object_unbox(IL2CPP.Il2CppObjectBaseToPtrNotNull(list)),
                (uint)size);
        }

        private static readonly Dictionary<Type, IntPtr> SetItemCache = new Dictionary<Type, IntPtr>();

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

        /// <summary>
        /// Call the collection's native set_Item(int, T) directly, passing the value
        /// DATA pointer. This is the correct write path; the interop indexer passes
        /// the boxed header and corrupts the element.
        /// </summary>
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

        /// <summary>
        /// [2026-09-26 02:46] Added: raw byte snapshot of a rune. The apply-error handler in
        /// EditorEngine keeps one before every write so a rejected edit can be rolled back -
        /// a bad edit must never destroy the item (user report: items vanished after edits).
        /// </summary>
        public static byte[] SnapshotRaw(IntPtr data)
        {
            if (data == IntPtr.Zero) throw new ArgumentNullException(nameof(data));
            byte[] buf = new byte[RuneStructSize];
            System.Runtime.InteropServices.Marshal.Copy(data, buf, 0, RuneStructSize);
            return buf;
        }

        /// <summary>
        /// [2026-09-26 02:46] Added: write a snapshot from SnapshotRaw back into a collection
        /// slot (rollback path of the apply-error handler). The restored content is pre-signed
        /// with the game's HMAC BEFORE the push, exactly like the normal write path, so the
        /// synchronous OnEquipmentWornChanged integrity check does not treat it as tampering.
        /// </summary>
        public static void RestoreRaw(Player player, Il2CppObjectBase collection, Type collectionType, int index, byte[] snapshot)
        {
            if (snapshot == null || snapshot.Length < RuneStructSize)
                throw new ArgumentException("snapshot too small", nameof(snapshot));
            IntPtr tmp = System.Runtime.InteropServices.Marshal.AllocHGlobal(RuneStructSize);
            try
            {
                System.Runtime.InteropServices.Marshal.Copy(snapshot, 0, tmp, RuneStructSize);
                // [2026-09-28 10:40] OBSOLETE - PreSign always threw (ComputeRuneHMAC NRE);
                // the caller's Rebaseline after RestoreRaw re-signs via the game's
                // RefreshAllRuneSignatures. Kept as a comment to preserve intent.
                // PreSign(player, tmp);
                SetElementRaw(collection, collectionType, index, tmp);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(tmp);
            }
        }

        /// <summary>
        /// Apply a config entry to a rune's data memory. Only fields the entry
        /// actually defines are written. FormulaVersion is always forced to the
        /// current version, otherwise SaveSystem.MigrateRuneFormulas recomputes
        /// StatValues on every save and wipes custom magnitudes. Count fields are
        /// re-derived from the FixedList lengths (same job as RepairRuneCounts).
        /// </summary>
        public static void ApplyEntry(IntPtr data, RuneEntry e)
        {
            if (data == IntPtr.Zero) throw new ArgumentNullException(nameof(data));
            EnsureOffsets();

            // [2026-09-26 02:46] Added: count-invariant guard (error prevention). The game's
            // layout is StatValues = secondaries + 1 (slot 0 = PrimaryStat magnitude). A value
            // count equal to the secondary count is accepted as shorthand and expanded here by
            // prepending the live primary value so every secondary lands in its real slot.
            // Anything else misaligns the values and the game strips such items.
            int secN = e.SecondaryStats != null
                ? Math.Min(e.SecondaryStats.Count, FixedListCapacity)
                : ReadListLen(data + OffSecStats);
            if (e.StatValues != null && secN >= 0)
            {
                if (e.StatValues.Count != secN && e.StatValues.Count != secN + 1)
                    throw new InvalidOperationException(
                        $"StatValues count {e.StatValues.Count} does not match {secN} secondary stat(s) - expected {secN + 1} (one per secondary + PrimaryStat value) or {secN}");
                if (e.StatValues.Count == secN)
                {
                    float primaryVal = *(float*)(data + OffStatValues + FixedListDataOffset);
                    var expanded = new List<float>(secN + 1) { primaryVal };
                    expanded.AddRange(e.StatValues);
                    e.StatValues = expanded;
                    _logInfo($"[RuneMemory] StatValues expanded to the game layout (prepended PrimaryStat value {primaryVal}).");
                }
            }

            if (e.Set.HasValue) *(int*)(data + OffSet) = (int)e.Set.Value;
            if (e.SlotType.HasValue) *(int*)(data + OffSlotType) = (int)e.SlotType.Value;
            if (e.Rarity.HasValue) *(int*)(data + OffRarity) = (int)e.Rarity.Value;
            if (e.Stars.HasValue) *(int*)(data + OffStars) = (int)e.Stars.Value;
            if (e.Level.HasValue) *(int*)(data + OffLevel) = e.Level.Value;
            if (e.PrimaryStat.HasValue) *(int*)(data + OffPrimaryStat) = (int)e.PrimaryStat.Value;

            if (e.SecondaryStats != null)
            {
                int n = Math.Min(e.SecondaryStats.Count, FixedListCapacity);
                int[] arr = new int[n];
                for (int i = 0; i < n; i++) arr[i] = (int)e.SecondaryStats[i];
                fixed (int* sp = arr)
                    WriteFixedList<int>(data + OffSecStats, sp, n);
            }

            if (e.StatValues != null)
            {
                int n = Math.Min(e.StatValues.Count, FixedListCapacity);
                float[] arr = new float[n];
                for (int i = 0; i < n; i++) arr[i] = e.StatValues[i];
                fixed (float* vp = arr)
                    WriteFixedList<float>(data + OffStatValues, vp, n);
            }

            if (e.StatUpgrades != null)
            {
                int n = Math.Min(e.StatUpgrades.Count, FixedListCapacity);
                int[] arr = new int[n];
                for (int i = 0; i < n; i++) arr[i] = e.StatUpgrades[i];
                fixed (int* up = arr)
                    WriteFixedList<int>(data + OffStatUpgrades, up, n);
            }

            *(int*)(data + OffSecCount) = ReadListLen(data + OffSecStats);
            *(int*)(data + OffValCount) = ReadListLen(data + OffStatValues);
            *(int*)(data + OffUpgCount) = ReadListLen(data + OffStatUpgrades);
            *(int*)(data + OffFormula) = SaveSystem.CurrentRuneFormulaVersion;
        }

        /// <summary>
        /// Pre-sign a rune with the game's own HMAC before writing it back into a
        /// collection (the write fires OnEquipmentWornChanged synchronously and the
        /// integrity check must not treat the change as tampering).
        /// </summary>
        public static void PreSign(Player player, IntPtr data)
        {
            try
            {
                if (player._runeSignatureStore == null) return;
                Rune boxed = FreshBox(data);
                if (boxed == null) return;
                FixedString64Bytes uuid = *(FixedString64Bytes*)(data + OffUUID);
                int hmac = player.ComputeRuneHMAC(boxed);
                player._runeSignatureStore[uuid] = hmac;
            }
            catch (Exception ex) { _logError($"[RuneMemory] pre-sign failed: {ex.Message}"); }
        }

        /// <summary>
        /// Rebuild the runtime integrity baseline after editing. RefreshAllRuneSignatures
        /// only adds *missing* entries, so the store is cleared first to make sure the
        /// edited runes are re-signed - otherwise a stale pre-edit HMAC makes
        /// PeriodicRuneIntegrityCheck treat the change as tampering.
        /// </summary>
        public static void Rebaseline(Player player)
        {
            try
            {
                if (player._runeSignatureStore != null)
                {
                    player._runeSignatureStore.Clear();
                    _logInfo("[RuneMemory] cleared rune signature store for re-baseline");
                }
                player.RefreshAllRuneSignatures();
                _logInfo($"[RuneMemory] re-baselined; signature store count={player._runeSignatureStore?.Count ?? -1}");

                bool valid = player.ValidateRuneState();
                _logInfo($"[RuneMemory] ValidateRuneState={valid}");
                if (!valid)
                    _logWarn("[RuneMemory] ValidateRuneState returned false - stats may be reverted by the integrity coroutine.");
            }
            catch (Exception ex) { _logError($"[RuneMemory] re-baseline failed: {ex.Message}"); }
        }

        /// <summary>Recalculate the cached rune bonuses so the edits take effect on derived stats.</summary>
        public static void RecalcBonuses(Player player)
        {
            try
            {
                if (player.Runes != null)
                    player.Runes.UpdateRuneBonuses();
            }
            catch (Exception ex) { _logError($"[RuneMemory] UpdateRuneBonuses failed: {ex.Message}"); }
        }
    }
}
