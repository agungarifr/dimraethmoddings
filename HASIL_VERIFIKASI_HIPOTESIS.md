# Verifikasi Hipotesis: Tipe Character, Pilihan Chat NPC, dan "Afinity"

**Tanggal:** 23 September 2026
**Metode:** Pemeriksaan sumber utama (primary source) — stub C# hasil decompile, refleksi interop il2cpp (Assembly-CSharp.dll), dan string literal dari `global-metadata.dat`. Isi badan metode native tidak dapat dipulihkan dari disk.

---

## Ringkasan Hipotesis Awal (yang diuji)

> Tipe character (Joker / Stoic / Heroic / Scholar) memengaruhi **pilihan reply chat terhadap NPC**, dan memilih reply yang "tepat" menambah **afinity**.

### Hasil per bagian:

| Bagian hipotesis | Status |
|---|---|
| Ada "tipe character" yang dipilih saat pembuatan karakter | ✅ **TERKONFIRMASI** (dengan nuansa) |
| Tipe tersebut memengaruhi pilihan reply saat chat NPC | ✅ **TERKONFIRMASI** (struktural) |
| Istilah "afinity" | ❌ **SALAH** — istilah sebenarnya **Relationship** |
| Ada pilihan reply yang "tepat" / kuis jawaban benar | ⚠️ **NUANSED** — mekanismenya berbeda |
| Nilai reward konkret tiap pilihan bisa diekstrak dari file game | ❌ **TIDAK DAPAT** dari file statis di disk |

---

## 1. "Tipe character" — TERKONFIRMASI

Ada enum `Persona { Joker, Stoic, Heroic, Scholar }` yang dipilih saat pembuatan karakter, dan tersimpan di `Player.Persona`. Selain itu ada `Player.ConversationArchetype` (sebuah `NetworkVariable`), sehingga tipe ini tersinkron antar pemain dalam sesi multiplayer.

Di sistem dialog, tipe ini disebut **`ConversationArchetypeType { Heroic, Stoic, Joker, Scholar }`** — empat varian yang sama, hanya urutannya beda.

## 2. Tipe memengaruhi pilihan reply — TERKONFIRMASI (struktural)

Kelas **`PlayerDialogueEntry`** memiliki 4 kolom opsi yang masing-masing terikat ke satu archetype:

- `HeroicOption`
- `StoicOption`
- `JokerOption`
- `ScholarOption`

Ada method `GetOption(archetype)` yang mengembalikan teks opsi sesuai tipe karakter. Ini membuktikan: **satu node percakapan menyediakan opsi-opsi berbeda untuk tipe berbeda** — persis sesuai dugaan Anda.

Alur pemilihan di `NPCDatabase` melibatkan `SelectBestDialogueEntry`, `WeightedRandom`, `RegisterChoiceServerRpc` (diproses di server), dan `UpdateClientRelationshipClientRpc` (update relasi dikirim kembali ke klien). Ada juga `TallyVotes` untuk voting multiplayer.

## 3. "Afinity" — SALAH istilah; sebenarnya RELATIONSHIP

**Tidak ada istilah/string "Affinity" di mana pun.** Istilah yang benar adalah **Relationship**:

- `NPCRelationship` / `RelationshipData` dengan komponen:
  - `Opinion` (opini)
  - `Friendship` (pertemanan)
  - `Romance` (romansa)
  - `ValueAlignment` (keselarasan nilai)
- `RelationshipStatus` bertingkat dari `Stranger` → … → `SoulMate`.

Jadi mekanisme "penambahan nilai" itu nyata, tetapi namanya **Relationship**, bukan "Affinity".

## 4. Pilihan "tepat" — NUANSED (bukan kuis jawaban benar)

Tidak ada sistem "tipe NPC pavorit / jawaban benar yang harus ditebak". Strukturnya adalah:

1. Setiap entri percakapan punya 4 opsi beraroma persona (`HeroicOption`, `StoicOption`, dst).
2. Tiap entri punya **`RelationshipBonus`** — angka yang menentukan seberapa besar relasi bertambah.
3. Memilih opsi menambah **archetype point** (`AddArchetypePoint`) dan mengubah **Relationship**.
4. Ada `EnsurePersonaCoverage` agar percakapan tidak selalu jatuh ke persona yang sama (variasi).
5. Di multiplayer ada **voting berbobot** (`TallyVotes`, `WeightedRandom`) untuk menentukan hasil bersama.

Jadi intinya: **bukan "memilih jawaban benar untuk naik afinitas", melainkan memilih opsi yang sesuai persona Anda, yang tiap entri punya `RelationshipBonus` sendiri.** "Benar" relatif terhadap arketipe Anda, bukan terhadap teka-teki.

## 5. Nilai reward konkret (angka CSV) — TIDAK DAPAT dari file statis

Upaya ekstraksi data dialog dari file game (CSV dengan kolom `HeroicOption`, `StoicOption`, `JokerOption`, `ScholarOption`, `RelationshipBonus`, `NPCResponseID`, `UniqueID`, dst) **gagal dari sumber statis**:

- `ScanMarkers` memindai SELURUH file `Dimraeth_Data` (termasuk bundle yang sudah didekompresi, `.resS`, `resources.assets`, `sharedassets*`, `globalgamemanagers`) — penanda kolom CSV **hanya ditemukan di `global-metadata.dat` sebagai literal kode**, tidak ada di data aset/bundle yang nyata.
- Dialog dimuat saat runtime (CSV → `NPCDatabase`), sehingga teks & angkanya **tidak tersimpan sebagai teks polos** di aset yang dapat diekstrak.
- Katalog Addressable (`catalog.bin`) hanya berisi 4 bundle lokal; tidak ada kunci addressable khusus dialog.
- File save (`LocalLow\Mudtek\Dimraeth`) hanya berisi karakter/dunia, bukan data dialog.
- Catatan: `resources.assets` memang **mendeklarasikan tipe TextAsset (classId 49)** di metadata-nya, tetapi pemindaian penanda tidak menemukan konten CSV dialog di sana — jadi bukan sumber data dialog.

**Satu-satunya cara** untuk melihat angka `RelationshipBonus` konkret per opsi adalah **menjalankan game** (mis. di bawah BepInEx/CheatMenu) dan mengamati perubahan `Opinion`/Relationship secara langsung — tidak bisa didapat dari berkas statis di disk.

---

## Kesimpulan Akhir

Hipotesis Anda **benar secara struktural** pada intinya: tipe character memang menghasilkan pilihan reply yang berbeda saat chat NPC, dan pilihan tersebut memengaruhi relasi. Namun:

1. Istilahnya **Relationship**, bukan "Afinity".
2. Tidak ada "jawaban benar" mutlak — yang ada adalah **opsi beraroma persona + `RelationshipBonus` per entri + akumulasi archetype point + voting berbobot**.
3. Nilai reward angka konkret **tidak bisa diekstrak dari file game statis**; hanya bisa diamati dengan menjalankan game.

**Bukti sumber utama:** enum `Persona`; `Player.Persona`; `Player.ConversationArchetype` (NetworkVariable); `PlayerDialogueEntry` (HeroicOption/StoicOption/JokerOption/ScholarOption + `RelationshipBonus` + `GetOption`); `NPCRelationship`/`RelationshipData` (Opinion/Friendship/Romance/ValueAlignment); `RelationshipStatus`; `NPCDatabase` (SelectBestDialogueEntry, WeightedRandom, RegisterChoiceServerRpc, UpdateClientRelationshipClientRpc, TallyVotes, EnsurePersonaCoverage); literal `global-metadata.dat`.
