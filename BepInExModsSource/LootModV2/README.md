# LootModV2 — Loot 10x (Monster + Node Panen + Interactable)

Versi **baru** dari `LootAndExpMod`, khusus **loot saja** — **tidak ada EXP modifier sama sekali**.

## Yang diubah vs LootAndExpMod lama

| Aspek | LootAndExpMod lama | LootModV2 |
|---|---|---|
| Drop item monster | ✅ 10x | ✅ 10x |
| Drop gold monster | ✅ 10x | ✅ 10x |
| Drop chance monster 100% | ✅ | ✅ |
| **EXP (combat/quest/pet)** | ❌ ada (30x) | ✅ **DIHAPUS** |
| **Node panen** (stone/pickaxe, wood/log/kapak, rumput/bush/plant/glove) | ❌ tidak ada | ✅ **BARU 10x** |
| **Interactable di map** (dead body / box / tekan C) | ❌ tidak ada | ✅ **BARU 10x** |

## Cara kerja (target patch, diverifikasi dari interop `Assembly-CSharp.dll`)

### Monster (dipertahankan)
- `MonsterUtils.ApplyDropChanceModifiers` → postfix paksa `__result = 100` (drop chance 100%).
- `MonsterUtils.ItemDropCheck` → prefix perbesar rentang `ItemDrops` (`dropRange.min/max`) × `ItemMultiplier`.
- `Formulas.CalculateGoldDrop` → postfix `__result *= GoldMultiplier`.

### Node panen (BARU) — stone, wood/log, rumput, bush, plant, dll.
Semua tipe node diwakili `HarvestType` (SmallRock, Coal, Iron, Copper, LogA/B/C, PinewoodBranch, Potato, Carrot, Tenderberry, WoolandFlax, Apple, LooseStone, …) dan dihitung oleh metode di **`HarvestClient`**:
- `HarvestClient.CalculateItemsForDamage(HarvestOption, int damage) -> List<HarvestedItem>` → postfix kalikan `ItemCount` tiap item × `HarvestMultiplier`.
- `HarvestClient.RollBonusItems(HarvestOption) -> List<HarvestedItem>` → postfix kalikan item bonus (chance) × `HarvestMultiplier`.

Karena lewat titik yang sama, ini **otomatis mencakup semua node** yang butuh tool apa pun (pickaxe ⛏, kapak 🪓, glove 🧤).

### Interactable di map (BARU) — dead body / box / tekan C
`InteractableReward.ApplyReward(InteractableRewardEntry entry, Player player)` → prefix: jika `entry.Type == GrantItem`, kalikan `entry.GrantedItemAmount` × `InteractableMultiplier`.

### Equipment rarity tertinggi (BARU)
`Runes.ReturnRandomRuneData(..., List<Rarity> rarities, ...)` → prefix: ganti daftar `rarities` menjadi `[rarity tertinggi yang vanilla izinkan]` (max dari list, tanpa meng-inject Ancient bila vanilla tidak mengizinkannya). Karena semua sumber equipment (monster, chest, deed, craft) lewat metode ini, semuanya terkena. Pakai `ref` + list baru → list milik pemanggil tidak ikut termutasi.

> **Integrasi ke CumisModifier:** patch ini (kelas `Patch_MaxEquipmentRarity` + config `ForceHighestRarity`) sudah mandiri dan siap dipindah ke project CumisModifier — cukup salin kelasnya dan daftarkan via `Harmony.CreateAndPatchAll`.

## Konfigurasi (`BepInEx/config/com.custom.lootmodv2.cfg`)

| Key | Default | Fungsi |
|---|---|---|
| `ItemMultiplier` | 10 | Drop item monster ×N |
| `GoldMultiplier` | 10 | Drop gold monster ×N |
| `HarvestMultiplier` | 10 | Drop node panen (stone/wood/rumput) ×N |
| `InteractableMultiplier` | 10 | Reward interactable (dead body/box) ×N |
| `GuaranteeMonsterDrops` | true | Drop chance monster 100% |
| `ForceHighestRarity` | true | Equipment (monster/chest/craft) selalu drop rarity **tertinggi yang diizinkan vanilla** (max dari list `Rarities`), bukan dipaksa Ancient |

## Build & install

```
& "..\..\dotnet-sdk\dotnet.exe" build -c Debug
```
Hasil: `bin\Debug\LootModV2.dll`

Deploy: salin `LootModV2.dll` (+ `.deps.json` bila ada pengambilan dependensi NuGet — project ini murni referensi lokal, jadi cukup DLL-nya) ke `BepInEx\plugins\`. Nonaktifkan `LootAndExpMod` lama agar tidak dobel.

> ⚠️ Karena project ini memperluas data/node server-side di multiplayer, perhitungan tetap bergantung host. Untuk barter/MP: gunakan di host agar semua pemain merasakan drop 10x.

*Struktur project*
```
LootModV2/
├─ LootModV2.csproj
├─ LootModV2.cs            # entry point + konfigurasi
├─ Patches/
│  └─ LootPatches.cs       # semua Harmony patch
└─ README.md
```