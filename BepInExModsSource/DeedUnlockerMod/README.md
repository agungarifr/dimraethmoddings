# DeedUnlockerMod

Mod BepInEx untuk **Dimraeth** yang membuka semua tier di papan quest (Deed Board) dan
menaikkan batas rarity loot dari quest ke atas Mythical.

## Masalah yang Diperbaiki

1. **Tier quest cuma sampai 3.** Vanilla membatasi Deed Board pada `BaseTierCap` (di-set ke 3 di data)
   dan baru naik lewat story milestone. Padahal data tiernya ada sampai 10.
2. **Item di atas Mythical tidak bisa didapat dari quest.** Loot equipment quest di-clamp ke
   `Runes.Rarity.Mythical` (dan batas bintang tertentu) lewat `DeedEquipmentLootConfig.MaxRarity`/`MaxStars`.

## Instalasi

1. Pastikan **BepInEx 6 (IL2CPP)** sudah terpasang di folder game.
2. Copy `DeedUnlockerMod.dll` ke `BepInEx/plugins/`.
3. Config auto-generate saat game pertama dijalankan.

## Konfigurasi

Edit `BepInEx/config/com.freebuff.deedunlocker.cfg`:

```ini
[DeedBoard]
## Highest Deed Board tier that is unlocked regardless of story milestones (1-10).
MaxTierCap = 10

[Loot]
## Highest equipment rarity deed quests may drop (Common, Uncommon, Rare, Mythical, Heroic, Ancient).
MaxLootRarity = Ancient

## Highest equipment star rating deed quests may drop (One..Nine).
MaxLootStars = Nine
```

## Cara Kerja

Semua patch adalah **Harmony postfix** (host-side), mengikuti pola mod lain di repo ini:

- `DeedManager.GetWorldTierCap()` → dipaksa `>= MaxTierCap`, jadi 10 tier terbuka.
- `DeedManager.IsTierUnlocked()` (private) → selalu `true`, server tidak menolak tier tinggi.
- `DeedManager.GetUnlockedTierForDeed()` → slider UI/accept menampilkan semua tier milik deed.
- `DeedDefinition.GetConfigForTier()` → `MaxRarity`/`MaxStars` dinaikkan ke nilai config,
  sehingga loot roll & UI menampilkan rarity/bintang lebih tinggi.

## Catatan

- Dirancang untuk **single-player & self-hosted multiplayer** — karena tier cap itu host-authoritative
  dan disinkronkan ke client, patch di host berlaku untuk semua pemain di world.
- Tidak mengubah file game di disk; semua patch di memory.
- `MaxTierCap` hanya membuka tier yang memang sudah di-author di data (tiap deed punya `MaxTiers` sendiri).

## Verifikasi Log

Cek `BepInEx/LogOutput.log`:

```
[Info  :DeedUnlockerMod] DeedUnlockerMod v1.0.0 Initialized!
[Info  :DeedUnlockerMod] Deed Tier Cap: 10 (all tiers unlocked)
[Info  :DeedUnlockerMod] Deed Loot Max Rarity: Ancient
[Info  :DeedUnlockerMod] Deed Loot Max Stars: Nine
```
