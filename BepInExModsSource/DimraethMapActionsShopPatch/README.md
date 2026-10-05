# DimraethMapActions Shop Patch

A standalone BepInEx 6 (IL2CPP) plugin that **patches the installed
`DimraethMapActions-v1.1.0-Extended.dll` at runtime**. It does not reference,
rebuild, or replace that DLL — it locates the type
`Dimraeth.MapActions.MapButtons` and Harmony-prefixes its private static
`FindShop(NPCType)`.

## Why

The map's **Befr Shop** / **Saqi Shop** buttons only open when a live, *stocked*
`NPCShop` exists. Stock is only filled on the **server when the NPC is
network-spawned** (`NPCShop.OnNetworkSpawn` → populate; `Update`/`StoreRefreshCheck`
also require `NetworkObjectId != 0`). The original lookup:

- inspects only the single schedule-active variant and its own GameObject; and
- its fallback scan rejects several real owner variants (notably `BefrSitting`).

NPCs are scene-streamed and only the schedule/quest-active variant is spawned, so the
button often reports `"<X> unavailable until <owner> is present."`

## What it does

Two Harmony prefixes on the original `MapButtons`:

1. **`FindShop`** — a widened finder. The original only accepts an `NPCShop` that is
   network-*spawned* and only matches `NPC.NPCType`, which is `None` until the NPC is
   activated. The finder matches on the reliable `NPCSharedShop` key (all pose
   variants — `Befr`/`BefrSitting`/`BefrInPlayerBaseA`, all `Saqi*`), checks children,
   and prefers an active object in a loaded scene.
2. **`OpenShop`** — if the owner is already spawned, the original runs unchanged.
   Otherwise (default `ForceNpcSpawn = true`) it closes the map and:
   - **spawns the owner itself**, mirroring the game's own spawn path
     (`NetworkPrefabManager.Singleton.NPCPrefabs` → `Instantiate` → `NetworkObject.Spawn`),
     at the owner's authored scene position, then
   - polls until the shop exists **and its replicated stock has populated**
     (`ItemsForSale`/`EquipmentForSale` non-empty, bounded by a short grace period),
     then opens it via `PSM.SwapToNewState(NPCBuy)` + `ItemShop.SetShop(...)`.

   `RequestReconcile` alone is not enough: the game only reconciles/spawns NPCs for a
   scene whose group has players in it, so an owner in **another area** (e.g. Befr while
   the player is in the farmlands) is never spawned that way. Hence the direct spawn.

   The spawn is gated by the owner's **own** `NPCScheduling.ShouldBeActive` state
   (`NpcSpawner.AnyActiveSchedule`, scene-placed schedulers only). An NPC that is quest-
   locked or off-duty therefore reports `false`, is **not** spawned, and its button keeps
   reporting that it is not present. This is intentional — the patch does not conjure
   locked NPCs. **Saqi Shop** stays unavailable until Saqi is unlocked; once unlocked and
   on-duty, the same path opens her shop too.

## Config

`BepInEx/config/dev.dimraeth.mapactionsshoppatch.cfg`:

- `General.ForceNpcSpawn` (default `true`) — if the owner is not spawned, spawn its
  registered prefab (mirroring the game's NPC spawn path) and open the shop once stocked.
  Does **not** bypass quest locks (see the schedule/quest gate above).
- `General.VerboseLog` (default `true`) — log every `NPCShop` candidate plus the owner's
  `NPCScheduling`/`EnvironmentNPCScheduling` state on a button press.
- `General.OpenWaitSeconds` (default `15`) — how long to wait for the NPC to spawn.


## Build / install

Built output goes straight to the game's plugin folder:

```
dotnet build .\DimraethMapActionsShopPatch.csproj -c Release
```

Use the repo's local SDK if `dotnet` is not on PATH:

```
D:\SteamLibrary\steamapps\common\Dimraeth\modding\dotnet-sdk\dotnet.exe build .\DimraethMapActionsShopPatch.csproj -c Release
```

Installed file: `BepInEx/plugins/DimraethMapActionsShopPatch.dll`
(plus its `.pdb` / `.deps.json`).

## Remove

Delete `BepInEx/plugins/DimraethMapActionsShopPatch.dll` (and its `.pdb` /
`.deps.json`). The original DLL is untouched.
