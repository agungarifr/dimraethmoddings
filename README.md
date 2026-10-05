# Dimraeth Moddings

Source code and documentation for the Dimraeth (Unity / IL2CPP) BepInEx mod suite,
plus the reverse-engineering tools and research docs used to build it.

This repository is a **source + docs snapshot** intended for device migration. Large,
regenerable artifacts (runtimes, SDKs, decompiled dumps, build outputs, old backups)
are intentionally excluded — see [Restoring build dependencies](#restoring-build-dependencies).

## Layout

```
BepInExModsSource/      Main mod projects (29 x .csproj) + per-mod README/NEXUS docs
ModsSource/             Earlier standalone mod sources (3 projects)
<tool>/                 Reverse-engineering / analysis tools (21 x .csproj)
docs/                   Research documentation
  anticheat/            Anti-cheat analysis (11 chapters + graph)
  graphify-reports/     Historical code-graph reports (markdown only)
assets/                 Art / thumbnails / mod.pdf
releases/               Published mod .zip packages
*.md, changelog.txt     Top-level reports (Hell Mode, Nexus, forensic reports, etc.)
AGENTS.md               Project agent/workflow notes
.gitignore
opencode.example.json   Redacted sample of the local agent config
```

## Mod projects (`BepInExModsSource/`)

| Project | Project | Project |
|---|---|---|
| AlwaysRegenMod | AntiCheatBypassMod | AttackSpeedMod |
| AutoPickupMod | BarrageOfArrowsTuner | CarryWeightMod |
| ConfigurableLevelCapFreebuff | ConfigurableLevelCapFreebuff_Debug | ContagionTuner |
| DamageNumberTuner | DayNightToggleMod | DeedUnlockerMod |
| DimraethMapActionsShopPatch | DimraethModPack | DimraethSanctumChests |
| EquipmentStatEditor | EquippedStatModifier | FireballTuner |
| HellModeLevel75Freebuff | HellModeMod | LootAndExpMod |
| LootModV2 | NavMeshFixMod | PerfectParryMod |
| PlagueShardsHoming | PursuingBlizzardTuner | RahanerChestMod |
| TwisterTuner | UpgradeBonusStatIsNotRandom | |

## Analysis tools

`BundleScan`, `ByteDump`, `CatDump`, `CheckRva`, `DirDump`, `DisasmProbe`, `DumpStrings`,
`InspectTool`, `Lz4Test`, `MetaInspect`, `MetaStrings`, `ModInspect`, `RelationshipMultiplier`,
`SaveProbe`, `ScanInterop`, `ScanMarkers`, `ScanRegion`, `ScanStrings`, `ScanTools`,
`SerDump`, `SerParse`.

## Restoring build dependencies

The projects compile against BepInEx and the game's IL2CPP interop assemblies, which are
**not** committed (game-derived and large). To build on the new device:

1. Install the game and BepInEx (IL2CPP, x64) into the game folder.
2. Place the mod source under `modding/BepInExModsSource` so the relative references
   (`..\..\BepInEx\core`, `..\..\BepInEx\interop`, and for some tools `..\..\MelonLoader\...`)
   resolve as they did originally. The `.csproj` files use `$(BepInExCore)` / `$(BepInExInterop)`
   MSBuild properties pointing at `modding/BepInEx`.
3. Run the BepInEx IL2CPP interop generator once so `BepInEx/interop/Assembly-CSharp.dll`
   and the other interop DLLs exist, then `dotnet build` each mod.

Suggested on-disk layout after cloning:

```
<game>/
  BepInEx/                     (installed runtime + generated interop)
  modding/
    BepInExModsSource/         (from this repo)
    ModsSource/
    <tools>/                   (from this repo)
```

## Notes

- `opencode.json` was **excluded** because it contained an API key. Copy
  `opencode.example.json` to `opencode.json` and set `MIDAS_API_KEY` in your environment.
- The CodeGraph index (`.codegraph/`) and graphify graph data are regenerable and excluded;
  the human-readable graph reports are kept under `docs/graphify-reports/`.
- Some files under `docs/` record reverse-engineered game internals (including a Supabase
  anon key captured from the shipped client).
