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
                // [2026-10-06 09:00] OBSOLETE: _hasForce (satu tipe) digantikan _hasPlan/ClearPlan.
                // _hasForce = false;
                ClearPlan();
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
        // [2026-10-06 09:00] DESAIN BARU (permintaan user 2026-10-06): "semua slot dari SATU set".
        // Menggantikan opsi (c') lama "tepat N keping untuk SATU (set,slot)":
        //   1) PlanRuneDrop(): pilih SATU set acak (peluang sama antar set monster), lalu kumpulkan
        //      SEMUA slot = cfg.Slots ∩ slot yang didukung set itu (RuneSetDefinition.SlotDefinitions),
        //      tiap slot digandakan sesuai EquipmentDropCounts.Resolve(set, slot).
        //   2) ApplyPlan(): set cfg.RuneDropCount = total keping rencana. cfg.RuneSets/Slots monster
        //      TIDAK diubah; tipe setiap keping dipaksa lewat prefix ReturnRandomRuneData.
        //   3) ApplyPlannedType(): prefix Runes.ReturnRandomRuneData memberi tipe (set,slot) rencana
        //      satu per keping -> satu kill = seluruh slot set terpilih.
        // Alasan ganti: desain lama mengunci satu tipe sehingga slot lain (Bracer/Ring/Charm/…)
        // tak pernah bisa muncul walaupun monster mengizinkannya.
        // Desain lama (disimpan untuk rujukan):
        //   - PlanRuneDrop() menyampling 1 tipe via Runes.ReturnRandomRuneData.
        //   - ApplyNarrowing() mempersempit cfg.RuneSets/cfg.Slots ke tipe itu; loop native spawn N.
        //   - RestoreNarrowing() mengembalikan list asli (dipanggil di EndSource).
        // =====================================================================
        // [2026-10-06 09:00] OBSOLETE: _sampling tak lagi dipakai — PlanRuneDrop kini memilih set
        // langsung (tanpa sampling via Runes.ReturnRandomRuneData), jadi tidak ada log keping yang
        // perlu disembunyikan. Digantikan guard _hasPlan di bawah.
        // static bool _sampling;
        static bool _hasPlan;
        static readonly System.Collections.Generic.List<PlannedSlot> _plan =
            new System.Collections.Generic.List<PlannedSlot>();
        static int _planCursor;
        static MonsterConfiguration _planCfg;
        static int _planOrigCount;

        struct PlannedSlot
        {
            public Runes.RuneSet Set;
            public Runes.SlotType Slot;
        }

        // [2026-10-06 09:00] OBSOLETE (lihat catatan _sampling di atas):
        // /// <summary>True saat mengambil sample tipe (agar log keping tidak ikut tercatat).</summary>
        // public static bool Sampling => _sampling;

        // [2026-10-06 09:00] OBSOLETE: label satu-tipe (ForcedLabel) digantikan PlanLabel karena
        // rencana kini memuat banyak tipe. Kode lama disimpan untuk rujukan:
        // public static string ForcedLabel => _hasForce ? _forceSet + "/" + _forceSlot : "none";

        /// <summary>Label rencana untuk diagnostik (tipe pertama + jumlah keping).</summary>
        public static string PlanLabel
        {
            get
            {
                if (!_hasPlan || _plan.Count == 0) return "none";
                return _plan[0].Set + "/" + _plan[0].Slot + " x" + _plan.Count;
            }
        }

        /// <summary>Rencanakan drop: pilih 1 set (peluang sama), kumpulkan semua slot viable-nya.</summary>
        public static int PlanRuneDrop(MonsterConfiguration cfg)
        {
            RestoreNarrowing();
            ClearPlan();
            if (cfg == null || cfg.RuneSets == null || cfg.RuneSets.Count == 0
                || cfg.Slots == null || cfg.Slots.Count == 0) return 0;

            // [2026-10-06 09:00] Peluang sama antar set. Bila set terpilih tak beririsan dengan
            // cfg.Slots, coba set berikutnya (rotasi) agar drop tidak kosong.
            int setCount = cfg.RuneSets.Count;
            int start = UnityEngine.Random.Range(0, setCount);
            Runes.RuneSet chosen = default(Runes.RuneSet);
            Il2CppSystem.Collections.Generic.List<Runes.SlotType> viable = null;
            for (int k = 0; k < setCount; k++)
            {
                Runes.RuneSet set = cfg.RuneSets[(start + k) % setCount];
                Il2CppSystem.Collections.Generic.List<Runes.SlotType> v = TryViableSlots(set, cfg.Slots);
                if (v != null && v.Count > 0)
                {
                    chosen = set;
                    viable = v;
                    break;
                }
            }
            if (viable == null || viable.Count == 0) return 0;

            for (int i = 0; i < viable.Count; i++)
            {
                Runes.SlotType slot = viable[i];
                int copies = EquipmentDropCounts.Resolve(chosen, slot);
                if (copies < 1) copies = 1;
                for (int c = 0; c < copies; c++)
                {
                    PlannedSlot entry = default(PlannedSlot);
                    entry.Set = chosen;
                    entry.Slot = slot;
                    _plan.Add(entry);
                }
            }
            if (_plan.Count == 0) return 0;
            _hasPlan = true;
            return _plan.Count;
        }

        static Il2CppSystem.Collections.Generic.List<Runes.SlotType> TryViableSlots(
            Runes.RuneSet set, Il2CppSystem.Collections.Generic.List<Runes.SlotType> slots)
        {
            // [2026-10-06 09:00] Irisan manual cfg.Slots ∩ slot yang didukung set (setara
            // Runes.ViableSlotsFor(..., warnOnFallback:false), tapi lolos dari kendala tipe
            // IEnumerable interop). ViableSlotsFor sendiri hanya membandingkan SlotType, jadi
            // kita memakai sumber data yang sama (RuneSetDefinition.SlotDefinitions).
            try
            {
                RuneManager mgr = RuneManager.Singleton;
                if (mgr == null) return null;
                RuneSetDefinition def = mgr.GetRuneSetDefinition(set);
                if (def == null || def.SlotDefinitions == null) return null;
                var result = new Il2CppSystem.Collections.Generic.List<Runes.SlotType>();
                for (int i = 0; i < slots.Count; i++)
                {
                    Runes.SlotType slot = slots[i];
                    for (int j = 0; j < def.SlotDefinitions.Count; j++)
                    {
                        RuneSlotDefinition sd = def.SlotDefinitions[j];
                        if (sd != null && sd.SlotType == slot)
                        {
                            result.Add(slot);
                            break;
                        }
                    }
                }
                return result;
            }
            catch { return null; }
        }

        /// <summary>Ambil tipe (set,slot) berikutnya untuk satu keping native.</summary>
        public static bool TakePlannedType(out Runes.RuneSet set, out Runes.SlotType slot)
        {
            set = default(Runes.RuneSet);
            slot = default(Runes.SlotType);
            if (!_hasPlan || _plan.Count == 0) return false;
            // [2026-10-06 09:00] Monster "empowered" menggandakan RuneDropCount di RuneDropCheck; wrap
            // agar keping ekstra tetap di dalam set terpilih (bukan tipe acak dari seluruh pool).
            PlannedSlot entry = _plan[_planCursor % _plan.Count];
            _planCursor++;
            set = entry.Set;
            slot = entry.Slot;
            return true;
        }

        /// <summary>Prefix Runes.ReturnRandomRuneData: paksa tipe (set,slot) rencana per keping.</summary>
        public static void ApplyPlannedType(
            ref Il2CppSystem.Collections.Generic.List<Runes.RuneSet> runeSets,
            ref Il2CppSystem.Collections.Generic.List<Runes.SlotType> slotTypes)
        {
            // [2026-10-06 09:00] Guard _sampling dihapus (kini selalu false / tak dipakai).
            if (!Enabled || !Active || !_hasPlan) return;
            Runes.RuneSet set;
            Runes.SlotType slot;
            if (!TakePlannedType(out set, out slot)) return;
            var sets = new Il2CppSystem.Collections.Generic.List<Runes.RuneSet>();
            sets.Add(set);
            var slots = new Il2CppSystem.Collections.Generic.List<Runes.SlotType>();
            slots.Add(slot);
            runeSets = sets;
            slotTypes = slots;
        }

        /// <summary>Terapkan total keping rencana; restore count asli bila tak ada rencana.</summary>
        public static void ApplyPlan(MonsterConfiguration cfg, int n, int origCount)
        {
            RestoreNarrowing();
            if (cfg == null) return;
            if (!_hasPlan || n <= 0)
            {
                cfg.RuneDropCount = origCount;
                return;
            }
            _planCfg = cfg;
            _planOrigCount = origCount;
            // [2026-10-06 09:00] Tidak lagi mengubah cfg.RuneSets/cfg.Slots (dulu ApplyNarrowing).
            cfg.RuneDropCount = n;
        }

        /// <summary>Kembalikan RuneDropCount asli (tidak menyentuh rencana).</summary>
        public static void RestoreNarrowing()
        {
            if (_planCfg != null)
            {
                try { _planCfg.RuneDropCount = _planOrigCount; }
                catch { }
            }
            _planCfg = null;
            _planOrigCount = 0;
        }

        /// <summary>Bersihkan rencana + kursor.</summary>
        public static void ClearPlan()
        {
            _plan.Clear();
            _planCursor = 0;
            _hasPlan = false;
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
