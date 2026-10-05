# Dimraeth Sanctum Chests

A standalone BepInEx 6 (IL2CPP) plugin that adds a **Sanctum Chests** button list
underneath the map quick-actions panel supplied by the friend-made
`DimraethMapActions v1.1.0 Extended` mod.

It does **not** modify that DLL or the `DimraethMapActionsShopPatch` plugin. It resolves
the original type by name at runtime and Harmony-postfixes one method.

## What it does

- Lists every chest placed in the player's base (the `PlayerBase` scene, "Sanctum").
- One clickable button per chest, labelled `"<name> (<used>/<total>)"`.
  - The name is the player-renamed `Storage.DisplayName`, falling back to
    `StorageName`, `UniqueID`, or the object name.
  - Slot usage comes from the chest's replicated `Inventory`.
- Chests are listed in alphabetical order.
- Clicking a button opens that chest **remotely**, from anywhere on the map, using the
  game's own storage UI (the same approach the existing `RahanerChestMod` uses).
- The list is capped in height and scrolls (drag handle + mouse wheel) so any number of
  chests can be shown.
- The list refreshes every time the map panel is shown, so newly placed/renamed chests
  appear next time you open the map.

Only chest-type containers are included
(`ChestBasic`, `ChestBarrel`, `PinewoodChest`, `OakCabinet`, `HeartOakChest`); workbenches,
stoves and other facilities are not.

## Install

The build copies `DimraethSanctumChests.dll` straight into
`BepInEx\plugins\`. Requires:

- BepInEx 6 IL2CPP (already used by the other mods here)
- `DimraethMapActions-v1.1.0-Extended.dll` installed (optional but that is what provides
  the host panel)

## Build

```
& "..\..\..\modding\dotnet-sdk\dotnet.exe" build ".\DimraethSanctumChests.csproj" -c Release
```

The output path is `..\..\..\BepInEx\plugins\`.

## Config

`BepInEx\config\dev.dimraeth.sanctumchests.cfg`

| Key | Default | Meaning |
| --- | --- | --- |
| `General.Enabled` | `true` | Add the chest list under the map panel. |
| `General.VerboseLog` | `true` | Log the storage scan while building the list. |
| `General.OnlyAccessible` | `true` | Skip chests the local player cannot open. |
| `General.PanelHeight` | `190` | Height in pixels of the scrollable list. |

## Notes / limits

- A chest must be network-spawned to be listed (its contents are only readable then).
  Chests in the Sanctum are normally spawned whenever the base is loaded.
- Opening is a local UI action mirroring interaction; if a chest is genuinely locked by
  ownership/quest rules it may still be filtered by `OnlyAccessible` or refuse to open.
