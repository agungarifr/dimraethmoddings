# Chrono Spear (Chrono Spike) — Laporan Decompile Lengkap: Mekanisme Projectile "Homing"

> **Tanggal:** 2026-10-04
> **Sumber bukti (ground truth):** dump ISIL hasil decompile IL2CPP di
> `modding/cpp2il_isil_out/IsilDump/Assembly-CSharp/` (salinan sama di `modding/DecompilerTool/isil_out/`) +
> layout field dari `modding/cpp2il_cs_out/DiffableCs/Assembly-CSharp/*.cs`.
> File `.cs` hasil decompile **hanya stub** (`throw null`), jadi semua logika di bawah dibaca dari
> disassembly/ISIL — baris ISIL dikutip agar bisa dicek ulang.
> Catatan dump ISIL: offset memori di instruksi `Move [rbx+N]` bernilai **desimal** (contoh: `[rbx+240]` = `0xF0`).
>
> Dokumen ini melengkapi `docs/ChronoSpike_Spell_HowItWorks.md` (analisis perilaku spell secara umum) dan
> **berfokus pada pertanyaan spesifik: "bagaimana projectile-nya bisa homing / auto bergerak ke musuh terdekat?"**
> Semua klaim di dokumen ini diverifikasi ulang langsung dari ISIL, bukan copy dari dokumen lama.

---

## 0. Ringkasan eksekutif — JAWABAN SINGKAT

**Chrono Spear TIDAK melakukan homing (pengejaran) saat terbang.** Yang terjadi:

1. **"Auto target" terjadi SEKALI saat cast (OnStart).** Spell mencari musuh dalam radius **10 unit**
   (`GetAllEnemiesInRange`) lalu memilih target berdasarkan prioritas (default: jumlah stack DoT terbanyak;
   tie → terdekat. Upgrade **Crippling Spike** mengubah prioritas jadi **terdekat** murni).
2. Posisi target saat itu **di-snapshot** ke field `BaseSpellLibrary._targetPosition` (offset `0xF0`).
   Setelah itu **tidak ada satu pun kode Chrono yang menulis ulang 0xF0 selama penerbangan.**
3. Projectile terbang di sepanjang **kurva parametrik statis**: garis lurus dari titik spawn ke snapshot
   target + lengkungan vertikal `sin(π·progress)·_arcHeight` + lengkungan lateral
   `sin(π·progress)·_lateralArc` (busur "bow" antar shard). Titik akhir kurva dihitung ulang tiap frame
   DARI FIELD YANG SAMA — bukan dari posisi musuh hidup.
4. Kesan "homing / auto ke musuh terdekat" berasal dari kombinasi:
   - **seleksi target otomatis saat cast** (shard benar-benar "memilih" musuh terdekat/prioritas),
   - **kurva yang tampak seperti rudal pemandu** (lengkung lateral + arc),
   - **umpan tembak mid-flight** (`SweepStepForHits`) yang menangkap musuh yang sedang bergerak,
   - waktu terbang sangat singkat (~0.8 s untuk 13 unit @ 16 unit/s) sehingga snapshot jarang meleset.

> **Implikasi untuk mod:** jika mod Anda melakukan *turn-rate homing* (heading berputar mengejar musuh tiap
> frame), itu **perilaku yang BERBEDA dari Chrono Spear vanilla** — perilaku itu justru milik
> `AetherHowlProjectilePrefab` (lihat §8). Untuk meniru Chrono, targetkan **sekali saat spawn** lalu
> terbang di kurva statis. Untuk homing sungguhan, ikuti pola AetherHowl (turn-rate + look-ahead).
> Resep kedua jalur ada di §10.

---

## 1. Hierarki kelas & file sumber

```
BaseSpellLibrary            (NetworkBehaviour — punya _targetPosition 0xF0, _owner 0xA0, _target 0xB0)
  └── BaseSpell             (Update/retarget pipeline, VFX rotate helpers)
        └── SkillShot       (mesin gerakan projectile: Update, MoveProjectile*, Sweep, arrival)
              └── ChronoSpikePrefab   (spell Chrono Spear — root cast DAN split children)
ChronoSpikeVolley           (penghitung hit per-target untuk satu volley)
```

File kunci:

| File ISIL | Isi |
|---|---|
| `SkillShot.txt` | seluruh mesin gerakan (Update 531, MoveProjectileAlongCurve 2186, MoveProjectileTowardsTarget 2443, OnRetargetedTargetUpdated 1352, dst.) |
| `ChronoSpikePrefab.txt` | OnStart, seleksi target, volley, dampak (ResolveImpact) |
| `BaseSpell.txt` / `BaseSpellLibrary.txt` | handler retarget owner, inisialisasi spawn, helper |
| `AetherHowlProjectilePrefab.txt` | **template homing sungguhan** di game ini (UpdateHomingTarget 77) |
| `AlphaRepositionProjectilePrefab.txt` | homing kedua (`_homingStrength = 2.0`) |

---

## 2. Layout field yang relevan (offset hex, terverifikasi dari cpp2il)

### BaseSpellLibrary (target & owner)

| Offset | Field | Tipe | Arti |
|---|---|---|---|
| `0xA0` | `_owner` | ObjectsCommon | pemilik spell |
| `0xB0` | `_target` | ObjectsCommon | **referensi objek target** (dipakai homing AetherHowl) |
| **`0xF0`** | **`_targetPosition`** | Vector2 | **TITIK TUJUAN projectile — inilah "target" yang dibaca mesin gerakan** |
| `0xF8` | `_randomPositions` | List\<Vector2\> | — |

### SkillShot (mesin gerakan)

| Offset | Field | Tipe | Arti |
|---|---|---|---|
| `0x290` | `_originalDirectionToTarget` | Vector2 | arah asli → target (dihitung saat re-aim) |
| `0x298` | `_updatedTargetPosition` | Vector2 | posisi target ter-update (helper re-aim) |
| `0x2A0` | `_originalStartingPosition` | Vector2 | titik awal penerbangan (basis kurva & jarak max) |
| `0x2A8` | `_maximumTravelDistance` | float | jarak tempuh maksimum (Chrono = 13) |
| `0x2B0` | `_rotateFrom` | FollowCasterType | mode rotasi (Chrono = 1) |
| `0x2BA` | `_useCurvedTrajectory` | bool | **Chrono = true** → jalur kurva |
| `0x2BB` | `_launchProjectile` | bool | sudah diluncurkan? |
| `0x2BC` | `_projectileStopped` | bool | berhenti (env blocker) |
| `0x2BD` | `_extrapolateMaxDistance` | bool | flag ekstrapolasi titik tujuan saat re-aim |
| `0x2BE` | `_targetPositionReached` | bool | sudah sampai titik tujuan |
| `0x2BF` | `_maxDistanceReached` | bool | sudah capai jarak max |
| `0x2C4` | `_projectileSpeed` | float | kecepatan (Chrono = 16) |
| `0x2C8` | `_arcHeight` | float | tinggi lengkungan vertikal |
| `0x2CC`/`0x2D0` | `_minArcHeight`/`_maxArcHeight` | float | rentang arc (di-set via `SetArcHeightRange`) |
| `0x2D4` | `_lateralArc` | float | besar lengkungan lateral (Chrono = `_volleyLateralBow`) |
| `0x2D8` | `throwTime` | float | timer parametrik kurva (bertambah deltaTime saat terbang) |

### ChronoSpikePrefab (khusus spell ini)

| Offset | Field | Arti |
|---|---|---|
| `0x2F8` | `_projectileVfx` | VFX projectile (diaktifkan saat launch) |
| `0x300` | `_impactVfx` | VFX impact |
| `0x320` | `_splitProjectilePrefab` | prefab clone untuk 2 shard anak |
| `0x341` | `_splittingSpike` | unlock volley 3 shard |
| `0x342` | `_cripplingSpike` | ganti prioritas target → terdekat |
| `0x350` | `_selectedTarget` | target terpilih shard ini |
| `0x358` | `_resolvedTargets` | HashSet dedup impact |
| `0x368` | `_isSplitProjectile` | true untuk shard anak |
| `0x370` | `_overrideTarget` | target yang dipaksakan oleh parent volley |
| `0x378`/`0x380` | `_groundTargetOverride`/`_hasGroundTargetOverride` | titik tanah untuk slot tanpa musuh |
| `0x388` | `_volley` | objek hit-counter per volley |
| `0x390` | `_volleySlot` | slot 0/1/2 (juga delay launch) |
| `0x394` | `_volleyLateralBow` | busur lateral shard ini |

Konstanta Chrono (`const`, dari header cpp2il — terverifikasi):
`_seekRange=10`, `_stepDistance=3`, `_stepTime=0.2`, `_windUpTime=0.2`, `_projectileSpeed=16`,
`_splitProjectileCount=3`, `_lateralBowStep=2`, `_volleyLaunchStagger=0.06`, `_repeatHitDamageScale=0.5`,
`_groundFanAngle=16`, `_groundFanJitter=4`, `_baseStacksConsumable=3`, `_baseStackBonusPct=0.2`.

---

## 3. Timeline cast → terbang (ringkas)

```
OnStart()                                            [ChronoSpikePrefab.txt ~700+]
 ├─ ReadSkillTreeUpgrades()
 ├─ _useCurvedTrajectory(0x2BA)=1, _projectileSpeed(0x2C4)=16, _maximumTravelDistance(0x2A8)=13,
 │  _rotateFrom(0x2B0)=1
 ├─ root cast: _dashDestination = ComputeDashDestination()
 ├─ root cast: _splittingSpike ? AssignSplittingVolley() : _selectedTarget = SelectPriorityTarget()
 ├─ split child: _selectedTarget = _overrideTarget  /  _targetPosition = _groundTargetOverride
 ├─ JIKA _selectedTarget != null:
 │     _targetPosition(0xF0) = _selectedTarget.transform.position     ← SATU-SATUNYA snapshot  [ISIL 206-207]
 ├─ _lateralArc(0x2D4) = _volleyLateralBow(0x394)                       [ISIL 208-209]
 └─ coroutine WindUpAndLaunch: tunggu 0.2 + slot*0.06 dtk → LaunchProjectile()
                                (+ dash caster 3 unit selama 0.2 dtk untuk root cast)

SkillShot.Update()  setiap frame                    [SkillShot.txt 531-1035]
 ├─ BaseSpell.Update()
 ├─ kunci Z transform ke DesiredZ()
 ├─ jika BELUM launch (0x2BB=0): re-aim dari input owner (UpdateAimFromOwnerInput / prefab facing)
 │                               + FollowOwnerCheck + RotateTowardsTarget   ← hanya fase wind-up
 └─ jika SUDAH launch (0x2BB=1):
      prevPos = transform.position
      if (_useCurvedTrajectory) throwTime(0x2D8) += deltaTime; MoveProjectileAlongCurve()
      else                     MoveProjectileTowardsTarget()
      SweepStepForHits(prevPos)                       ← umpan tembak mid-flight
      if (jarak(_targetPosition, pos) <= ε)  → _targetPositionReached=1, OnDestinationReached()
      if (jarak(pos, _originalStartingPosition) >= _maximumTravelDistance)
                                              → _maxDistanceReached=1, OnMaxDistanceReached()
      jika _destroyOnEnvironmentBlock dan kena blocker → berhenti + OnDestinationReached()

OnDestinationReached() → ResolveImpact(_selectedTarget, "destination reached")
```

---

## 4. Seleksi target — bagian "auto ke musuh terdekat"

Pool musuh: `BaseSpellLibrary.GetAllEnemiesInRange(posisi, _seekRange = 10)` (radius 10 unit).

### 4.1 `SelectPriorityTarget(report)` (tanpa Splitting Spike)
- `dotPriority = !_cripplingSpike`
- Jika `dotPriority`: pindai semua musuh → pilih **stack DoT terbanyak** (`CountDotStacks` = jumlah stack
  burn+poison+bleed dari `_dotEffects[3]`); **tie → terdekat dari caster**.
- Jika `_cripplingSpike`: `GetNearest(enemies)` → **musuh terdekat** dari posisi owner (jarak Euclidean).
- Kosong → `null` (shard terbang ke titik tanah hasil kipas `GroundSpotForSlot`).

### 4.2 `SelectTopPriorityTargets(count=3, report)` (dengan Splitting Spike)
- Sort yang sama (DoT terbanyak DESC, atau jarak ASC jika Crippling), ambil 3 teratas.
- `ComputeLateralBows`: slot dibagi round-robin ke target (`slot % targets.Count`); dalam grup berukuran k,
  `bow[j] = (j - (k-1)/2) * _lateralBowStep` → k=1:`[0]`, k=2:`[-1,+1]`, k=3:`[-2,0,+2]`.
- `SpawnSplitProjectiles`: clone `_splitProjectilePrefab` untuk slot 1-2, isi `_overrideTarget`
  (atau `_groundTargetOverride`), `_volleySlot`, `_volleyLateralBow`, share `_volley`.
  Slot 0 = cast root itu sendiri. Launch ter-stagger 0.20/0.26/0.32 dtk.

> **Inilah satu-satunya "auto"**: pemilihan musuh terdekat/prioritas dilakukan **di saat cast**.
> Tidak ada loop pengejaran setelahnya.

---

## 5. Matematika gerakan (terverifikasi per-instruksi)

### 5.1 `MoveProjectileTowardsTarget()` — lurus ke titik `0xF0`  [ISIL 2570-2695]

```csharp
// pseudocode rekonstruksi 1:1 dari ISIL
Vector2 pos    = transform.position;
Vector2 delta  = _targetPosition - pos;                 // [rbx+240]/[rbx+244] dibaca SETIAP FRAME
float   dist   = delta.magnitude;

if (dist <= epsilonKecil) {                             // snap → tiba
    transform.position = _targetPosition (z=DesiredZ());
    _launchProjectile = 0; _targetPositionReached = 1; OnDestinationReached();
    return;
}

float step = _projectileSpeed * Time.deltaTime;         // [rbx+708] * dt
if (step >= dist) { /* snap → tiba (sama seperti di atas) */ }
else transform.position = pos + delta / dist * step (z=DesiredZ());
```

Catatan penting: method ini **membaca `0xF0` segar tiap frame**. Siapa pun yang menulis `0xF0`
(mesin game ATAU mod) langsung mengarahkan projectile — inilah "tuas setir" yang dipakai mod homing.

### 5.2 `MoveProjectileAlongCurve()` — kurva statis  [ISIL 2313-2441] — yang dipakai Chrono

```csharp
// pseudocode rekonstruksi 1:1 dari ISIL
Vector2 dir       = (_targetPosition - _originalStartingPosition).normalized;
float   totalDist = Vector2.Distance(_targetPosition, _originalStartingPosition);
float   dist      = Mathf.Min(totalDist, _maximumTravelDistance);   // clamp 13
Vector2 endPoint  = _originalStartingPosition + dir * dist;

float travelTime  = dist / _projectileSpeed;            // 16 u/s
float progress    = throwTime / travelTime;             // throwTime += dt tiap frame
progress          = Mathf.Clamp01(progress);

Vector2 linePos   = Vector2.Lerp(_originalStartingPosition, endPoint, progress);
float   bell      = Mathf.Sin(progress * Mathf.PI);     // 0 → 1 → 0 (konstanta 0x18465DBA0 = π)

Vector2 pos       = linePos;
pos.y            += bell * _arcHeight;                  // lengkungan vertikal
if (_lateralArc != 0f)                                  // busur lateral (bow)
    pos += new Vector2(-dir.y, dir.x) * (bell * _lateralArc);   // tegak lurus arah terbang

transform.position = pos (z=DesiredZ());
```

Implikasi:
- Kurva sepenuhnya **f(parametrik waktu)** terhadap dua titik: `_originalStartingPosition` dan `_targetPosition`.
- **Jika `0xF0` ditulis ulang saat terbang**, arah `dir` dan titik akhir ikut berubah → projectile
  "melengkung" halus ke titik baru (mekanisme re-aim yang dipakai game untuk hold/channel — §6).
- `throwTime` mentok di `progress=1` → projectile berhenti tepat di titik akhir kurva.
- `bell` membuat laju sepanjang arc sedikit > `_projectileSpeed` (chord = kecepatan konstan).

### 5.3 Deteksi tumbukan mid-flight
`SweepStepForHits(prevPos)` (ISIL 2697+) menyapu footprint antara posisi frame sebelumnya dan sekarang
(Physics2D overlap) → `ApplyTargetHit(target)` → `ResolveImpact(target, "mid-flight collision")`.
Jadi musuh yang bergerak tetap kena **sapuan**, bukan karena projectile mengejar.

---

## 6. SEMUA situs penulisan `_targetPosition` (0xF0) — kapan titik tujuan boleh berubah

Diverifikasi dengan grep seluruh dump ISIL (`Move [r+n], ...` ke offset 240 desimal):

| # | Lokasi | Kapan | Menulis apa |
|---|---|---|---|
| 1 | `BaseSpellLibrary.Initialize` (BaseSpellLibrary.txt 2572) | spawn spell | titik dari `SpellCreationData` |
| 2 | `BaseSpellLibrary.SpawnPrefab` (19843, 27889) | spawn prefab | parameter `targetPosition` |
| 3 | **`ChronoSpikePrefab.OnStart`** (ChronoSpikePrefab.txt 781) | cast | `_groundTargetOverride` (slot tanpa musuh) |
| 4 | **`ChronoSpikePrefab.OnStart`** (827) | cast | **snapshot `_selectedTarget.transform.position`** |
| 5 | `ChronoSpikePrefab.AssignSplittingVolley` (3484) | cast | `GroundSpotForSlot(0)` saat tak ada musuh |
| 6 | `SkillShot.Start` (SkillShot.txt 518) | inisialisasi | salinan titik awal |
| 7 | `SkillShot.UpdateAimFromOwnerInput` (1849) | **pra-launch** (wind-up) | titik dari input owner (stick/mouse) |
| 8 | `SkillShot.OnRetargetedTargetUpdated` (1545) | sinkronisasi aim owner (network) | `start + arah·(maxDist+1)` **hanya jika `_extrapolateMaxDistance`** |
| 9 | `SkillShot.ExtrapolateMaxDistanceCheck` (3334) | saat re-aim | ekstrapolasi titik jauh |
| 10 | `SkillShot.RecomputeTargetAlongFacing2D` (6911) | dipanggil spell tertentu | titik baru di sepanjang facing |
| 11 | `BaseSpell.TrackDashTarget` (5954) | dash | titik tracking dash |
| 12 | `BaseSpell.HandleRetargetedCastComplete / HandleRetargetedActivateRelease / HandleChannelAimSync` (1172/1217/1241) | RPC retarget (hold/channel) | `finalTargetPosition` dari **aim owner** |
| 13 | `AetherHowlProjectilePrefab.UpdateHomingTarget` (655/707) | **setiap frame** | `pos + heading·lookAhead` ← **homing sungguhan** |
| 14 | `AlphaRepositionProjectilePrefab` (939/991) | setiap frame | pola homing sama |

> **Untuk Chrono Spear: hanya #3/#4/#5 yang terjadi — semuanya saat cast.**
> Jalur #7–#12 adalah sistem **re-aim oleh OWNER** (tangan pemain/aim monster), bukan pengejaran musuh.
> Hanya #13/#14 yang benar-benar mengejar musuh per frame — dan itu spell lain.

---

## 7. `OnRetargetedTargetUpdated` — "retarget" yang sering keliru dianggap homing

[SkillShot.txt 1352-1551] Dipanggil dari pipeline retarget owner (`HandleRetargetedCastComplete`,
`HandleRetargetedActivateRelease`, `HandleChannelAimSync/TargetSync` di `BaseSpell`) — yaitu saat
**tangan pemain menggeser aim** pada spell tipe hold/channel, disinkronkan lewat network.

```csharp
if (_owner == null) return;
origin = (_rotateFrom == 2) ? owner.transform.position : Utility.GetMiddleOfSprite(owner.transform);
delta  = finalTargetPosition - origin;
if (delta.sqrMagnitude >= 0.25f)                       // konstanta 0x18465DB88 = CollapsedAimSqrDistance
    _originalDirectionToTarget = delta.normalized;
FollowOwnerCheck(); UpdateSpellPositionWithOriginalDirection();
RotateTowardsTarget(false, _rotateFrom);
if (_extrapolateMaxDistance && _maximumTravelDistance != 0f)
    _targetPosition = _originalStartingPosition + _originalDirectionToTarget * (_maximumTravelDistance + 1f);
```

Ini **bukan** tracking musuh: sumbernya posisi aim owner. Untuk projectile Chrono yang sudah terbang,
tidak ada yang memanggil ini.

---

## 8. Homing sungguhan di game ini: `AetherHowlProjectilePrefab` (template untuk mod)

Field khusus: `_homingStrength` (0x300, default **2.0 rad/s ≈ 114.6°/s**), `_homingAngleLimit` (0x304,
default **90°**), `_maxTravelDistanceValue` (0x2FC), `_initialDirection` (0x320), `_initialDirectionSet` (0x328).

```csharp
// rekonstruksi UpdateHomingTarget() [AetherHowlProjectilePrefab.txt 77-711]
void Update() { UpdateHomingTarget(); base.SkillShot.Update(); }   // setir DULU, baru mesin gerakan

void UpdateHomingTarget() {
    ObjectsCommon target = ResolveHomingTarget();   // _target (0xB0), null jika TargetIsHiddenFromTracking
    if (!_initialDirectionSet) CacheInitialDirection();  // _initialDirection = arah ke 0xF0 saat spawn

    Vector2 pos        = transform.position;
    Vector2 dirToTarget = ((Vector2)target.transform.position - pos).normalized;
    // sudut antara arah terbang dan arah ke target:
    float angleDeg     = Mathf.Acos(Clamp(dot(...), -1, 1)) * Mathf.Rad2Deg;   // 0x18465DD44 = Rad2Deg
    // batasi arah "yang dituju" agar tidak > _homingAngleLimit dari arah awal:
    Vector2 desired    = (angleDeg > _homingAngleLimit)
                         ? RotateTowards(_initialDirection, dirToTarget, _homingAngleLimit * Mathf.Deg2Rad)
                         : dirToTarget;
    // putar heading bertahap: maksimum _homingStrength * deltaTime radian per frame:
    Vector2 heading    = RotateTowards(currentHeading, desired, Mathf.Max(0, _homingStrength) * Time.deltaTime);

    // tulis titik tujuan JAUH DI DEPAN sepanjang heading (look-ahead = _maxTravelDistanceValue):
    _targetPosition = pos + heading * _maxTravelDistanceValue;        // [rbx+240] tiap frame
    BaseSpell.RotateAndPosition(gameObject, pos, pos + heading, ...); // visual ikut membelok
}

Vector2 RotateTowards(Vector2 from, Vector2 to, float maxRadians)
    => ((Vector3)Vector3.RotateTowards(from, to, maxRadians, 0f)).normalized;   // bungkus Unity API
```

Kunci desainnya:
1. **Target = objek `_target` (0xB0)** yang ditetapkan saat cast — bukan query "terdekat" per frame.
   "Musuh terdekat" dipilih **sekali** oleh OnStart spell.
2. Steering = **rotasi heading dengan turn-rate** (`_homingStrength` rad/detik), bukan belok instan.
3. Yang ditulis ke `0xF0` **bukan posisi musuh**, melainkan **titik look-ahead sepanjang heading**
   (`pos + heading · jarak`) — supaya `MoveProjectileTowardsTarget` "menggigit" arah heading tanpa
   memicu arrival lebih awal.
4. Batas sudut 90° dari arah awal mencegah putaran balik 180° yang tidak wajar.

`AlphaRepositionProjectilePrefab` memakai pola identik (`_homingStrength = 2.0`).

---

## 9. Konstanta literal pool (inferred dari pemakaian)

| Alamat | Nilai (inferred) | Dipakai untuk |
|---|---|---|
| `0x18465DB98` | `1.0f` | clamp01 progress |
| `0x18465DBA0` | `π` | `sin(progress·π)` |
| `0x18465DBE8` | `-1.0f` | clamp cos |
| `0x18465DB88` | `0.25f` | `CollapsedAimSqrDistance` (cocok dengan const `BaseSpell`) |
| `0x18465DB84` | `0.017453292f` (Deg2Rad) | batas sudut homing |
| `0x18465DD44` | `57.29578f` (Rad2Deg) | sudut → derajat |
| `0x18465DBF8` | ε arrival (kecil, ~0.05-0.1) | `jarak(pos, 0xF0) <= ε` → OnDestinationReached |
| `0x18465E1C8` | deadzone stick | `ComputeDashDestination` |

---

## 10. Resep replikasi untuk mod (dua jalur)

### Jalur A — "persis seperti Chrono Spear" (bukan homing, tapi terlihat auto)
1. Saat spawn: cari musuh via `BaseSpellLibrary.GetAllEnemiesInRange(posisi, 10)` → pilih terdekat
   (atau prioritas DoT). Snapshot posisinya ke `0xF0` **sekali**.
2. Set `_originalStartingPosition (0x2A0)` = posisi spawn, `_useCurvedTrajectory (0x2BA)=1`,
   `_projectileSpeed (0x2C4)=16`, `_maximumTravelDistance (0x2A8)=13`, `_lateralArc (0x2D4)` = busur
   kecil (±2 bila beberapa shard berbagi target; `bow = (j-(k-1)/2)*2`).
3. Biarkan `SkillShot.Update` terbang sendiri. Hasil: shard melengkung "seperti dipandu" ke arah musuh.
4. Jangan tulis `0xF0` lagi saat terbang — itu yang membuat perilaku menyimpang dari vanilla.

### Jalur B — homing sungguhan (pola AetherHowl, untuk mengejar musuh bergerak)
1. Simpan `heading` (arah awal = arah ke target saat spawn).
2. Tiap frame **sebelum** `SkillShot.Update` (Prefix pada Update, atau subclass):
   ```
   toTarget = (targetPos - pos).normalized
   desired  = clamp sudut ke ±90° dari arah awal          (opsional, bikin natural)
   heading  = RotateTowards(heading, desired, 2.0f * dt)  // ~115°/detik
   0xF0     = pos + heading * lookAhead                   // lookAhead = 0x2A8 (mis. 13)
   ```
3. Pilih turn-rate yang pas: **115-180°/s** terasa "memburu"; >270°/s terasa snap/instan;
   speed projectile memengaruhi radius belokan (makin cepat, makin lebar busurnya).

### Pitfall TERVERIFIKASI dari percobaan sebelumnya (PlagueShardsHoming)
Pitfall ini membuat mod homing terlihat "gagal" padahal logikanya benar:

1. **VFX streak bukan transform projectile.** Visual shard adalah instance partikel yang **di-detach**
   (`_projectileVfxInstance` 0x338, di-spawn unparented via `SpawnAndTrack`, tidak pernah digerakkan
   game). Membelokkan hitbox saja → hitbox membelok tak terlihat, **streak tetap lurus**. Solusi:
   putar **kecepatan partikel** (`_projectileParticles` 0x340 → GetParticles → rotate `m_Velocity` →
   SetParticles) sebesar delta belok per frame (simpan buffer array, read-modify-write).
2. **Jangan tulis posisi musuh langsung ke `0xF0`** — begitu `jarak(pos, 0xF0) <= ε`, arrival terpicu
   lebih awal (dan kurva terpotong). Tulis titik **look-ahead** `pos + heading·maxTravel` seperti AetherHowl.
3. **Gate "sedang terbang"** memakai `_launchProjectile (0x2BB)`, BUKAN `_flightElapsed (0x370)`
   (field itu hanya maju jika profil kecepatan VFX ada; bisa tetap 0 → homing tak pernah aktif).
4. **Kecepatan:** `MoveProjectileTowardsTarget` membaca `0x2C4`, tapi `PlagueShardPrefab.DriveSpeedProfile`
   menulis ulang `0x2C4` tiap frame dari `_shardSpeed (0x2F8)`. Ubah sumber (`0x2F8`), bukan tujuan.
5. Untuk spell bertrajektori lurus, cek dulu `_useCurvedTrajectory (0x2BA)`: jika `true`, yang jalan
   `MoveProjectileAlongCurve` (basis kurva `_originalStartingPosition`) — arahkan dengan cara lain
   (perbarui kedua titik, atau paksa `0x2BA=0`).
6. `SweepStepForHits` memproyeksikan kandidat dari `_launchPosition` sepanjang `_flightDirection`
   (khas PlagueShard) — saat membelok, perbarui keduanya agar hitbox sapuan mengikuti jalur nyata.

### Checklist "meniru Chrono Spear" vs "homing"
| Aspek | Chrono Spear vanilla | Homing (AetherHowl) | Mod PlagueShardsHoming saat ini |
|---|---|---|---|
| Pemilihan target | sekali saat cast (DoT/terdekat, radius 10) | sekali saat cast (`_target`) | query **terdekat tiap frame** (radius 30) |
| Titik tujuan `0xF0` | snapshot sekali | ditulis tiap frame = pos+heading·lookAhead | ditulis tiap frame = pos+heading·lookAhead |
| Belok | tidak (kurva statis sin) | turn-rate 2.0 rad/s, batas 90° | turn-rate konfig (default 180°/s) |
| Visual | VFX anak menempel | `RotateAndPosition` | partikel diputar manual (SteerParticleVfx) |
| Kesan | "rudal pemandu" | "pemburu" | "pemburu" |

---

## 11. Referensi cepat (nomor baris ISIL)

**SkillShot.txt** — `Start` 3 · `Update` 531 · `CheckForTargetPositionReached` 1037 ·
`AimSpellAtTarget` 1313 · `OnRetargetedTargetUpdated` 1352 · `UpdateAimFromOwnerInput` 1564 ·
`MoveProjectileAlongCurve` 2186 · `MoveProjectileTowardsTarget` 2443 · `SweepStepForHits` 2697 ·
`CheckForMaximumDistanceReached` 3179 · `ExtrapolateMaxDistanceCheck` 3299 ·
`CaptureOriginalTargetPosition` 4029 · `RotateTowardsTarget` 4486 · `LaunchProjectile` 5686 ·
`SetMonsterTarget` 6392 · `RecomputeTargetAlongFacing2D` 6654.

**ChronoSpikePrefab.txt** — `OnStart` ~700 (snapshot target di ISIL 206-207, bow di 208-209) ·
`ReadSkillTreeUpgrades` 1234 · `SelectPriorityTarget` 1550 · `CountDotStacks` 2078 · `GetNearest` 2219 ·
`WindUpAndLaunch` 2804 · `ComputeDashDestination` 2862 · `AssignSplittingVolley` 3256 ·
`ComputeLateralBows` 3525 · `GroundSpotForSlot` 3920 · `SpawnSplitProjectiles` 4220 ·
`SelectTopPriorityTargets` 5085 · `ApplyTargetHit` 5708 · `OnDestinationReached` 6081 · `ResolveImpact` ~7361.

**AetherHowlProjectilePrefab.txt** — `OnStart` 3 · `Update` 44 · `UpdateHomingTarget` 77 ·
`ResolveHomingTarget` 712 · `CacheInitialDirection` 801 · `RotateTowards` 983 · `ApplyTargetHit` 1053.

---

## 12. Status verifikasi

| Klaim | Status |
|---|---|
| `0xF0 = BaseSpellLibrary._targetPosition` (Vector2) | ✅ terverifikasi (cpp2il field layout + ISIL) |
| Chrono menulis `0xF0` hanya saat cast (3 situs, semuanya di OnStart/AssignSplittingVolley) | ✅ terverifikasi (grep seluruh dump) |
| Formula `MoveProjectileAlongCurve` (lerp + sin(π·p)·arc + lateral·perpendicular) | ✅ terverifikasi instruksi per instruksi |
| Formula `MoveProjectileTowardsTarget` (lurus + snap arrival) | ✅ terverifikasi |
| Pipeline retarget = aim owner (bukan tracking musuh) | ✅ terverifikasi (OnRetargetedTargetUpdated + handler BaseSpell) |
| AetherHowl = homing turn-rate menulis look-ahead ke `0xF0` tiap frame | ✅ terverifikasi |
| Nilai literal float (§9) | ⚠️ inferred dari pemakaian (pool literal IL2CPP bersama) |
| Detail damage/DoT (stack consumption, 50% repeat hit) | ✅ lihat `docs/ChronoSpike_Spell_HowItWorks.md` §13-14 |
