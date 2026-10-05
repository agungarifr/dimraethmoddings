using System;
using UnityEngine;

namespace DimraethModPack.Modules.Loot
{
    /// <summary>
    /// [2026-09-30 09:15] Runtime boost "drop table khusus equipment".
    /// Alur (disetujui user): saat drop equipment tipe (set, slot) digenerate di sumber loot
    /// (monster / chest box / quest &amp; reward), jadikan tepat N keping sesuai
    /// <see cref="EquipmentDropCounts.Resolve"/>. Keping ekstra = ROLL ULANG per keping
    /// (stat beda per keping) dengan MAX rarity &amp; MAX stars "yang diperbolehkan vanilla"
    /// (maksimum dari pool drop tersebut; fallback = rarity/stars trigger rune).
    /// Trigger keping juga diseragamkan ke max rarity/stars saat N&gt;1.
    ///
    /// Desain hook (bebas masalah double-enqueue, tanpa antrian):
    ///   1. Patch sumber (Patch_SrcFlag_*) set kedalaman "sedang generate loot" (BeginSource/EndSource).
    ///   2. Patch generate (Patch_Pools_*) menyimpan pool rarity/stars maksimal (NotePools).
    ///   3. Patch add (Patch_AddRuneToWorld / Patch_AddRuneToInventory) menambah N-1 keping ekstra
    ///      lewat jalur add yang SAMA dengan trigger. _busy mencegah keping ekstra ikut di-boost.
    /// </summary>
    public static class EquipmentDropBoost
    {
        static int _depth;
        static bool _busy;
        static bool _hasPools;
        static Runes.Rarity _maxRarity = Runes.Rarity.Ancient;
        static Runes.Stars _maxStars = Runes.Stars.Nine;
        static bool _empowered;

        public static bool Active => _depth > 0;

        public static void BeginSource() => _depth++;

        public static void EndSource()
        {
            if (--_depth <= 0)
            {
                _depth = 0;
                _hasPools = false;
                _empowered = false;
                // [2026-09-30 11:40] Kembalikan pool asli monster setelah drop selesai (opsi c').
                RestoreNarrowing();
                _hasForce = false;
            }
        }

        public static bool Enabled =>
            LootModV2Module.Instance != null && LootModV2Module.Instance.IsEnabled;

        /// <summary>Pool dari Runes.ReturnRandomRuneData(list rarity, list stars, empowered).</summary>
        public static void NotePools(
            Il2CppSystem.Collections.Generic.List<Runes.Rarity> rarities,
            Il2CppSystem.Collections.Generic.List<Runes.Stars> stars,
            bool empowered)
        {
            if (!Enabled || !Active || _busy) return;
            _maxRarity = MaxOf(rarities, Runes.Rarity.Ancient);
            _maxStars = MaxOf(stars, Runes.Stars.Nine);
            _empowered = empowered;
            _hasPools = true;
        }

        /// <summary>Pool dari Runes.GenerateRandomRune(RuneDrop) — chest box &amp; quest reward.</summary>
        public static void NotePools(RuneDrop drop)
        {
            if (!Enabled || !Active || _busy || drop == null) return;
            // Tooltip game: "If empty, all rarities/stars are valid" -> fallback = nilai enum tertinggi.
            _maxRarity = MaxOf(drop.AllowedRarities, Runes.Rarity.Ancient);
            _maxStars = MaxOf(drop.AllowedStars, Runes.Stars.Nine);
            _empowered = false;
            _hasPools = true;
        }

        /// <summary>
        /// Cek apakah trigger rune harus di-boost. Mengembalikan true + count = total keping,
        /// sekaligus menyeragamkan trigger ke max rarity/stars (keputusan user 2026-09-30:
        /// semua keping max rarity &amp; max star yang diperbolehkan vanilla).
        /// </summary>
        public static bool TryBeginBoost(ref Rune rune, out int count)
        {
            count = 0;
            if (!Enabled || _busy || !Active || rune.IsEmpty()) return false;
            count = EquipmentDropCounts.Resolve(rune.Set, rune.SlotType);
            if (count <= 1) return false;
            if (_hasPools)
            {
                rune.Rarity = _maxRarity;
                rune.Stars = _maxStars;
            }
            return true;
        }

        /// <summary>Keping ekstra: tipe (set, slot) sama, stat/rarity/stars di-roll ulang.</summary>
        public static Rune MakeCopy(Rune trigger)
        {
            try
            {
                var sets = new Il2CppSystem.Collections.Generic.List<Runes.RuneSet>();
                sets.Add(trigger.Set);
                var slots = new Il2CppSystem.Collections.Generic.List<Runes.SlotType>();
                slots.Add(trigger.SlotType);
                var rars = new Il2CppSystem.Collections.Generic.List<Runes.Rarity>();
                rars.Add(_hasPools ? _maxRarity : trigger.Rarity);
                var sts = new Il2CppSystem.Collections.Generic.List<Runes.Stars>();
                sts.Add(_hasPools ? _maxStars : trigger.Stars);
                return Runes.ReturnRandomRuneData(sets, slots, rars, sts, _empowered);
            }
            catch
            {
                return Rune.Empty;
            }
        }

        // =====================================================================
        // [2026-09-30 11:40] Opsi (c'): "tepat N per (set,slot)" TANPA menyentuh ServerRPC.
        // Desain memang menginginkan N keping bertipe (set,slot) SAMA dengan stat di-reroll.
        // Kita wujudkan dengan mengunci SATU tipe per drop di loop multi-drop native:
        //   1) PlanRuneDrop(): sample 1 tipe memakai RNG ber-bobot milik game
        //      (Runes.ReturnRandomRuneData), lalu N = EquipmentDropCounts.Resolve(set,slot).
        //   2) ApplyNarrowing(): persempit cfg.RuneSets/cfg.Slots monster ke tipe itu selama drop,
        //      sehingga loop native men-spawn tepat N keping tipe sama (tiap keping di-reroll).
        //   3) RestoreNarrowing(): kembalikan list asli (dipanggil di EndSource, saat depth 0).
        // Aman: hanya memakai patch RuneDropCheck (prefix/postfix) + Runes.ReturnRandomRuneData
        // (prefix). Tidak ada panggilan ke ServerRPC sama sekali.
        // =====================================================================
        static bool _sampling;
        static bool _hasForce;
        static Runes.RuneSet _forceSet;
        static Runes.SlotType _forceSlot;
        static MonsterConfiguration _narrowCfg;
        static Il2CppSystem.Collections.Generic.List<Runes.RuneSet> _origSets;
        static Il2CppSystem.Collections.Generic.List<Runes.SlotType> _origSlots;

        /// <summary>True saat mengambil sample tipe (agar log keping tidak ikut tercatat).</summary>
        public static bool Sampling => _sampling;

        /// <summary>Label tipe terkunci untuk diagnostik.</summary>
        public static string ForcedLabel => _hasForce ? _forceSet + "/" + _forceSlot : "none";

        /// <summary>Rencanakan drop: sample 1 tipe, kembalikan N yang harus diterapkan.</summary>
        public static int PlanRuneDrop(MonsterConfiguration cfg)
        {
            _hasForce = false;
            if (cfg == null) return 0;
            Rune sample = Rune.Empty;
            _sampling = true;
            try
            {
                sample = Runes.ReturnRandomRuneData(cfg.RuneSets, cfg.Slots, cfg.Rarities, cfg.Stars, false);
            }
            catch { }
            finally { _sampling = false; }

            if (!sample.IsEmpty())
            {
                _forceSet = sample.Set;
                _forceSlot = sample.SlotType;
                _hasForce = true;
                return EquipmentDropCounts.Resolve(_forceSet, _forceSlot);
            }
            // Fallback (gagal sample): pakai nilai tertinggi antar tipe yang diizinkan.
            return EquipmentDropCounts.ResolveForConfig(cfg);
        }

        /// <summary>Terapkan N + persempit pool monster (bila N&gt;1); restore nilai bila N&lt;=1.</summary>
        public static void ApplyNarrowing(MonsterConfiguration cfg, int n, int origCount)
        {
            RestoreNarrowing();
            if (cfg == null) return;
            if (!_hasForce || n <= 1)
            {
                cfg.RuneDropCount = origCount;
                return;
            }
            _narrowCfg = cfg;
            _origSets = cfg.RuneSets;
            _origSlots = cfg.Slots;
            var sets = new Il2CppSystem.Collections.Generic.List<Runes.RuneSet>();
            sets.Add(_forceSet);
            var slots = new Il2CppSystem.Collections.Generic.List<Runes.SlotType>();
            slots.Add(_forceSlot);
            cfg.RuneSets = sets;
            cfg.Slots = slots;
            cfg.RuneDropCount = n;
        }

        /// <summary>Kembalikan cfg.RuneSets/Slots asli. Tidak mengubah _hasForce.</summary>
        public static void RestoreNarrowing()
        {
            if (_narrowCfg != null)
            {
                try
                {
                    _narrowCfg.RuneSets = _origSets;
                    _narrowCfg.Slots = _origSlots;
                }
                catch { }
            }
            _narrowCfg = null;
            _origSets = null;
            _origSlots = null;
        }

        public static void DrainWorld(ServerRPC rpc, Rune trigger, int count,
            Areas.Scene scene, Vector2 position, ulong networkId, bool enforcePersonalLoot, bool neverShare)
        {
            _busy = true;
            try
            {
                for (int i = 1; i < count; i++)
                {
                    var copy = MakeCopy(trigger);
                    // [2026-09-30 10:42] Obsolete: memanggil RPC yang di-patch langsung dari dalam
                    // patch-nya -> re-enter detour HarmonyX -> StackOverflow. Diganti panggilan ke
                    // method asli lewat reverse patch (Patch_AddRuneToWorld.CallOriginal).
                    // if (!copy.IsEmpty()) rpc.AddRuneToWorldServerRpc(copy, scene, position, networkId, enforcePersonalLoot, neverShare);
                    // [2026-09-30 11:10] Obsolete & BERBAHAYA (CRASH): reverse patch pun memakai jalur
                    // CopyOriginal yang sama, jadi tetap re-enter detour -> StackOverflow. DrainWorld
                    // tidak dipakai lagi; jumlah drop kini via cfg.RuneDropCount di RuneDropCheck.
                    // if (!copy.IsEmpty()) Patch_AddRuneToWorld.CallOriginal(rpc, copy, scene, position, networkId, enforcePersonalLoot, neverShare);
                }
            }
            catch { }
            finally { _busy = false; }
        }

        public static void DrainInventory(Inventory inv, Rune trigger, int count)
        {
            _busy = true;
            try
            {
                for (int i = 1; i < count; i++)
                {
                    var copy = MakeCopy(trigger);
                    // [2026-09-30 10:42] Obsolete: `inv.AddRuneToInventory(copy)` langsung -> re-enter
                    // detour HarmonyX. Diganti panggilan method asli lewat reverse patch.
                    // if (!copy.IsEmpty()) inv.AddRuneToInventory(copy);
                    // [2026-09-30 11:10] Obsolete & BERBAHAYA (CRASH): reverse patch memakai jalur
                    // CopyOriginal yang sama -> tetap re-enter detour. DrainInventory tidak dipakai lagi.
                    // if (!copy.IsEmpty()) Patch_AddRuneToInventory.CallOriginal(inv, copy);
                }
            }
            catch { }
            finally { _busy = false; }
        }

        static Runes.Rarity MaxOf(Il2CppSystem.Collections.Generic.List<Runes.Rarity> list, Runes.Rarity fallback)
        {
            if (list == null || list.Count == 0) return fallback;
            Runes.Rarity max = list[0];
            for (int i = 1; i < list.Count; i++) if (list[i] > max) max = list[i];
            return max;
        }

        static Runes.Stars MaxOf(Il2CppSystem.Collections.Generic.List<Runes.Stars> list, Runes.Stars fallback)
        {
            if (list == null || list.Count == 0) return fallback;
            Runes.Stars max = list[0];
            for (int i = 1; i < list.Count; i++) if (list[i] > max) max = list[i];
            return max;
        }
    }
}
