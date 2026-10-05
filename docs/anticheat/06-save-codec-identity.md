# 06 — SaveCodec & SaveCodec.IdentityHasher (save envelope crypto + identity tag)

**Scope.** Static analysis of the decompiled game code only. All behavioral claims cite
`file:line` in the ISIL dumps under
`modding\cpp2il_isil_out\IsilDump\Assembly-CSharp\` (directory omitted below).
Signatures/constants are cross-checked against `modding\DecompilerTool\ilrecovery_out\`.
No runtime testing; nothing modified.

**Confidence legend.** **Verified** = directly observable at cited lines; **Inferred** =
strongly suggested by cited evidence but needs interpretation (enum ordinals, generic
thunk identities, helper semantics); **Unknown** = dump does not resolve it (collected in
*Open questions*, never guessed in the body).

**Sources**

| Source | Role |
|---|---|
| `SaveCodec.txt` (5778 lines) | real bodies of all 13 `SaveCodec` methods |
| `SaveCodec_NestedType_IdentityHasher.txt` (206 lines) | real bodies of `IdentityHasher` |
| `DecompilerTool\ilrecovery_out\SaveCodec.cs` (= `Algo.cs` = `IdentityHasher.cs`) | signatures, constants, `Algo` enum |
| `SaveNameVerdict.txt`, `SaveNameRules.txt`, `ilrecovery_out\SaveNameVerdict.cs` | brief: save-name verdicts vs identity tag |
| `SaveSystem.txt`, `SaveSystem_NestedType___c__DisplayClass*.txt`, `QACheckpoint.txt` | enforcement call sites |
| `CharacterIdentityIntegrity.txt` | consumer of `ComputeIdentityTag`/`CreateIdentityHasher` (doc 04) |

---

## 1. Purpose & threat model

`SaveCodec` is the **local save-file cryptographic envelope** plus the **identity-tag key
hierarchy**:

1. **Envelope.** Every save file is `JSON → UTF-8 → GZip → AES-CBC (PKCS7)`, wrapped with a
   versioned binary header and a HMAC-SHA256 (or, on a legacy read path, AES-GCM)
   authentication tag. Per-file random salt (16 B) and nonce (12 B) drive per-file key
   derivation via HKDF-SHA256.
2. **Identity tags.** A dedicated derived key (`info = "identity"`, salt
   `"Dimraeth.CharacterIdentity.v1"`) feeds `IdentityHasher`, an HMAC-SHA256 that produces
   the Base64 identity tag used by `CharacterIdentityIntegrity` (doc 04).

**Threat model (defends against).** (a) Casual save-file editing — any byte change in the
ciphertext or header breaks the MAC (`CryptographicOperations.FixedTimeEquals` check) and
fails decryption with `CryptographicException`; (b) file corruption — magic/version/length
framing and `EndOfStreamException` on short reads; (c) identity-field tampering — the tag
subsystem (doc 04); (d) plain obfuscation of save contents from quick inspection.

**What it does NOT defend against.** A determined reverse engineer. The master key is
**assembled at runtime from constants compiled into the game** — four 8-byte static arrays
`S0..S3` mixed through a fixed LCG seeded with `KeyMaskSeed` (§4). Anyone with the binary
can reproduce `ComposeMasterKey()` and derive every subkey. There is no per-user secret,
no server, and the KDF salt for identity tags is a hardcoded ASCII literal. This is
tamper *evidence* and friction, not key secrecy. **Verified** (constants in `.cctor`
L5154–5778; mixing in `ComposeMasterKey` L103–201).

`UseAesGcmForWriting = false` (**Verified**, `SaveCodec.cs`) with `Algo.AesCbcHmac = 2`
means **new saves are written with AES-CBC + HMAC-SHA256**; the AES-GCM branch exists on
the read path only (legacy files). **Inferred** (write-path algo byte = 2 is observed at
L1801–1805; the const is Verified).

---

## 2. API surface

From `ilrecovery_out\SaveCodec.cs` (identical in `Algo.cs` / `IdentityHasher.cs`), bodies
per the ISIL dumps:

| Member | Kind | Signature / value | Body at |
|---|---|---|---|
| `Algo` | private enum : byte | `AesGcm = 1, AesCbcHmac = 2` | n/a (declaration) |
| `IdentityHasher` | public sealed class : IDisposable | field `_hmac` (HMACSHA256, at +16) | `SaveCodec_NestedType_IdentityHasher.txt` |
| `FILE_MAGIC` | private const uint | `1246909254u` (= `0x4A525346`) | n/a (value seen L1561) |
| `FILE_VERSION` | private const byte | `1` | n/a (value seen L1567) |
| `INFO_ENC` | static readonly byte[] | ASCII `"enc"` | `.cctor` L5514–5524 |
| `INFO_MAC` | static readonly byte[] | ASCII `"mac"` | `.cctor` L5530–5540 |
| `INFO_IDENTITY` | static readonly byte[] | ASCII `"identity"` | `.cctor` L5547–5557 |
| `IDENTITY_SALT` | static readonly byte[] | ASCII `"Dimraeth.CharacterIdentity.v1"` | `.cctor` L5564–5574 |
| `KeyMaskSeed` | private const uint | `625341585u` (= `0x2545F491`) | n/a (value seen L166) |
| `S0..S3` | static readonly byte[] | 4 × `byte[8]` (RVA-initialized) | `.cctor` L5580–5643 |
| `UseAesGcmForWriting` | private const bool | `false` | n/a (declaration) |
| `Rng` | static readonly | `RandomNumberGenerator.Create()` | `.cctor` L5645 |
| `NSettings` | static readonly | `JsonSerializerSettings` | `.cctor` L5654–5741 |
| `ComposeMasterKey` | private static | `byte[] ComposeMasterKey()` | L3 (body L103–201) |
| `DeriveSubkey` | private static | `byte[] DeriveSubkey(byte[] salt, ReadOnlySpan<byte> info)` | L203 |
| `ComputeIdentityTag` | public static | `string ComputeIdentityTag(string canonical)` | L557 |
| `CreateIdentityHasher` | public static | `IdentityHasher CreateIdentityHasher()` | L756 |
| `EncryptAndSerialize<T>` | public static | `void EncryptAndSerialize<T>(Stream fs, T obj)` | L938 (ISIL L1464–2000) |
| `DecryptAndDeserialize<T>` | public static | `T DecryptAndDeserialize<T>(Stream fs)` | L2002 (ISIL L2764–3536) |
| `HkdfSha256` | private static | `void HkdfSha256(byte[] ikm, byte[] salt, ReadOnlySpan<byte> info, int length, Span<byte> okm)` | L3538 |
| `SerializeAndCompress<T>` | private static | `byte[] SerializeAndCompress<T>(T obj)` | L4142 |
| `DecompressAndDeserialize<T>` | private static | `T DecompressAndDeserialize<T>(byte[] data)` | L4444 |
| `WriteU32` | private static | `void WriteU32(Stream s, uint v)` | L4828 |
| `ReadU32` | private static | `uint ReadU32(Stream s)` | L4936 |
| `GetU32BE` | private static | `byte[] GetU32BE(uint v)` | L5068 |
| *(static init)* | `.cctor` | — | L5154 (ISIL L5467–5777) |

`IdentityHasher` methods: `.ctor(byte[] key)` at `SaveCodec_NestedType_IdentityHasher.txt`
L3, `Compute(string canonical)` L70, `Dispose()` L170. All 13 + 3 method headers are the
complete sets in their files (**Verified**, header scans).

---

## 3. Constants and string literals (exact)

| Constant | Exact value | Where |
|---|---|---|
| `FILE_MAGIC` | `1246909254u` = `0x4A525346` (written big-endian: `4A 52 53 46`) | ilrecovery decl.; immediate `0x4A525346` at `SaveCodec.txt:1561` |
| `FILE_VERSION` | `1` (byte) | ilrecovery decl.; immediate at L1567 |
| `KeyMaskSeed` | `625341585u` = `0x2545F491` | ilrecovery decl.; immediate at L166 |
| LCG multiplier / increment | `0x19660D` = `1664525` / `0x3C6EF35F` = `1013904223` (Numerical Recipes LCG) | L184–185 |
| `S0..S3` | each `byte[8]` (32 bytes total key material), values in RVA data at `[0x185A17458]`, `[0x185A1CA98]`, `[0x185A1BDA8]`, `[0x185A1D110]` | L5580–5643 |
| Master key / subkey length | 32 bytes | L159–161 (`SzArrayNew byte[1],32`); `DeriveSubkey` output `byte[32]` L321–324 |
| HKDF output length arg | `32` | L341 (`r9d,20h` into `HkdfSha256`) |
| HKDF expand counter | 1 byte, first value `1` | L3929–3930 (`r13,1`), counter array `byte[1]` L3985–3991 |
| salt / nonce sizes | 16 / 12 bytes | L1527–1530 (salt), L1545–1548 (nonce) |
| IV | 16 bytes = nonce(12) ‖ 4 fresh RNG bytes | L1618–1651 (two `Array.Copy`) |
| MAC tag | 32 bytes (HMAC-SHA256 full digest) | `TransformFinalBlock` result written last; digest size implicit |
| Identity tag | Base64 of the 32-byte HMAC-SHA256 → 44 chars incl. one `=` pad | `Convert.ToBase64String` L716 / IH L160 (length **Inferred** from Base64 of 32 B) |
| `Algo` | `AesGcm = 1`, `AesCbcHmac = 2` | ilrecovery `SaveCodec.cs` |
| KDF label strings | `"enc"`, `"mac"`, `"identity"`, salt `"Dimraeth.CharacterIdentity.v1"` (all `Encoding.ASCII`) | `.cctor` L5514–5574 (strings inline in ISIL) |
| AES settings (write) | Mode `1` (CBC), Padding `2` (PKCS7) | L1666–1675 (setter immediates `rdx,1` / `rdx,2`) — enum mapping **Inferred** |
| GZip (write) | `GZipStream(ms, CompressionMode.Compress=1, leaveOpen: true)` | L4360–4364 — enum mapping **Inferred** |
| `NSettings` | `Formatting = 0`, `ReferenceLoopHandling = 1`, converters `[FixedStringJsonConverter, FixedListJsonConverter]`, `Error` handler from `SaveCodec+<>c` | L5654–5761 — enum mappings **Inferred** |

Exact strings quoted from the dump are **Verified**; numeric enum interpretations are
marked **Inferred** in the table.

---

## 4. Key hierarchy & KDF construction

```
S0,S1,S2,S3 (4×8 B, compiled in) + KeyMaskSeed (0x2545F491)
        │  ComposeMasterKey(): 32-step LCG mix
        ▼
master (32 B, zeroed after use)
        │  DeriveSubkey(salt, info) = HkdfSha256(ikm=master, salt, info, 32, okm)
        │
        ├─ salt = per-file random 16 B, info = "enc"       → encKey   (AES key)
        ├─ salt = per-file random 16 B, info = "mac"       → macKey   (HMAC key)
        └─ salt = "Dimraeth.CharacterIdentity.v1", info = "identity"
                                                            → identity key (IdentityHasher)
```

### 4.1 `ComposeMasterKey()` — L3, body L103–201

Reconstructed exactly from the ISIL (**Verified**, condensed from L116–198):

```csharp
var parts = new byte[][] { S0, S1, S2, S3 };     // statics +32..+56   (L116–158)
var key = new byte[32];                          //                    (L159–161)
uint state = 0x2545F491;                         // KeyMaskSeed        (L166)
for (int i = 0; i < 32; i++) {                   //                    (L167–193)
    var part = parts[i & 3];                     // i&3                (L170–174)
    state = state * 0x19660D + 0x3C6EF35F;       // LCG step           (L184–185)
    key[i] = (byte)((state >> 24) ^ part[i >> 2]);  //                (L186–191)
}
return key;                                      //                    (L194–198)
```

Each `S*` is 8 bytes and `i >> 2` runs 0..7, so every key byte XORs one LCG output byte
with one static byte. The LCG is applied **before** the first output byte (state seeded
outside the loop, updated inside). **Verified** (L166 vs L184–185 ordering).

### 4.2 `DeriveSubkey(byte[] salt, ReadOnlySpan<byte> info)` — L203

1. `master = ComposeMasterKey()` (inlined into the disassembly L229–315; the formal ISIL
   references the same constants). **Verified**.
2. `okm = new byte[32]` (L321–324).
3. `HkdfSha256(master, salt, info, 32, okm)` — call at L521, length 32 at L341.
   **Verified**.
4. `CryptographicOperations.ZeroMemory(master)` (L531; also in the disassembly at
   L353–355) — the master key is wiped after use. **Verified**.
5. Returns `okm`. **Verified**.

### 4.3 `HkdfSha256(ikm, salt, info, length, okm)` — L3538–4141

Standard **RFC 5869 HKDF with SHA-256**, extract-then-expand:

- **Extract:** `prk = new HMACSHA256(salt).ComputeHash(ikm)` — HMAC keyed by **salt**,
  message **ikm** (L3892 `HMACSHA256..ctor(salt)`, L3902 `ComputeHash(ikm)`). **Verified**.
- **Expand** (loop while `produced < length`, L3935–3936):
  - `hmac = new HMACSHA256(prk)` (L3943).
  - `T(i) = HMAC(prk, T(i-1) ‖ info ‖ counter)` where `T(0) = empty` (first iteration
    skips the `TransformBlock` for the empty previous block — guarded at L3950–3951),
    `info` is fed as a block (L3961–3982), and the **counter is a single byte** starting
    at `1` (`byte[1]` with `[0]=1` at L3985–3991; incremented per block at L4063).
    `TransformFinalBlock` closes each HMAC (L3999). **Verified**.
  - `n = Math.Min(length - produced, 32)` (L4018–4019) and the first `n` bytes of `T(i)`
    are copied into the `okm` slice (L4020–4060). **Verified**.
  - Span bounds misuse raises `ThrowHelper.ThrowArgumentOutOfRangeException`
    (L4029, L4040). **Verified**.

With 32-byte outputs only one expand block (`counter = 1`) is ever produced in practice;
the loop is generic in `length`. **Inferred** (from `length=32` call sites).

---

## 5. Identity tag: byte layout and where it lives

### 5.1 Production (`ComputeIdentityTag` L557–755 + `IdentityHasher`)

1. `ComputeIdentityTag(null)` → `null`. **Verified** (null check before hashing).
2. `using var hasher = CreateIdentityHasher()` (call at L678).
3. `bytes = Encoding.UTF8.GetBytes(canonical)` (L697 + virtual `GetBytes`). **Verified**.
4. `hash = hasher._hmac.ComputeHash(bytes)` (L708) — **32 raw bytes** of HMAC-SHA256 keyed
   by the "identity" subkey. **Verified**.
5. `return Convert.ToBase64String(hash)` (L716) — **44-char Base64 string** (32 bytes → 44
   chars incl. one `=` pad). String length **Inferred** (Base64 arithmetic); the call is
   **Verified**.

`CreateIdentityHasher()` (L756–937):
- `key = DeriveSubkey(salt: IDENTITY_SALT, info: INFO_IDENTITY)` — call at L884 with the
  statics-sourced salt/info spans. **Verified** (call + statics reads) / salt==`IDENTITY_SALT`,
  info==`INFO_IDENTITY` **Inferred** from static offsets (+24/+16) and §3 labels.
- Allocates `HMACSHA256(key)` (L907; same construction appears in `IdentityHasher..ctor`
  at IH L52–58 — inlined here **Inferred**) and stores it in `_hmac` at `this+16`
  (IH L59–60). **Verified**.
- `CryptographicOperations.ZeroMemory(key)` (L921) — the derived key is wiped after the
  HMAC has copied it. **Verified**.

`IdentityHasher` internals (`SaveCodec_NestedType_IdentityHasher.txt`):
- `.ctor(byte[] key)` L3–68: base ctor + `_hmac = new HMACSHA256(key)` at `+16`. **Verified**.
- `Compute(string canonical)` L70–168: `canonical == null || _hmac == null` → `return null`
  (L131–134, return at L162–167); else
  `_hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical))` (L137–148) →
  `Convert.ToBase64String(...)` (L149–161). **Verified**.
- `Dispose()` L170–205: `_hmac?.Dispose()` (L193–197) then `_hmac = null` (L198–204).
  **Verified**.

### 5.2 Tag byte layout

| Layer | Layout | Evidence |
|---|---|---|
| Raw HMAC | 32 bytes, HMAC-SHA256(key = identity subkey, msg = UTF-8(canonical)) | L697–708 / IH L137–148 |
| Stored form | Base64 of the 32 bytes → 44 chars (`A–Za–z0–9+/` + `=` pad) | L716 / IH L160 |
| Storage location | `PlayerData.characterIdentityTag`, native offset `+0x78` (120), inside the **JSON payload** (default Newtonsoft field-name naming, i.e. property `"characterIdentityTag"` — **Inferred**; `NSettings` sets no naming policy, L5654–5741) | doc 04 offset map; §6 payload chain |
| Not present in | the envelope framing/header — the envelope never stores or checks the identity tag itself | §6 layout (**Verified**) |

### 5.3 Where the tags sit in the byte stream (both senses of "tag")

**Authentication tag (MAC).** 32 bytes, the **last field of the envelope**, computed over
everything except itself and the magic/version/algo bytes:
`HMAC(macKey, salt ‖ nonce ‖ cipherLenBE ‖ iv ‖ cipher)` — four `TransformBlock` calls
(L1859, L1869, L1882, L1894) plus `TransformFinalBlock` for the cipher (L1902), i.e. the
covered regions in **exactly file order**. On read, the recomputed MAC is compared with
`CryptographicOperations.FixedTimeEquals` (L3053). **Verified**.

**Identity tag.** Not a stream field at all; it lives at `PlayerData +0x78` inside the JSON
that is GZip-compressed and encrypted as the envelope's *ciphertext payload*. The envelope
only protects it transitively (as ciphertext + MAC). **Verified** (payload chain §6.2).

---

## 6. Envelope byte layout & per-method behavior

### 6.1 File layout (`EncryptAndSerialize<T>` L938–2001, ISIL L1464–2000)

| # | Field | Size | Write evidence |
|---|---|---|---|
| 1 | `FILE_MAGIC` u32 BE (`0x4A525346`) | 4 | `WriteU32(fs, 0x4A525346)` L1560–1563 |
| 2 | `FILE_VERSION` u8 (`1`) | 1 | `fs.WriteByte(1)` (slot 39) L1564–1569 |
| 3 | algo u8 (`2` = `AesCbcHmac`) | 1 | virtual `WriteByte(2)` (slot 944) L1798–1805 |
| 4 | salt (RNG) | 16 | generated L1524–1541 (`Rng.GetBytes`, slot 6); written L1808–1814 |
| 5 | nonce (RNG) | 12 | generated L1542–1559; written L1817–1823 |
| 6 | cipherLen u32 BE | 4 | `WriteU32(fs, cipher.Length)` L1826–1833 |
| 7 | iv = nonce(12) ‖ rng(4) | 16 | assembled L1618–1651; written L1927–1939 |
| 8 | cipher | cipherLen | written L1940–1946 |
| 9 | mac (HMAC-SHA256) | 32 | written last, L1949–1955 |

MAC covered bytes: fields 4–8 (`salt ‖ nonce ‖ cipherLenBE ‖ iv ‖ cipher`), **Verified**
(L1859–1902). All nine writes **Verified** at the cited lines.

### 6.2 `EncryptAndSerialize<T>(Stream fs, T obj)` — L938–2001

1. `plain = SerializeAndCompress<T>(obj)` (generic thunk `0x1800A8580(obj, …)` at L1517–1523
   — attribution **Inferred**; see Open questions).
2. `salt = new byte[16]; Rng.GetBytes(salt)` (L1524–1541); `nonce = new byte[12];
   Rng.GetBytes(nonce)` (L1542–1559). **Verified**.
3. Write magic (L1560–1563) and version (L1564–1569). **Verified**.
4. `encKey = DeriveSubkey(salt, INFO_ENC)` (L1586–1604); `macKey = DeriveSubkey(salt,
   INFO_MAC)` (L1605–1616). **Verified** (statics offsets +0 and +8 = the `"enc"`/`"mac"`
   labels of §3).
5. `iv = new byte[16]`; `Array.Copy(nonce, iv, 12, 0)` (L1618–1628); 4 RNG bytes appended
   via `Array.Copy(rng4, 0, iv, 12, 4)` (L1629–1651). **Verified**.
6. `Aes.Create()` (L1657); set `Mode = 1` (CBC) (L1666–1668), `Padding = 2` (PKCS7)
   (L1673–1675), `Key = encKey` (L1679–1682), `IV = iv` (L1687–1689). **Verified**
   (immediates) / enum names **Inferred**.
7. `cipher = ` AES-CBC over `plain` through `CryptoStream(new MemoryStream(), encryptor,
   Write)` (L1690–1731) with final-block flush on dispose (L1733–1738); `cipher =
   ms.ToArray()` (L1753–1760). **Verified**.
8. Write algo byte 2 (L1798–1805), salt (L1808–1814), nonce (L1817–1823), `cipherLen`
   (L1826–1833). **Verified**.
9. `mac = HMACSHA256(macKey)` over `salt ‖ nonce ‖ GetU32BE(cipherLen) ‖ iv ‖ cipher`
   (`GetU32BE` at L1835–1837, HMAC init L1839–1845, blocks L1859/1869/1882/1894,
   final L1902, digest via slot 496 L1903–1908). **Verified**.
10. Write iv (L1927–1939), cipher (L1940–1946), mac (L1949–1955). **Verified**.
11. Oddity: an unconditional-looking `PlatformNotSupportedException(<literal
    [0x185A01BB8]>)` raise at L1571–1584 sits between steps 3 and 4 with no predicate
    visible — almost certainly an interleaved EH clause (a save-write that always threw
    would be nonsensical). **Verified** occurrence / semantics **Unknown** (Open questions).

### 6.3 `DecryptAndDeserialize<T>(Stream fs)` — L2002–3537 (ISIL L2764–3536)

1. `magic = ReadU32(fs)` (L2821); `!= 0x4A525346` → `CryptographicException` with message
   literal `[0x185A40D50]` (literal at L3339, ctor L3344). **Verified** (throw sites);
   message text **Unknown**.
2. `version = fs.ReadByte()`; `!= 1` → `CryptographicException`, literal `[0x185A411C0]`
   (L3526, ctor L3531). **Verified**.
3. `algo = fs.ReadByte()`:
   - `2` (AesCbcHmac) → path of steps 4–8.
   - `1` (AesGcm) → `encKey = DeriveSubkey(salt, INFO_ENC)` (L3286), `new AesGcm(encKey)`
     (L3293–3299), `Decrypt(nonce, cipher, tag16, plain)` (L3313). Legacy read path.
     **Verified** calls; GCM tag-size 16 and stream position of `tag16` **Inferred**.
   - else → `CryptographicException`, literal `[0x1859E7660]` (L3354, ctor L3359).
4. `cipherLen = ReadU32(fs)` (L2874); short reads anywhere → `EndOfStreamException`
   (via `ReadU32` itself L5054–5066 and stream-read sites; type literal `[0x185A90740]`
   reused at L3406–3509). **Verified** (throw sites).
5. `encKey = DeriveSubkey(salt, INFO_ENC)` (L2943); `macKey = DeriveSubkey(salt, INFO_MAC)`
   (L2956). **Verified**.
6. Recompute MAC over `salt ‖ nonce ‖ GetU32BE(cipherLen) ‖ iv ‖ cipher`
   (`GetU32BE` L2999; HMAC init L2964; blocks L2980/2992/3011/3023; final L3031), then
   `CryptographicOperations.FixedTimeEquals(recomputed, stored)` (L3053) — mismatch →
   `CryptographicException`, literal `[0x185A60368]` (L3369, ctor L3374). **Verified**.
7. AES-CBC decrypt: `Aes.Create` (L3082), `CryptoStream(new MemoryStream(), decryptor,
   …)` (L3120–3141), `Key = encKey`, `IV = iv`. **Verified**.
8. `return DecompressAndDeserialize<T>(plain)` (generic thunk `0x18195B740` at L3222).
   **Verified**.
9. A fifth `CryptographicException` ctor at L3456 (literal via `[0x185A67CC8]`, message
   unresolved) has an unidentified trigger. **Verified** existence / trigger **Unknown**.

### 6.4 `SerializeAndCompress<T>(T obj)` — L4142–4443

1. `json = JsonConvert.SerializeObject(obj, NSettings)` (L4325–4332). **Verified**.
2. `bytes = Encoding.UTF8.GetBytes(json)` (L4335–4343). **Verified**.
3. `ms = new MemoryStream()` (L4350); `gzip = new GZipStream(ms, 1 /*Compress*/,
   true /*leaveOpen*/)` (L4356–4364). **Verified** (immediates) / enums **Inferred**.
4. `gzip.Write(bytes, 0, bytes.Length)` (virtual slot 912, L4375–4380). **Verified**.
5. Dispose `gzip` (L4382–4387); `result = ms.ToArray()` (slot 1024, L4388–4393); dispose
   `ms` (L4396–4401). **Verified**.

### 6.5 `DecompressAndDeserialize<T>(byte[] data)` — L4444–4827

1. `input = new MemoryStream(data)` (L4666–4672). **Verified**.
2. `gzip = new GZipStream(input, 0 /*Decompress*/)` (L4678–4685). **Verified** / enum
   **Inferred**.
3. `outMs = new MemoryStream()` (L4690–4695); `gzip.CopyTo(outMs)` (L4700–4705). **Verified**.
4. `text = Encoding.UTF8.GetString(outMs.ToArray())` (L4706–4721). **Verified**.
5. `result = JsonConvert.DeserializeObject<T>(text, NSettings)` (generic JSON thunk
   `0x181860890(text, NSettings, typeof(T))` at L4723–4739). **Verified**.
6. `result == null` → `throw new CryptographicException(<literal [0x185A47B80]>)`
   (L4804–4817). **Verified** (throw) / message **Unknown**.
7. All three streams disposed via `IDisposable` in finally paths (L4744–4791). **Verified**.

### 6.6 `WriteU32` / `ReadU32` / `GetU32BE` — L4828–5153

- `WriteU32(Stream s, uint v)`: `buf = { v>>24, v>>16, v>>8, v }` (L4894–4917) then
  `s.Write(buf, 0, 4)` (virtual slot 912, L4918–4926). **Big-endian. Verified.**
- `ReadU32(Stream s)`: reads exactly 4 bytes (virtual slot 880, L5025–5033); a short read
  (`!= 4`) → `throw new EndOfStreamException()` (L5054–5066). Reassembles big-endian:
  `((b0<<8|b1)<<8|b2)<<8|b3` (L5036–5047). **Verified.**
- `GetU32BE(uint v)`: allocates and returns `byte[4] { v>>24, v>>16, v>>8, v }`
  (L5121–5149). **Verified.**

### 6.7 `.cctor` — L5154–5777 (ISIL L5467–5777)

1. `INFO_ENC = Encoding.ASCII.GetBytes("enc")` (L5514–5524) at statics+0. **Verified**.
2. `INFO_MAC = ASCII("mac")` (L5530–5540) at statics+8. **Verified**.
3. `INFO_IDENTITY = ASCII("identity")` (L5547–5557) at statics+16. **Verified**.
4. `IDENTITY_SALT = ASCII("Dimraeth.CharacterIdentity.v1")` (L5564–5574) at statics+24.
   **Verified** (string quoted from dump).
5. `S0..S3 = new byte[8] { … }` via `RuntimeHelpers.InitializeArray` from RVA blobs
   `[0x185A17458]`, `[0x185A1CA98]`, `[0x185A1BDA8]`, `[0x185A1D110]` (L5580–5643) at
   statics+32..+56. **Verified** (allocation + wiring) / byte values **Unknown**.
6. `Rng = RandomNumberGenerator.Create()` (L5645) at statics+64. **Verified**.
7. `NSettings = new JsonSerializerSettings { Formatting = 0, ReferenceLoopHandling = 1,
   Converters = { new FixedStringJsonConverter(), new FixedListJsonConverter() },
   Error = <SaveCodec+<>c handler> }` (L5654–5761) at statics+72. **Verified**
   (immediates + constructor calls) / enum names **Inferred**.

---

## 7. Enforcement call sites

Generic call convention (Verified from all sites): the thunk's extra register carries
`typeof(T)`; helpers `0x18195DEF0` = `EncryptAndSerialize<T>`, `0x18195C7F0` =
`DecryptAndDeserialize<T>`, `0x18195B740` = `DecompressAndDeserialize<T>` (internal),
`0x18195EDA0` = `SaveSystem.ReadSaveFile<T>` (**Inferred** identities; call shapes
**Verified**).

### Write path (encrypt)

| Call site | Enclosing | Behavior |
|---|---|---|
| `SaveSystem_NestedType___c__DisplayClass25_0.txt:174` (`<SavePlayerData>b__1(FileStream fs)`, method at L120) | `SavePlayerData` closure | `SaveFiles.AtomicWrite(...)` (same file L106) passes an `Action<FileStream>` that tail-calls `EncryptAndSerialize<T>(fs, obj)`. **Verified** |
| `…DisplayClass26_0.txt:184`, `…27_0.txt:174`, `…28_0.txt:184`, `…39_0.txt:68`, `…42_0.txt:68` | sibling save closures (player/world/settings/banlist/etc.; outer names **Inferred** from display-class numbering) | identical atomic-write + encrypt pattern. Calls **Verified** |

### Read path (decrypt)

| Call site | Enclosing method (header) | Behavior |
|---|---|---|
| `SaveSystem.txt:2964` | `SaveReadResult ReadSaveFile<T>(String path, out T obj)` (L2503) | central generic read helper: opens the file, `DecryptAndDeserialize<T>(fs)`; a null decode logs `Debug.LogError("[SaveSystem] '" + path + "' decoded to nothing - treating as corrupt.")` (literals L2972–2975) and returns result `2` (L2984). **Verified** |
| `SaveSystem.txt:4259, 4329` | `TryLoadPlayerWithFallback(String name)` (L3712) | `ReadSaveFile<PlayerSaveFile>` thunks (backup fallback logic around them; `EndsWith("bak1")`→`…+"2"` at L4234–4250). **Verified** (calls) / type arg **Inferred** |
| `SaveSystem.txt:4929, 4997` | `TryLoadWorldWithFallback(String name)` (L4624) | same pattern for world saves. **Verified** (calls) |
| `SaveSystem.txt:15167` | `<LoadSystemSettings>g__TryDeserializeSettings\|40_0(String path, out CharacterSettingsData s)` (L14995) | settings-file decode (`typeof` literal `[0x185A01618]`). **Verified** |
| `SaveSystem.txt:15374` | `<LoadBanList>g__TryDeserializeBanList\|43_0(String path, out BanListData list)` (L15239) | ban-list decode (literal `[0x185A014F8]`). **Verified** |
| `SaveSystem.txt:15663, 15687` | `<LoadWorldAll>g__TryLoadWorldWithFallbackLocal\|46_0(String name, String worldsDir)` (L15413) | world-file decode via `ReadSaveFile<T>`. **Verified** (calls) |
| `QACheckpoint.txt:4171, 4488` | QA tooling methods | direct `DecryptAndDeserialize<T>` thunks. **Verified** (calls) / purpose **Inferred** (QA checkpoint load) |

### Identity-tag API consumers

| Call | Site | Purpose |
|---|---|---|
| `SaveCodec.ComputeIdentityTag` | `CharacterIdentityIntegrity.txt:120` (`Stamp`), `:285` (`FailsIdentityCheck`) | tag production/verification (doc 04) |
| `SaveCodec.CreateIdentityHasher` | `CharacterIdentityIntegrity.txt:1030` (`Reconcile`) | brute-force tag recomputation (doc 04) |

**Verified** (exhaustive `Call SaveCodec.` + thunk grep across the dump directory).

---

## 8. SaveNameVerdict and the identity tag

`SaveNameVerdict` (ilrecovery + `SaveNameVerdict.txt`) is a **plain verdict DTO for
character/save *name* validation** — a struct with `IsValid` (+0), `ErrorKey` (+8),
`EnglishFallback` (+16), `IsEmpty` (+24):

| Method | Behavior | Evidence |
|---|---|---|
| `.ctor(bool, string, string, bool)` | straight field assignment | `SaveNameVerdict.txt:3–49` |
| `Ok()` | `{ IsValid=true, ErrorKey=EnglishFallback=<interned empty string via [0x185D2CA10]+0xB8>, IsEmpty=false }` | `SaveNameVerdict.txt:51–107` |
| `Fail(string key, string english, bool isEmpty = false)` | `{ IsValid=false, ErrorKey=key, EnglishFallback=english, IsEmpty=isEmpty }` | `SaveNameVerdict.txt:109–163` |

**Use of the identity tag: none.** A grep for `Identity|identity|SaveCodec|Canonical` over
`SaveNameVerdict.txt` and `SaveNameRules.txt` (the rules that produce these verdicts —
`Validate` at `SaveNameRules.txt:660`, plus `Normalize` L3, width/charset helpers) returns
**zero matches**. **Verified** (absence of references). The name-validation subsystem
(length/width/Unicode-category rules) is fully decoupled from the identity-tag subsystem;
the only shared detail is the same interned-empty-string helper `[0x185D2CA10]+0xB8`
(**Verified**, `SaveNameVerdict.txt:57–66` vs doc 04 `Field` fallback).

---

## 9. Evidence appendix (claim → evidence → confidence)

`SC.txt` = `SaveCodec.txt`; `IH.txt` = `SaveCodec_NestedType_IdentityHasher.txt`;
`SNV.txt` = `SaveNameVerdict.txt`; `SS.txt` = `SaveSystem.txt`;
`ILR` = `DecompilerTool\ilrecovery_out`.

| # | Claim | Evidence | Conf. |
|---|---|---|---|
| 1 | 13 `SaveCodec` + 3 `IdentityHasher` methods at the listed headers | `SC.txt` / `IH.txt` header scans | Verified |
| 2 | `FILE_MAGIC=1246909254u`, `FILE_VERSION=1`, `KeyMaskSeed=625341585u`, `UseAesGcmForWriting=false`, `Algo{1,2}` | `ILR\SaveCodec.cs`; immediates `SC.txt:1561,1567,166` | Verified |
| 3 | Labels `"enc"/"mac"/"identity"`, salt `"Dimraeth.CharacterIdentity.v1"` via `Encoding.ASCII` | `SC.txt:5514–5574` | Verified |
| 4 | `S0..S3` = 4×`byte[8]` from RVA blobs | `SC.txt:5580–5643` | Verified (values Unknown) |
| 5 | `ComposeMasterKey`: `key[i] = (state>>24) ^ parts[i&3][i>>2]`, `state = state*0x19660D + 0x3C6EF35F`, seed `0x2545F491` | `SC.txt:116–198` | Verified |
| 6 | `DeriveSubkey` = HKDF(master, salt, info, 32) + master wipe | `SC.txt:321–324,341,521,531` | Verified |
| 7 | HKDF-SHA256 extract `HMAC(salt, ikm)`, expand `T(i)=HMAC(prk, T(i-1)‖info‖counter8[i])`, counter starts 1 | `SC.txt:3892,3902,3943,3961–3999,4063` | Verified |
| 8 | `ComputeIdentityTag` = Base64(HMAC-SHA256(UTF-8(canonical))) or null | `SC.txt:678,697,708,716` | Verified |
| 9 | `CreateIdentityHasher` derives the "identity" subkey and zeroes it | `SC.txt:884,907,921` | Verified (label binding Inferred) |
| 10 | `IdentityHasher._hmac` at +16; `Compute` null-safe; `Dispose` disposes+nulls | `IH.txt:52–60,131–167,193–204` | Verified |
| 11 | Identity tag = 44-char Base64 of 32-byte HMAC | `SC.txt:716` + Base64 arithmetic | Verified (length Inferred) |
| 12 | Identity tag stored at `PlayerData +0x78`, inside JSON payload, not in envelope framing | doc 04 offsets + §6.1 layout | Verified (JSON name Inferred) |
| 13 | Envelope order magic→version→algo→salt→nonce→cipherLen→iv→cipher→mac | `SC.txt:1560–1569,1798–1833,1927–1955` | Verified |
| 14 | mac = HMAC-SHA256(macKey, salt‖nonce‖cipherLenBE‖iv‖cipher) | `SC.txt:1835–1902` | Verified |
| 15 | mac is 32 B, final field; iv = nonce12‖rng4 | `SC.txt:1618–1651,1949–1955` | Verified |
| 16 | Write path: AES-CBC (Mode 1) + PKCS7 (Padding 2) with per-file encKey | `SC.txt:1657–1689` | Verified (enum names Inferred) |
| 17 | Algo byte written = 2 | `SC.txt:1801–1805` | Verified |
| 18 | Payload chain JSON→UTF8→GZip(Compress, leaveOpen)→byte[] | `SC.txt:4332–4393` | Verified |
| 19 | Read path re-verifies MAC with `FixedTimeEquals` before decrypting | `SC.txt:2964–3053` | Verified |
| 20 | `CryptographicException`s: magic (`[0x185A40D50]` L3339), algo (`[0x1859E7660]` L3354), MAC (`[0x185A60368]` L3369), version (`[0x185A411C0]` L3526) | `SC.txt:3334–3374,3521–3531` | Verified (texts Unknown) |
| 21 | Short reads → `EndOfStreamException` | `SC.txt:5054–5066` (+ type reuse L3406–3509) | Verified |
| 22 | AesGcm read path exists (`AesGcm..ctor` L3299, `Decrypt` L3313) | `SC.txt:3286–3313` | Verified (layout Inferred) |
| 23 | `WriteU32`/`ReadU32`/`GetU32BE` are big-endian | `SC.txt:4894–4926,5036–5061,5121–5149` | Verified |
| 24 | `SerializeAndCompress`/`DecompressAndDeserialize` = GZip + Newtonsoft with `NSettings` | `SC.txt:4325–4393,4666–4739` | Verified |
| 25 | Deserialize-null → `CryptographicException` (`[0x185A47B80]`) | `SC.txt:4804–4817` | Verified (text Unknown) |
| 26 | Encrypt call sites in `SaveFiles.AtomicWrite` closures | `…DisplayClass25_0.txt:106,174` (+ siblings) | Verified |
| 27 | Decrypt call sites incl. `ReadSaveFile<T>` + corrupt log | `SS.txt:2964,2972–2984,4259,4329,4929,4997,15167,15374,15663,15687` | Verified |
| 28 | `SaveNameVerdict` has zero identity-tag coupling | grep over `SNV.txt`+`SaveNameRules.txt` | Verified (absence) |
| 29 | `SaveNameVerdict.Ok/Fail` field layout & values | `SNV.txt:3–163` | Verified |
| 30 | `NSettings` = Formatting 0, RefLoop 1, FixedString/FixedList converters | `SC.txt:5654–5761` | Verified (enum names Inferred) |

---

## 10. Open questions

1. **Exception message texts.** Literals `[0x185A40D50]` (bad magic), `[0x185A411C0]`
   (bad version), `[0x1859E7660]` (unknown algo), `[0x185A60368]` (MAC mismatch),
   `[0x185A47B80]` (deserialize-null), `[0x185A01BB8]` (the platform-not-supported oddity),
   `[0x185A01DD8]` (`EndOfStreamException`) are indirected through helper `0x1805E5210`;
   their string contents are **Unknown**.
2. **`PlatformNotSupportedException` block at `SC.txt:1571–1584` (EncryptAndSerialize).**
   Appears with no visible predicate between the version-byte write and key derivation.
   Almost certainly an interleaved EH/fault clause (a write path that always threw could
   not produce saves), but the exact semantics are **Unknown**.
3. **Fifth `CryptographicException` ctor at `SC.txt:3456`** (decrypt path). Trigger and
   message **Unknown** (the other four are mapped).
4. **`S0..S3` byte values.** Reside in RVA data blobs not expanded in the dump → values
   **Unknown** (needed to actually compute the master key; see threat model).
5. **Generic thunk identities.** `0x1800A8580` = `SerializeAndCompress<T>` (L1517–1523) and
   `0x18195EDA0` = `SaveSystem.ReadSaveFile<T>` are **Inferred** from argument shapes
   (result types match exactly); not symbol-resolved in the dump.
6. **AES-GCM read layout.** `AesGcm.Decrypt(nonce, cipher, tag16, plain)` suggests a 16-byte
   tag at the tail position where CBC-HMAC stores 32; exact stream offsets for algo `1`
   are **Inferred** (the writer never emits algo `1`: `UseAesGcmForWriting=false`).
7. **JSON property name of the identity tag.** `PlayerData` fields carry no
   `JsonProperty` attributes (ilrecovery grep), and `NSettings` sets no naming policy →
   `"characterIdentityTag"` under default Newtonsoft naming, **Inferred**. A custom
   contract resolver registered elsewhere would change this.
8. **`NSettings` enum semantics.** `Formatting = 0` (None) and `ReferenceLoopHandling = 1`
   (Ignore) are **Inferred** from enum ordinals; `Error` handler body lives in
   `SaveCodec+<>c` (not analyzed here).
9. **`SaveFiles.AtomicWrite` semantics** (temp-file + rename?) — outside this doc's scope;
   only its callback contract (`Action<FileStream>`) is verified.
10. **Who calls the public `ComputeIdentityTag`/`CreateIdentityHasher` besides
    `CharacterIdentityIntegrity`** — exhaustive grep shows no other callers in
    Assembly-CSharp (**Verified** absence), but external/modding consumers are
    **Unknown**.
11. **`ReadU32`'s `EndOfStreamException` message** literal `[0x185A01DD8]` contents
    **Unknown** (same indirected-literal mechanism as #1).
