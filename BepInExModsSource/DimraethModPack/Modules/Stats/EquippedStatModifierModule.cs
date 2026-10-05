using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using DimraethModPack.Core;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace DimraethModPack.Modules.Stats
{
    public class EquippedStatModifierModule : ModModuleBase
    {
        public static EquippedStatModifierModule Instance { get; private set; }

        public override string Category => "Stats";
        public override string Name => "Equipped Stat Modifier";
        public override string Description => "Live editor to detect & edit secondary stat affixes and numbers on currently equipped gear";

        public ConfigEntry<KeyCode> MenuKey;

        public override void BindConfig(ConfigFile config)
        {
            Instance = this;
            string sec = "Stats.EquippedStatModifier";

            Enabled = config.Bind(sec, "Enabled", true,
                "Enable Equipped Stat Modifier (Default: true)");

            MenuKey = config.Bind(sec, "MenuKey", KeyCode.F8,
                "Hotkey to toggle the equipped gear stat editor UI (Default: F8)");
        }

        public override void ApplyPatches(Harmony harmony)
        {
            // Direct memory manipulation via raw interop - no harmony hooks required.
        }

        public override float DrawSettings(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;
            curY += DrawToggle(x, curY, width, "Equipped Stat Modifier Active", Enabled, "Default: ON", labelStyle, btnStyle);

            GUI.Label(new Rect(x, curY, width - 180f, 24f),
                $"Toggle Shortcut: <color=#FFDD44>[{MenuKey.Value}]</color> <color=#888888>(or click button)</color>", labelStyle);

            if (GUI.Button(new Rect(x + width - 175f, curY, 175f, 24f), "▶ Open Stat Editor", btnStyle))
            {
                UI.Toggle();
            }
            curY += 28f;

            GUI.Label(new Rect(x, curY, width, 20f),
                "<color=#888888>Detects equipped gear (sash, rings, amulets, etc.) and allows modifying secondary affixes & magnitudes.</color>", labelStyle);
            curY += 22f;

            return curY - y;
        }

        public static void OnUpdate()
        {
            if (Instance == null || !Instance.IsEnabled) return;

            try
            {
                KeyCode toggleKey = Instance.MenuKey != null ? Instance.MenuKey.Value : KeyCode.F8;
                if (Input.GetKeyDown(toggleKey))
                {
                    UI.Toggle();
                }
                else if (UI.IsOpen && Input.GetKeyDown(KeyCode.Escape))
                {
                    UI.Toggle();
                }
            }
            catch { }
        }

        public static void OnGUI()
        {
            if (Instance == null || !Instance.IsEnabled) return;

            try
            {
                UI.Draw();
            }
            catch { }
        }

        // =================================================================
        // Core Memory & Interop Layer
        // =================================================================
        public static class Memory
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

            private static bool _ready;
            private static IntPtr _runeClass;

            public static void EnsureOffsets()
            {
                if (_ready) return;
                try
                {
                    RuntimeHelpers.RunClassConstructor(typeof(Rune).TypeHandle);
                    _runeClass = Il2CppClassPointerStore<Rune>.NativeClassPtr;

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
                    }
                }
                catch { }
                _ready = true;
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

            public static unsafe bool IsRuneEquipped(Rune rune)
            {
                if (ReferenceEquals(rune, null)) return false;

                try
                {
                    if (rune.IsEmpty()) return false;

                    string uuid = rune.UUID.ToString();
                    if (!string.IsNullOrWhiteSpace(uuid) && uuid != "00000000-0000-0000-0000-000000000000")
                        return true;

                    if (rune.Level > 0 || rune.SecondaryStatsCount > 0 || rune.PrimaryStat != Runes.Stat.None)
                        return true;
                }
                catch { }

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
                return IL2CPP.Il2CppObjectBaseToPtrNotNull(rune);
            }

            public static unsafe bool IsEmptyRune(IntPtr data)
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

            public static unsafe string ReadUuid(IntPtr data)
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

            public class Snapshot
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

            public static unsafe Snapshot Read(Rune rune)
            {
                if (ReferenceEquals(rune, null)) return new Snapshot { Empty = true };
                EnsureOffsets();

                if (!IsRuneEquipped(rune)) return new Snapshot { Empty = true };

                var snap = new Snapshot { Empty = false };

                try { snap.Uuid = rune.UUID.ToString(); } catch { snap.Uuid = ""; }

                try
                {
                    snap.Set = rune.Set;
                    snap.SlotType = rune.SlotType;
                    snap.Rarity = rune.Rarity;
                    snap.Stars = rune.Stars;
                    snap.Level = rune.Level;
                    snap.PrimaryStat = rune.PrimaryStat;
                }
                catch { }

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
                catch { }

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
                    catch { }
                }

                return snap;
            }

            public static unsafe Snapshot Read(IntPtr data)
            {
                if (data == IntPtr.Zero) return new Snapshot { Empty = true };
                EnsureOffsets();

                var snap = new Snapshot
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

            private static unsafe int ReadListLen(IntPtr listAddr)
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

            public static unsafe void SetElementRaw(Il2CppObjectBase collection, Type collectionType, int index, IntPtr valueData)
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

            public static unsafe bool ApplySecondaryStats(
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
                    error = $"Slot index {slotIndex} out of bounds.";
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
                    error = $"Max capacity is {FixedListCapacity}, requested {n}.";
                    return false;
                }

                if (newValues == null || newValues.Count != n)
                {
                    error = "Stat values count does not match stat count.";
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

                    // 1. SecondaryStats
                    *(ushort*)(p + _offSecStats) = (ushort)n;
                    int* statsPtr = (int*)(p + _offSecStats + FixedListDataOffset);
                    for (int i = 0; i < n; i++) statsPtr[i] = (int)newStats[i];
                    for (int i = n; i < FixedListCapacity; i++) statsPtr[i] = 0;

                    // 2. StatValues
                    *(ushort*)(p + _offStatValues) = (ushort)n;
                    float* valsPtr = (float*)(p + _offStatValues + FixedListDataOffset);
                    for (int i = 0; i < n; i++) valsPtr[i] = newValues[i];
                    for (int i = n; i < FixedListCapacity; i++) valsPtr[i] = 0f;

                    // 3. StatUpgrades
                    *(ushort*)(p + _offStatUpgrades) = (ushort)n;
                    int* upgPtr = (int*)(p + _offStatUpgrades + FixedListDataOffset);
                    for (int i = 0; i < n; i++) upgPtr[i] = upgrades[i];
                    for (int i = n; i < FixedListCapacity; i++) upgPtr[i] = 0;

                    // 4. Counts & Formula
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

                    // 6. Signature re-baseline & recalculate bonuses
                    Rebaseline(player);
                    RecalcBonuses(player);

                    return true;
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    return false;
                }
            }

            public static Rune FreshBox(IntPtr data)
            {
                if (data == IntPtr.Zero) return null;
                EnsureOffsets();
                if (_runeClass == IntPtr.Zero) return null;
                IntPtr boxed = IL2CPP.il2cpp_value_box(_runeClass, data);
                if (boxed == IntPtr.Zero) return null;
                return new Rune(boxed);
            }

            public static unsafe void PreSign(Player player, IntPtr data)
            {
                try
                {
                    if (player._runeSignatureStore == null) return;
                    Rune boxed = FreshBox(data);
                    if (boxed == null) return;
                    FixedString64Bytes uuid = *(FixedString64Bytes*)(data + _offUUID);
                    int hmac = player.ComputeRuneHMAC(boxed);
                    player._runeSignatureStore[uuid] = hmac;
                }
                catch { }
            }

            public static void Rebaseline(Player player)
            {
                try
                {
                    if (player._runeSignatureStore != null)
                        player._runeSignatureStore.Clear();
                    player.RefreshAllRuneSignatures();
                    player.ValidateRuneState();
                }
                catch { }
            }

            public static void RecalcBonuses(Player player)
            {
                try
                {
                    if (player.Runes != null)
                        player.Runes.UpdateRuneBonuses();
                }
                catch { }
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

        // =================================================================
        // Stat Catalog & Slot Metadata
        // =================================================================
        public static class Catalog
        {
            public static readonly string[] SlotNames = new string[]
            {
                "Slot 0: Rune",
                "Slot 1: Undercoat",
                "Slot 2: Belt (Sash)",
                "Slot 3: Charm",
                "Slot 4: Ring 1",
                "Slot 5: Ring 2",
                "Slot 6: Amulet 1",
                "Slot 7: Amulet 2",
                "Slot 8: Badge 1",
                "Slot 9: Badge 2",
                "Slot 10: Bracer 1",
                "Slot 11: Bracer 2"
            };

            public static readonly List<Runes.Stat> AllStats = new();

            static Catalog()
            {
                var values = (Runes.Stat[])Enum.GetValues(typeof(Runes.Stat));
                var sorted = new List<string>();
                var dict = new Dictionary<string, Runes.Stat>(StringComparer.OrdinalIgnoreCase);

                foreach (var v in values)
                {
                    if (v == Runes.Stat.None) continue;
                    string name = v.ToString();
                    sorted.Add(name);
                    dict[name] = v;
                }
                sorted.Sort(StringComparer.OrdinalIgnoreCase);

                foreach (var s in sorted)
                    AllStats.Add(dict[s]);
            }

            public static List<Runes.Stat> Filter(string query)
            {
                if (string.IsNullOrWhiteSpace(query)) return AllStats;
                string q = query.Trim();
                var list = new List<Runes.Stat>();
                foreach (var s in AllStats)
                {
                    if (s.ToString().IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                        list.Add(s);
                }
                return list;
            }
        }

        // =================================================================
        // In-game GUI Presentation
        // =================================================================
        public static class UI
        {
            public static bool IsOpen = false;

            private static int _selectedSlot = 2; // Default to Belt (Sash)
            private static int _lastLoadedSlot = -1;

            private static readonly List<Runes.Stat> _editStats = new();
            private static readonly List<string> _editValueStrings = new();
            private static readonly List<int> _editUpgrades = new();

            private static int _pickerRow = -1;
            private static string _searchQuery = "";
            private static int _pickerPage = 0;
            /* [2026-09-26 00:54:00] Obsolete: private static Vector2 _pickerScroll = Vector2.zero; */
            private static Vector2 _leftScroll = Vector2.zero;
            private static Vector2 _rightScroll = Vector2.zero;

            private static string _statusMsg = "";
            private static float _statusTimer = 0f;

            private static bool _stylesReady = false;
            private static readonly List<Texture2D> _textures = new();
            private static GUIStyle _sPanelBg;
            private static GUIStyle _sTitle;
            private static GUIStyle _sSub;
            private static GUIStyle _sSlotBtn;
            private static GUIStyle _sSlotBtnActive;
            private static GUIStyle _sSlotBtnEmpty;
            private static GUIStyle _sLabel;
            private static GUIStyle _sLabelBold;
            private static GUIStyle _sBtnAction;
            private static GUIStyle _sBtnGreen;
            private static GUIStyle _sBtnBlue;
            private static GUIStyle _sBtnRed;
            private static GUIStyle _sCloseBtn;
            private static GUIStyle _sDivider;
            private static GUIStyle _sTextField;
            private static GUIStyle _sPickerBg;

            public static void Toggle()
            {
                IsOpen = !IsOpen;
                if (IsOpen)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    _lastLoadedSlot = -1;
                    _pickerRow = -1;
                    Memory.EnsureOffsets();
                    AutoSelectFirstEquipped();
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            private static void AutoSelectFirstEquipped()
            {
                try
                {
                    var player = Memory.GetLocalPlayer();
                    if (player?.EquipmentWorn != null)
                    {
                        for (int i = 0; i < player.EquipmentWorn.Count; i++)
                        {
                            var rune = player.EquipmentWorn[i];
                            if (Memory.IsRuneEquipped(rune))
                            {
                                var curRune = player.EquipmentWorn[_selectedSlot];
                                if (!Memory.IsRuneEquipped(curRune))
                                {
                                    _selectedSlot = i;
                                }
                                break;
                            }
                        }
                    }
                }
                catch { }
            }

            public static void Draw()
            {
                if (!IsOpen) return;
                InitStyles();

                int savedDepth = GUI.depth;
                bool savedEnabled = GUI.enabled;

                try
                {
                    GUI.depth = -3000;
                    GUI.enabled = true;

                    float w = 780f;
                    float h = 640f;
                    float x = (Screen.width - w) * 0.5f;
                    float y = (Screen.height - h) * 0.5f;

                    // Panel background
                    GUI.Label(new Rect(x, y, w, h), GUIContent.none, _sPanelBg);
                    GUI.Label(new Rect(x, y, w, 4f), GUIContent.none, _sDivider);

                    // Header
                    GUI.Label(new Rect(x + 20f, y + 10f, 450f, 24f), "EQUIPPED STAT MODIFIER", _sTitle);
                    GUI.Label(new Rect(x + 20f, y + 34f, 500f, 18f),
                        "Detects equipped gear in real-time. Change secondary stat affixes & values, then apply.", _sSub);

                    if (GUI.Button(new Rect(x + w - 45f, y + 12f, 30f, 26f), "X", _sCloseBtn))
                    {
                        Toggle();
                        return;
                    }

                    var player = Memory.GetLocalPlayer();
                    if (player == null || player.EquipmentWorn == null)
                    {
                        GUI.Label(new Rect(x + 20f, y + 80f, w - 40f, 40f),
                            "<color=#FF8888>Waiting for player to load into game world...</color>", _sLabelBold);
                        return;
                    }

                    SyncWorkingCopy(player);

                    // Left slots column
                    float leftX = x + 16f;
                    float leftY = y + 60f;
                    float leftW = 230f;
                    float leftH = h - 100f;

                    DrawSlots(player, leftX, leftY, leftW, leftH);

                    // Vertical divider
                    GUI.Label(new Rect(leftX + leftW + 8f, leftY, 2f, leftH), GUIContent.none, _sDivider);

                    // Right editor panel
                    float rightX = leftX + leftW + 18f;
                    float rightY = leftY;
                    float rightW = w - (rightX - x) - 16f;
                    float rightH = leftH;

                    DrawEditor(player, rightX, rightY, rightW, rightH);

                    // Status bar
                    if (_statusTimer > 0f)
                    {
                        _statusTimer -= Time.unscaledDeltaTime;
                        GUI.Label(new Rect(x + 20f, y + h - 28f, w - 40f, 20f), _statusMsg, _sLabel);
                    }
                    else
                    {
                        GUI.Label(new Rect(x + 20f, y + h - 28f, w - 40f, 20f),
                            "<color=#888888>Press F8 to toggle | Edits modify memory directly and recalculate bonuses</color>", _sSub);
                    }

                    if (_pickerRow != -1)
                        DrawPicker(x, y, w, h);
                }
                finally
                {
                    GUI.depth = savedDepth;
                    GUI.enabled = savedEnabled;
                }
            }

            private static void SyncWorkingCopy(Player player)
            {
                if (_selectedSlot == _lastLoadedSlot) return;

                _lastLoadedSlot = _selectedSlot;
                _editStats.Clear();
                _editValueStrings.Clear();
                _editUpgrades.Clear();

                if (_selectedSlot < 0 || _selectedSlot >= player.EquipmentWorn.Count) return;

                Rune rune = player.EquipmentWorn[_selectedSlot];
                if (!Memory.IsRuneEquipped(rune)) return;

                var snap = Memory.Read(rune);
                for (int i = 0; i < snap.SecondaryStats.Count; i++)
                {
                    _editStats.Add(snap.SecondaryStats[i]);
                    float val = i < snap.StatValues.Count ? snap.StatValues[i] : 0f;
                    _editValueStrings.Add(val.ToString("0.##", CultureInfo.InvariantCulture));
                    int upg = i < snap.StatUpgrades.Count ? snap.StatUpgrades[i] : 0;
                    _editUpgrades.Add(upg);
                }
            }

            private static void DrawSlots(Player player, float x, float y, float width, float height)
            {
                GUI.Label(new Rect(x, y, width, 22f), "<b>EQUIPPED SLOTS</b>", _sLabelBold);
                y += 24f;
                height -= 24f;

                int slotCount = player.EquipmentWorn.Count;

                /* [2026-09-26 00:54:00] Obsolete: GUI.BeginScrollView is stripped from the game's Il2Cpp build.
                Rect viewRect = new Rect(x, y, width, height);
                float contentHeight = slotCount * 34f;
                Rect scrollContentRect = new Rect(0, 0, width - 16f, contentHeight);
                _leftScroll = GUI.BeginScrollView(viewRect, _leftScroll, scrollContentRect);
                */

                for (int i = 0; i < slotCount; i++)
                {
                    float itemY = y + (i * 34f);
                    bool isCur = (_selectedSlot == i);

                    Rune rune = player.EquipmentWorn[i];
                    bool hasItem = Memory.IsRuneEquipped(rune);

                    string slotTitle = i < Catalog.SlotNames.Length ? Catalog.SlotNames[i] : $"Slot {i}";
                    string btnText;
                    if (hasItem)
                    {
                        var snap = Memory.Read(rune);
                        string lvlText = snap.Level > 0 ? $" (+{snap.Level})" : "";
                        btnText = isCur ? $"▶ {slotTitle}{lvlText}" : $"● {slotTitle}{lvlText}";
                    }
                    else
                    {
                        btnText = isCur ? $"▶ {slotTitle} (Empty)" : $"○ {slotTitle} (Empty)";
                    }
                    GUIStyle style = isCur ? _sSlotBtnActive : (hasItem ? _sSlotBtn : _sSlotBtnEmpty);

                    if (GUI.Button(new Rect(x, itemY, width - 20f, 30f), btnText, style))
                    {
                        _selectedSlot = i;
                        _pickerRow = -1;
                        SyncWorkingCopy(player);
                    }
                }

                /* [2026-09-26 00:54:00] Obsolete: Removed along with BeginScrollView
                GUI.EndScrollView();
                */
            }

            private static void DrawEditor(Player player, float x, float y, float width, float height)
            {
                if (_selectedSlot < 0 || _selectedSlot >= player.EquipmentWorn.Count) return;

                Rune rune = player.EquipmentWorn[_selectedSlot];
                if (!Memory.IsRuneEquipped(rune))
                {
                    string slotName = _selectedSlot < Catalog.SlotNames.Length ? Catalog.SlotNames[_selectedSlot] : $"Slot {_selectedSlot}";
                    GUI.Label(new Rect(x, y + 20f, width, 30f), $"<b>{slotName}</b> is currently empty.", _sLabelBold);
                    GUI.Label(new Rect(x, y + 50f, width, 50f),
                        "Equip an item into this slot in-game to inspect and edit its secondary stats.\n(Select an equipped slot from the left list to view active items).", _sSub);
                    return;
                }

                var snap = Memory.Read(rune);
                string slotTitle = _selectedSlot < Catalog.SlotNames.Length ? Catalog.SlotNames[_selectedSlot] : $"Slot {_selectedSlot}";

                GUI.Label(new Rect(x, y, width, 22f), $"<b>ITEM: {slotTitle.ToUpper()}</b>", _sTitle);
                y += 24f;

                string infoText = $"Level: <color=#FFDD44>+{snap.Level}</color>  |  " +
                                  $"Stars: <color=#FFDD44>{snap.Stars}</color>  |  " +
                                  $"Rarity: <color=#FFAA55>{snap.Rarity}</color>  |  " +
                                  $"Set: <color=#55FFFF>{snap.Set}</color>";
                GUI.Label(new Rect(x, y, width, 20f), infoText, _sLabel);
                y += 20f;

                string primaryInfo = $"Primary Stat: <color=#55FF88><b>{snap.PrimaryStat}</b></color> (Core item stat)";
                GUI.Label(new Rect(x, y, width, 20f), primaryInfo, _sLabel);
                y += 24f;

                // Secondary Stats Header
                GUI.Label(new Rect(x, y + 2f, 220f, 24f),
                    $"<b>Secondary Stats ({_editStats.Count} / {Memory.FixedListCapacity}):</b>", _sLabelBold);

                if (_editStats.Count < Memory.FixedListCapacity)
                {
                    if (GUI.Button(new Rect(x + width - 150f, y, 140f, 24f), "+ Add Secondary Stat", _sBtnAction))
                    {
                        Runes.Stat candidate = Runes.Stat.Health;
                        foreach (var s in Catalog.AllStats)
                        {
                            if (s != snap.PrimaryStat && !_editStats.Contains(s))
                            {
                                candidate = s;
                                break;
                            }
                        }
                        _editStats.Add(candidate);
                        _editValueStrings.Add("10");
                        _editUpgrades.Add(0);
                    }
                }
                y += 32f;

                // Scrollable affixes
                /* [2026-09-26 00:54:00] Obsolete: GUI.BeginScrollView is stripped from the game's Il2Cpp build.
                Rect viewRect = new Rect(x, y, width, 300f);
                float contentH = Mathf.Max(300f, _editStats.Count * 46f + 10f);
                Rect scrollContent = new Rect(0, 0, width - 16f, contentH);
                _rightScroll = GUI.BeginScrollView(viewRect, _rightScroll, scrollContent);
                */

                for (int i = 0; i < _editStats.Count; i++)
                {
                    float rowY = y + (i * 46f) + 4f;
                    DrawRow(i, x, rowY, width - 20f);
                }

                /* [2026-09-26 00:54:00] Obsolete: Removed along with BeginScrollView
                GUI.EndScrollView();
                */
                y += 306f;

                // Actions
                float btnH = 34f;
                float btnW = (width - 20f) / 3f;

                if (GUI.Button(new Rect(x, y, btnW, btnH), "✓ Apply to Item", _sBtnGreen))
                {
                    Apply(player);
                }

                if (GUI.Button(new Rect(x + btnW + 10f, y, btnW, btnH), "💾 Save Character", _sBtnBlue))
                {
                    if (Memory.TriggerSave(player, out string saveMsg))
                        _statusMsg = $"<color=#55FF55>{saveMsg}</color>";
                    else
                        _statusMsg = $"<color=#FF5555>{saveMsg}</color>";
                    _statusTimer = 3.5f;
                }

                if (GUI.Button(new Rect(x + (btnW + 10f) * 2, y, btnW - 10f, btnH), "↺ Revert", _sBtnAction))
                {
                    _lastLoadedSlot = -1;
                    SyncWorkingCopy(player);
                    _statusMsg = "<color=#55FFFF>Reloaded stats from item in memory.</color>";
                    _statusTimer = 2.5f;
                }
            }

            private static void DrawRow(int index, float x, float y, float width)
            {
                string statName = _editStats[index].ToString();
                string valStr = index < _editValueStrings.Count ? _editValueStrings[index] : "0";

                GUI.Label(new Rect(x, y + 4f, 26f, 26f), $"#{index + 1}", _sSub);

                float pickX = x + 30f;
                if (GUI.Button(new Rect(pickX, y + 2f, 175f, 28f), $"▼ {statName}", _sBtnAction))
                {
                    _pickerRow = index;
                    _searchQuery = "";
                    /* [2026-09-26 00:54:00] Obsolete: _pickerScroll = Vector2.zero; */
                    _pickerPage = 0;
                }

                float stepX = pickX + 182f;
                if (GUI.Button(new Rect(stepX, y + 2f, 32f, 28f), "-10", _sBtnAction))
                    AdjustVal(index, -10f);

                if (GUI.Button(new Rect(stepX + 34f, y + 2f, 28f, 28f), "-1", _sBtnAction))
                    AdjustVal(index, -1f);

                float textX = stepX + 64f;
                string newVal = GUI.TextField(new Rect(textX, y + 4f, 60f, 24f), valStr, _sTextField);
                if (newVal != valStr)
                    _editValueStrings[index] = newVal;

                float addX = textX + 64f;
                if (GUI.Button(new Rect(addX, y + 2f, 28f, 28f), "+1", _sBtnAction))
                    AdjustVal(index, +1f);

                if (GUI.Button(new Rect(addX + 30f, y + 2f, 34f, 28f), "+10", _sBtnAction))
                    AdjustVal(index, +10f);

                float delX = addX + 68f;
                if (GUI.Button(new Rect(delX, y + 2f, 28f, 28f), "✕", _sBtnRed))
                {
                    _editStats.RemoveAt(index);
                    _editValueStrings.RemoveAt(index);
                    if (index < _editUpgrades.Count) _editUpgrades.RemoveAt(index);
                    if (_pickerRow == index) _pickerRow = -1;
                }
            }

            private static void AdjustVal(int index, float delta)
            {
                if (index < 0 || index >= _editValueStrings.Count) return;
                string str = _editValueStrings[index];
                if (!float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out float cur))
                    cur = 0f;
                float updated = (float)Math.Round(cur + delta, 2);
                if (updated < 0f && delta < 0f) updated = 0f;
                _editValueStrings[index] = updated.ToString(CultureInfo.InvariantCulture);
            }

            private static void Apply(Player player)
            {
                if (_selectedSlot < 0 || _selectedSlot >= player.EquipmentWorn.Count) return;

                var parsed = new List<float>(_editStats.Count);
                for (int i = 0; i < _editStats.Count; i++)
                {
                    string s = i < _editValueStrings.Count ? _editValueStrings[i] : "0";
                    if (!float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float f) ||
                        float.IsNaN(f) || float.IsInfinity(f))
                    {
                        _statusMsg = $"<color=#FF5555>Error: Invalid number '{s}' on affix #{i + 1}.</color>";
                        _statusTimer = 4f;
                        return;
                    }
                    parsed.Add((float)Math.Round(f, 2));
                }

                var seen = new HashSet<Runes.Stat>();
                for (int i = 0; i < _editStats.Count; i++)
                {
                    if (!seen.Add(_editStats[i]))
                    {
                        _statusMsg = $"<color=#FF5555>Error: Duplicate secondary '{_editStats[i]}' not allowed.</color>";
                        _statusTimer = 4f;
                        return;
                    }
                }

                bool ok = Memory.ApplySecondaryStats(player, _selectedSlot, _editStats, parsed, _editUpgrades, out string err);
                if (ok)
                {
                    string slotName = _selectedSlot < Catalog.SlotNames.Length ? Catalog.SlotNames[_selectedSlot] : $"Slot {_selectedSlot}";
                    _statusMsg = $"<color=#55FF55>✓ Successfully modified {slotName}! Stats updated in-game.</color>";
                }
                else
                {
                    _statusMsg = $"<color=#FF5555>Apply failed: {err}</color>";
                }
                _statusTimer = 4f;
            }

            private static void DrawPicker(float px, float py, float pw, float ph)
            {
                float modalW = 420f;
                float modalH = 460f;
                float mx = px + (pw - modalW) * 0.5f;
                float my = py + (ph - modalH) * 0.5f;

                GUI.Label(new Rect(mx, my, modalW, modalH), GUIContent.none, _sPickerBg);
                GUI.Label(new Rect(mx, my, modalW, 3f), GUIContent.none, _sDivider);

                GUI.Label(new Rect(mx + 15f, my + 10f, 300f, 22f), $"<b>CHOOSE STAT (Affix #{_pickerRow + 1})</b>", _sTitle);

                if (GUI.Button(new Rect(mx + modalW - 35f, my + 10f, 25f, 22f), "✕", _sCloseBtn))
                {
                    _pickerRow = -1;
                    return;
                }

                GUI.Label(new Rect(mx + 15f, my + 38f, 60f, 24f), "Search:", _sLabel);
                _searchQuery = GUI.TextField(new Rect(mx + 80f, my + 38f, modalW - 100f, 24f), _searchQuery, _sTextField);

                var matches = Catalog.Filter(_searchQuery);
                float listY = my + 70f;
                float listH = modalH - 120f; // Reduced to make room for pagination buttons

                /* [2026-09-26 00:54:00] Obsolete: GUI.BeginScrollView causes NotSupportedException in this IL2CPP build.
                Rect viewRect = new Rect(mx + 15f, listY, modalW - 30f, listH);
                float contentH = Mathf.Max(listH, matches.Count * 28f + 10f);
                Rect scrollContent = new Rect(0, 0, modalW - 50f, contentH);
                _pickerScroll = GUI.BeginScrollView(viewRect, _pickerScroll, scrollContent);
                */

                int itemsPerPage = 11;
                int totalPages = Mathf.CeilToInt((float)matches.Count / itemsPerPage);
                if (_pickerPage >= totalPages && totalPages > 0) _pickerPage = totalPages - 1;
                if (_pickerPage < 0) _pickerPage = 0;

                int startIndex = _pickerPage * itemsPerPage;
                int endIndex = Mathf.Min(startIndex + itemsPerPage, matches.Count);

                for (int i = startIndex; i < endIndex; i++)
                {
                    var s = matches[i];
                    string sName = s.ToString();
                    float itemY = listY + ((i - startIndex) * 28f);

                    bool isCurrent = (_pickerRow < _editStats.Count && _editStats[_pickerRow] == s);
                    string label = isCurrent ? $"<b><color=#55FF55>✓ {sName}</color></b>" : sName;

                    if (GUI.Button(new Rect(mx + 15f, itemY, modalW - 30f, 26f), label, isCurrent ? _sSlotBtnActive : _sBtnAction))
                    {
                        if (_pickerRow >= 0 && _pickerRow < _editStats.Count)
                            _editStats[_pickerRow] = s;
                        _pickerRow = -1;
                        _pickerPage = 0; // reset
                        break;
                    }
                }

                /* [2026-09-26 00:54:00] Obsolete: Removed along with BeginScrollView
                GUI.EndScrollView();
                */

                // Pagination Controls
                float pageY = my + modalH - 35f;
                if (GUI.Button(new Rect(mx + 15f, pageY, 100f, 24f), "Prev Page", _sBtnAction))
                {
                    if (_pickerPage > 0) _pickerPage--;
                }
                GUI.Label(new Rect(mx + 160f, pageY, 100f, 24f), $"Page {_pickerPage + 1} / {Mathf.Max(1, totalPages)}", _sLabel);
                if (GUI.Button(new Rect(mx + modalW - 115f, pageY, 100f, 24f), "Next Page", _sBtnAction))
                {
                    if (_pickerPage < totalPages - 1) _pickerPage++;
                }
            }

            private static Texture2D MakeTex(int w, int h, Color col)
            {
                var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
                t.hideFlags = HideFlags.DontSave;
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        t.SetPixel(x, y, col);
                    }
                }
                t.Apply();
                _textures.Add(t);
                return t;
            }

            private static void InitStyles()
            {
                if (_stylesReady) return;
                _stylesReady = true;

                _sPanelBg = new GUIStyle(GUI.skin.label);
                _sDivider = new GUIStyle(GUI.skin.label);
                _sPickerBg = new GUIStyle(GUI.skin.label);

                _sTitle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 17,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.95f, 0.85f, 0.40f, 1f) }
                };

                _sSub = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    richText = true,
                    normal = { textColor = new Color(0.70f, 0.68f, 0.65f, 1f) }
                };

                _sLabel = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    richText = true,
                    normal = { textColor = new Color(0.85f, 0.82f, 0.78f, 1f) }
                };

                _sLabelBold = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    richText = true,
                    normal = { textColor = new Color(0.95f, 0.92f, 0.85f, 1f) }
                };

                _sSlotBtn = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = new Color(0.90f, 0.90f, 0.90f, 1f) }
                };

                _sSlotBtnActive = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = Color.black }
                };

                _sSlotBtnEmpty = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Italic,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = new Color(0.55f, 0.52f, 0.48f, 1f) }
                };

                _sBtnAction = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };

                _sBtnGreen = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };

                _sBtnBlue = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };

                _sBtnRed = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };

                _sCloseBtn = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };

                _sTextField = new GUIStyle(GUI.skin.textField)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.yellow }
                };

                _sPanelBg.normal.background = MakeTex(1, 1, new Color(0.12f, 0.10f, 0.08f, 0.97f));
                _sPickerBg.normal.background = MakeTex(1, 1, new Color(0.15f, 0.13f, 0.11f, 0.99f));
                _sDivider.normal.background = MakeTex(1, 1, new Color(0.85f, 0.70f, 0.15f, 1f));

                _sSlotBtn.normal.background = MakeTex(1, 1, new Color(0.24f, 0.21f, 0.18f, 0.9f));
                _sSlotBtnActive.normal.background = MakeTex(1, 1, new Color(0.88f, 0.72f, 0.18f, 1f));
                _sSlotBtnEmpty.normal.background = MakeTex(1, 1, new Color(0.18f, 0.16f, 0.14f, 0.7f));

                _sBtnAction.normal.background = MakeTex(1, 1, new Color(0.28f, 0.25f, 0.22f, 1f));
                _sBtnGreen.normal.background = MakeTex(1, 1, new Color(0.18f, 0.58f, 0.25f, 1f));
                _sBtnBlue.normal.background = MakeTex(1, 1, new Color(0.18f, 0.42f, 0.68f, 1f));
                _sBtnRed.normal.background = MakeTex(1, 1, new Color(0.65f, 0.20f, 0.20f, 1f));
                _sCloseBtn.normal.background = MakeTex(1, 1, new Color(0.65f, 0.20f, 0.20f, 1f));
            }
        }
    }
}
