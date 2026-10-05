# ConfigurableLevelCapFreebuff

Mod BepInEx untuk **Dimraeth** yang menghapus hardcap level 25 dan menggantinya dengan cap yang bisa dikonfigurasi (default: level 99).

## Instalasi

1. Pastikan **BepInEx 6 (IL2CPP)** sudah terpasang di folder game.
2. Copy `ConfigurableLevelCapFreebuff.dll` ke `BepInEx/plugins/`.
3. Config file akan di-generate otomatis saat game dijalankan pertama kali.

## Konfigurasi

Edit `BepInEx/config/com.freebuff.configurablelevelcap.cfg`:

```ini
[Progression]
## Maximum character level (Default: 99). Set to 25 for vanilla behavior.
MaxLevelCap = 99

## Maximum per-attribute level (Concentration, Health, dst).
MaxAttributeCap = 99

## EXP multiplier for all sources (monster kills, quests, items).
## 1.0 = vanilla rate.
ExpMultiplier = 1.0

[General]
## Patch native GameAssembly.dll byte code (best-effort, byte-verified).
EnableNativeBytePatch = true
```

## Cara Kerja

### Sistem Leveling di Dimraeth

1. **Bunuh monster** → dapatkan EXP.
2. **Buka halaman Train** (PlayerUpgradeUI) → spend EXP untuk naikkan attribute (Concentration, Health, Magic Damage, Physical Damage, dst).
3. Setiap attribute naik 1 level = cost XP tertentu (mengikuti tabel `PlayerStats.XPRequiredForLevelUp`).
4. Level player juga naik otomatis saat EXP cukup.

### Cara Mod Ini Menghapus Cap

**Primary mechanism — GameConfig static field patch:**
- Mod menulis ulang `GameConfig.MaxLevel` dan `GameConfig.MaxAttributeLevel` melalui IL2CPP interop API.
- Ini adalah primary source yang dibaca oleh engine saat level-up dan attribute upgrade.
- Version-resilient (tidak hardcode RVA).

**Secondary mechanism — Native byte patch (opsional):**
- Patch bytecode `cmp reg, 0x19 (25)` di `GameAssembly.dll` menjadi `cmp reg, <cap>`.
- Byte-verified: hanya patch jika byte masih original, jadi aman saat game update.
- 10 site tercover: XP.IsXPGainBlocked (3), Player.InitializeHighestLevel (3), PlayerStats.ApplyUpgradeAttributeInternal (2), PlayerStats.UpgradeAttributeServerRpc (2).

**Tertiary — Harmony patches:**
- `XP.IsXPGainBlocked`: Izinkan XP gain sampai MaxLevelCap.
- `PlayerStats.UpgradeAttributeServerRpc`: Guard attribute upgrade.
- `XP.CalculateXPGained`: Apply ExpMultiplier.
- `PlayerStats.Update`: Level sync — naikkan level otomatis saat XP cukup.
- `Player.Awake`: Setup saat world dimuat.

## Kostumerisasi

### Set Level Cap ke 99 (default)
```ini
MaxLevelCap = 99
MaxAttributeCap = 99
```

### Set Level Cap ke 50
```ini
MaxLevelCap = 50
MaxAttributeCap = 50
```

### Restore Vanilla (cap 25)
```ini
MaxLevelCap = 25
MaxAttributeCap = 99
```

### EXP Multiplier 10x (leveling cepat)
```ini
ExpMultiplier = 10.0
```

## Keamanan

- Mod ini untuk **single-player** saja.
- Tidak ada modifikasi file game di disk — semua patch dilakukan di memory.
- Anti-cheat tetap jalan untuk world multiplayer (tapi karena single-player, tidak masalah).

## Log Output

Cek `BepInEx/LogOutput.log` untuk verifikasi:

```
[ConfigurableLC] GameConfig.MaxLevel: 25 -> 99 (cap=99) [OK]
[ConfigurableLC] GameConfig.MaxAttributeLevel: 99 -> 99 (cap=99) [OK]
[ConfigurableLC] Native byte: patched 10/10 sites (cap=99)
[ConfigurableLC] Player.Awake: level=1, cap=99
```

## Notes

- Engine mendukung attribute level sampai 99. Set `MaxAttributeCap = 99` untuk max.
- Player level juga bisa di-set sampai 99 (meski tabel XP akan sangat besar di level tinggi).
- Jika game update dan byte patch tidak works, mod tetap bekerja via GameConfig primary patch.
