using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using UnityEngine;

namespace DimraethModPack.Modules.Loot
{
    /// <summary>
    /// [2026-09-30 09:15] Model "drop table khusus equipment": form nested jumlah drop equipment (rune)
    /// per tipe. Rantai fallback (paling spesifik menang), disetujui user 2026-09-29/30:
    ///   Set+Slot  &gt;  Set  &gt;  Slot  &gt;  Global
    /// Nilai = ABSOLUTE (tepat N keping per drop equipment). 0 pada level override = "inherit"
    /// (ikut level berikutnya). Contoh user: "goblin drop 1 cloth armor, input 3 -&gt; jadi 3".
    /// </summary>
    public static class EquipmentDropCounts
    {
        public const int Inherit = 0;
        public const int Min = 1;
        public const int Max = 99;

        // [2026-09-30 09:15] Indexed by (int)Runes.RuneSet (None=0 .. GoblinFriend=47; nilai 33
        // tidak dipakai game tapi array tetap digeser ke ukuran max+1) dan (int)Runes.SlotType
        // (Rune=0 .. Bracer=7).
        const int SetCount = 48;
        const int SlotCount = 8;

        static readonly int[] _slot = new int[SlotCount];
        static readonly int[] _set = new int[SetCount];
        static readonly int[,] _setSlot = new int[SetCount, SlotCount];

        static ConfigEntry<int> _globalEntry;
        static ConfigEntry<string> _slotEntry;
        static ConfigEntry<string> _setEntry;
        static ConfigEntry<string> _setSlotEntry;

        // UI state
        static bool _showSets;
        static readonly bool[] _setOpen = new bool[SetCount];

        public static void Bind(ConfigEntry<int> global, ConfigEntry<string> slots, ConfigEntry<string> sets, ConfigEntry<string> setSlots)
        {
            _globalEntry = global;
            _slotEntry = slots;
            _setEntry = sets;
            _setSlotEntry = setSlots;

            Array.Clear(_slot, 0, _slot.Length);
            Array.Clear(_set, 0, _set.Length);
            Array.Clear(_setSlot, 0, _setSlot.Length);

            ParsePairs(slots.Value, (name, v) =>
            {
                if (Enum.TryParse<Runes.SlotType>(name, out var t)) _slot[ClampIndex((int)t, SlotCount)] = Clamp(v);
            });
            ParsePairs(sets.Value, (name, v) =>
            {
                if (Enum.TryParse<Runes.RuneSet>(name, out var t)) _set[ClampIndex((int)t, SetCount)] = Clamp(v);
            });
            ParsePairs(setSlots.Value, (name, v) =>
            {
                var parts = name.Split('/');
                if (parts.Length == 2
                    && Enum.TryParse<Runes.RuneSet>(parts[0], out var s)
                    && Enum.TryParse<Runes.SlotType>(parts[1], out var t))
                {
                    _setSlot[ClampIndex((int)s, SetCount), ClampIndex((int)t, SlotCount)] = Clamp(v);
                }
            });
        }

        /// <summary>Nilai efektif untuk drop equipment tipe (set, slot): set+slot &gt; set &gt; slot &gt; global.</summary>
        public static int Resolve(Runes.RuneSet set, Runes.SlotType slot)
        {
            int s = (int)set;
            int t = (int)slot;
            if (s >= 0 && s < SetCount && t >= 0 && t < SlotCount)
            {
                int v = _setSlot[s, t];
                if (v > 0) return v;
                v = _set[s];
                if (v > 0) return v;
                v = _slot[t];
                if (v > 0) return v;
            }
            return Global;
        }

        // [2026-09-30 11:10] Resolusi untuk satu MonsterConfiguration: ambil nilai TERTINGGI di antara
        // semua kombinasi (RuneSets x Slots) yang diizinkan monster itu. Nilai ini dipakai untuk
        // meng-set MonsterConfiguration.RuneDropCount (loop multi-drop native game). Bila salah satu
        // daftar kosong/null, fallback ke Global (tidak menebak = tidak over-drop).
        public static int ResolveForConfig(global::MonsterConfiguration cfg)
        {
            if (cfg == null) return Global;
            var sets = cfg.RuneSets;
            var slots = cfg.Slots;
            int best = 0;
            if (sets != null && slots != null && sets.Count > 0 && slots.Count > 0)
            {
                for (int i = 0; i < sets.Count; i++)
                {
                    for (int j = 0; j < slots.Count; j++)
                    {
                        int v = Resolve(sets[i], slots[j]);
                        if (v > best) best = v;
                    }
                }
            }
            return best > 0 ? best : Global;
        }

        static int Global => _globalEntry != null ? Clamp(_globalEntry.Value) : 1;

        public static int GetSlot(int i) => _slot[ClampIndex(i, SlotCount)];
        public static void SetSlot(int i, int v) { _slot[ClampIndex(i, SlotCount)] = Clamp(v); Save(); }

        public static int GetSet(int i) => _set[ClampIndex(i, SetCount)];
        public static void SetSet(int i, int v) { _set[ClampIndex(i, SetCount)] = Clamp(v); Save(); }

        public static int GetSetSlot(int s, int t) => _setSlot[ClampIndex(s, SetCount), ClampIndex(t, SlotCount)];
        public static void SetSetSlot(int s, int t, int v) { _setSlot[ClampIndex(s, SetCount), ClampIndex(t, SlotCount)] = Clamp(v); Save(); }

        public static void ResetAll()
        {
            Array.Clear(_slot, 0, _slot.Length);
            Array.Clear(_set, 0, _set.Length);
            Array.Clear(_setSlot, 0, _setSlot.Length);
            if (_globalEntry != null) _globalEntry.Value = 1;
            Save();
        }

        // ----- config codec: "Name=3,Name2=1" / "Set/Slot=5" -----

        static void ParsePairs(string encoded, Action<string, int> apply)
        {
            if (string.IsNullOrEmpty(encoded)) return;
            foreach (var pair in encoded.Split(','))
            {
                var kv = pair.Split('=');
                if (kv.Length != 2) continue;
                if (int.TryParse(kv[1].Trim(), out int v)) apply(kv[0].Trim(), v);
            }
        }

        static void Save()
        {
            if (_slotEntry != null) _slotEntry.Value = EncodeSingle(_slot, i => ((Runes.SlotType)i).ToString());
            if (_setEntry != null) _setEntry.Value = EncodeSingle(_set, i => ((Runes.RuneSet)i).ToString());
            if (_setSlotEntry != null)
            {
                var sb = new StringBuilder();
                for (int s = 0; s < SetCount; s++)
                    for (int t = 0; t < SlotCount; t++)
                    {
                        int v = _setSlot[s, t];
                        if (v <= 0) continue;
                        if (sb.Length > 0) sb.Append(',');
                        sb.Append(((Runes.RuneSet)s).ToString()).Append('/').Append(((Runes.SlotType)t).ToString()).Append('=').Append(v);
                    }
                _setSlotEntry.Value = sb.ToString();
            }
        }

        static string EncodeSingle(int[] values, Func<int, string> nameOf)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                int v = values[i];
                if (v <= 0) continue;
                if (sb.Length > 0) sb.Append(',');
                sb.Append(nameOf(i)).Append('=').Append(v);
            }
            return sb.ToString();
        }

        static int Clamp(int v) => Math.Clamp(v, 0, Max);
        static int ClampIndex(int i, int size) => Math.Clamp(i, 0, size - 1);

        // ----- form UI (IMGUI) -----
        // [2026-09-30 09:15] Layout baris meniru ModModuleBase.DrawIntSpinner (label + 4 tombol
        // -10/-1/+1/+10) supaya konsisten, tapi membaca/menulis model in-memory (bukan ConfigEntry
        // per sel — 47x8 ConfigEntry tidak praktis), oleh itu helper lokal ini ada.

        public static float DrawNested(float x, float y, float width, GUIStyle labelStyle, GUIStyle btnStyle)
        {
            float curY = y;

            GUI.Label(new Rect(x, curY, width, 24f), "<color=#88AACC>-- Per Slot Type (0 = ikut level atas) --</color>", labelStyle);
            curY += 28f;
            foreach (Runes.SlotType slot in Enum.GetValues(typeof(Runes.SlotType)))
            {
                int idx = (int)slot;
                curY += DrawRow(x, curY, width, "  " + slot, () => GetSlot(idx), v => SetSlot(idx, v), btnStyle);
            }

            GUI.Label(new Rect(x, curY, width, 24f), "<color=#88AACC>-- Per Rune Set (nested: set lalu slot) --</color>", labelStyle);
            curY += 28f;
            if (GUI.Button(new Rect(x, curY, width - 175f, 24f), _showSets ? "[ v ] Tutup daftar set" : "[ > ] Buka daftar set", btnStyle))
                _showSets = !_showSets;
            curY += 28f;

            if (_showSets)
            {
                foreach (Runes.RuneSet set in Enum.GetValues(typeof(Runes.RuneSet)))
                {
                    if (set == Runes.RuneSet.None) continue;
                    int sIdx = (int)set;

                    // Baris set = sekaligus tombol foldout untuk 8 slot di dalamnya (nested).
                    if (GUI.Button(new Rect(x, curY, width - 175f, 24f), $"{(_setOpen[sIdx] ? "[ v ]" : "[ > ]")} {set}: {GetSet(sIdx)}", btnStyle))
                        _setOpen[sIdx] = !_setOpen[sIdx];
                    DrawRowButtons(x, curY, width, () => GetSet(sIdx), v => SetSet(sIdx, v), btnStyle);
                    curY += 28f;

                    if (_setOpen[sIdx])
                    {
                        foreach (Runes.SlotType slot in Enum.GetValues(typeof(Runes.SlotType)))
                        {
                            int tIdx = (int)slot;
                            curY += DrawRow(x, curY, width, "      " + slot, () => GetSetSlot(sIdx, tIdx), v => SetSetSlot(sIdx, tIdx, v), btnStyle);
                        }
                    }
                }
            }

            if (GUI.Button(new Rect(x, curY, width - 175f, 24f), "Reset Semua Equipment Count", btnStyle))
                ResetAll();
            curY += 28f;

            return curY - y;
        }

        static float DrawRow(float x, float y, float width, string label, Func<int> get, Action<int> set, GUIStyle btnStyle)
        {
            GUI.Label(new Rect(x, y, width - 175f, 24f), $"{label}: <color=#FFDD44>{get()}</color> <color=#888888>(0 = inherit)</color>", btnStyle);
            DrawRowButtons(x, y, width, get, set, btnStyle);
            return 28f;
        }

        static void DrawRowButtons(float x, float y, float width, Func<int> get, Action<int> set, GUIStyle btnStyle)
        {
            // [2026-09-30 09:20] Fix: tombol harus membaca nilai saat ini (get) lalu menulis nilai+delta,
            // bukan menulis delta mentah (versi awal salah: set(cur-10) dengan cur=0 menghasilkan -10).
            int cur = get();
            if (GUI.Button(new Rect(x + width - 170f, y, 40f, 24f), "-10", btnStyle)) set(cur - 10);
            if (GUI.Button(new Rect(x + width - 126f, y, 36f, 24f), "-1", btnStyle)) set(cur - 1);
            if (GUI.Button(new Rect(x + width - 86f, y, 36f, 24f), "+1", btnStyle)) set(cur + 1);
            if (GUI.Button(new Rect(x + width - 46f, y, 40f, 24f), "+10", btnStyle)) set(cur + 10);
        }
    }
}
