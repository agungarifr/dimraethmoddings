# Relationship Multiplier (Dimraeth)

Mod **standalone** (BepInEx 6 / Il2Cpp) yang melipatgandakan perolehan **relationship gain** saat berbincang dengan NPC.

> Status: **project disiapkan, siap di-port ke dalam CumisModifier** — belum di-deploy ke folder game.

---

## Apa yang dilakukan mod ini

Game menyimpan perolehan relasi per komponen: **Opinion, Friendship, Romance, ValueAlignment** (struct `RelationshipData`). Nilai tiap komponen dihitung lewat method di kelas `NPCDialogues`:

| Method (dari interop `Assembly-CSharp.dll`) | Peran |
|---|---|
| `GainFor(RelationshipData, RelationshipStat) -> int` | gain **per komponen** relasi |
| `TotalRelationshipGain(RelationshipData) -> int` | gain **total** (mis. untuk UI/status) |

Mod ini memasang **Harmony postfix** pada dua method itu dan mengalikan hasilnya dengan pengali dari config.

```
__result = __result × multiplier
```

Hasilnya, seluruh jalur perhitungan relasi (yang melalui `GainFor`) otomatis terlipat — termasuk yang ditampilkan UI dan yang disimpan.

## Konfigurasi

File config dibuat otomatis di `BepInEx/config/dimraeth.relationshipmultiplier.cfg`:

- **`RelationshipGainMultiplier`** (default `2.0`) — pengali tiap komponen relasi.
  `1.0` = normal, `2.0` = 2x, `10.0` = 10x.
- **`TotalRelationshipGainMultiplier`** (default `1.0`) — pengali tambahan pada gain total.

## Struktur project

```
RelationshipMultiplier/
├─ RelationshipMultiplier.csproj      # target net6.0, ref ke BepInEx + interop game
├─ RelationshipMultiplierPlugin.cs    # entry point (BasePlugin), baca config, PatchAll
├─ Patches/
│  └─ RelationshipGainPatches.cs      # [HarmonyPatch] GainFor & TotalRelationshipGain
└─ README.md
```

## Build

```
& "..\dotnet-sdk\dotnet.exe" build -c Debug
```
(atau buka `.csproj` di Visual Studio 2022 / Rider).

Hasil: `bin\Debug\net6.0\RelationshipMultiplier.dll`

Referensi (`HintPath`) menunjuk ke `BepInEx\core` dan `BepInEx\interop` di folder game. Ubah `GameRoot` di `.csproj` bila folder game berbeda.

## Cara port ke dalam CumisModifier

Karena CumisModifier adalah plugin BepInEx juga, cukup **salin bagian patch** (yang penting saja):

1. Salin `Patches/RelationshipGainPatches.cs` ke project CumisModifier (sesuaikan namespace).
2. Pastikan project CumisModifier me-refer `Assembly-CSharp.dll` (interop) dan `0Harmony`.
3. Pada `Load()` CumisModifier, panggil:
   ```csharp
   var harmony = new Harmony("dimraeth.relationshipmultiplier");
   harmony.PatchAll();   // atau harmony.Patch(accessTools.Method(typeof(NPCDialogues), nameof(NPCDialogues.GainFor)), postfix: ...)
   ```
4. Sesuaikan sumber nilai pengali (bisa hardcode, ambil dari config CumisModifier, atau dari UI-nya).

`[BepInPlugin]` dan entry point hanya diperlukan jika dipakai sebagai plugin mandiri; saat di-port cukup ambil kelas patch-nya.

## ⚠️ Catatan multiplayer (penting)

Perhitungan relasi bersifat **otoritatif di host/server** (RPC `RegisterChoiceServerRpc` diproses di server, lalu `UpdateClientRelationshipClientRpc` dikirim ke klien).

- **Single-player / menjadi host** → mod bekerja penuh (semua gain terlipat & tersimpan).
- **Join server pemain lain** → mod di sisi klien saja **tidak cukup**; harus dipasang juga di sisi **host** agar nilai yang dihitung server ikut terlipat.

---

*Sumber tanda tangan method diverifikasi langsung dari `BepInEx\interop\Assembly-CSharp.dll` (lihat `modding/ModInspect`).*
