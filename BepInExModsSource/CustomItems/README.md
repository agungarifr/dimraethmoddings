# Custom Items

Data-driven custom item framework for Dimraeth (BepInEx 6 IL2CPP). Define new items —
consumables (food/drink/potions), resources, tools — and crafting recipes in a JSON
file, no recompiling. Ships with **Satay Madura** as the flagship item.

## How it works

The game's `Item` and `Recipe` are `ScriptableObject`s held in plain mutable lists on
`ItemManager.Singleton` (`Items`, `Recipes`) that every system reads: inventory,
crafting UI, shops, tooltips, drops. `ItemType` is just an int under the hood
(`InventoryEntry.Item` is a 4-byte field; saves and networking serialize it
numerically), so custom items register under free values (vanilla enum tops out at
393; defaults start at 1000).

On first frame where `ItemManager.Singleton` exists, this mod:
1. Parses `BepInEx\config\CustomItems\definitions.json` (seeded from the template on
   first run).
2. Builds `Item`/`Recipe` ScriptableObjects and adds them to `ItemManager`.
3. Patches string-name lookups (`Utility.ReturnItemFromString`, `Utility.ReturnItemType`,
   `Item.ReturnItemType`, `ItemManager.GetRecipeByString`) so custom names resolve.

## Satay Madura

- 10x BeastMeat -> 1x Satay Madura (Cooking, Campfire)
- Eating it: nourishment + 50% of max Health, Stamina and Concentration restored
  **over 30 minutes** (1800s), tracked per player and refreshed on re-eat.
- Icon: Roasted Droop Core sprite tinted red.

The over-time restore is implemented as a custom HoT (`RegenOverTime`): the game's
vanilla `Effect` entries only feed regen-rate formulas (Multiplier/Additive) and
cannot express "exactly 50% of max over N seconds". The tracker computes the totals
at consume time and writes them in per frame through the same public API vanilla
consumption uses (`ObjectsCommon.SetHealth/SetStamina/SetConcentration`).

## definitions.json

Top-level: `items` (ItemDef[]) and `recipes` (RecipeDef[]).

### ItemDef
| key | notes |
|---|---|
| `name` | unique key; usable wherever vanilla item names are accepted |
| `numericId` | optional `(ItemType)` value; auto-assigned >= 1000 when omitted |
| `category` | `ItemTooltipCategory`: `Food`, `Drink`, `HealthPotion`, `ConcentrationPotion`, `StaminaPotion`, `Cure`, `Potion`, `Resource`, `Spellbook`, `Recipe`, `Pet`, `OffensiveTool`, `HarvestTool` |
| `shopName`, `description`, `effectDescription` | tooltip text |
| `itemBarEquippable` | can go in the quick-use bar |
| `stackLimit`, `weight`, `buyValue`, `sellValue` | economy/stacks |
| `proficiency` | `None/Farming/Fishing/Cooking/Crafting/Alchemy/Gathering/Mining/Woodcutting` |
| `cooldownGroup`, `slotGroup` | `ItemBarCooldownGroup` / `ItemBarSlotGroup` names |
| `decayTime`, `decayIntoItem` | seconds until decay + target item name (0 = no decay) |
| `recipeUnlock`, `unlockRecipeOnPickUp` | vanilla recipe-unlock flow (comma-separated result names) |
| `icon` | `{ "fromItem": "<item name>", "tint": "#RRGGBB" }` |
| `edible` | see below; omit for non-consumables |

### EdibleDef
| key | notes |
|---|---|
| `type` | `Food`, `Drink`, `RecipeUnlock`, `MapFragment` |
| `nourishment` | hunger restored |
| `bonusHealth`, `bonusConcentration`, `bonusStamina` | instant top-up on eat |
| `noOverConsumption` | bypass over-consumption penalty |
| `effects` | vanilla pass-through: `[{ "effect": "RegenerationUp", "factor": 1.0, "time": 60 }]` (any `Effect` enum name) |
| `regenOverTime` | custom HoT: `{ "healthFraction", "staminaFraction", "concentrationFraction", "durationSeconds" }` — fraction of *current* max restored over the duration |

### RecipeDef
| key | notes |
|---|---|
| `uuid` | unique id (default `custom_<name>`) |
| `name` | name-lookup key (default = result) |
| `result`, `resultQuantity` | output item name + count |
| `ingredients` | `[{ "item": "BeastMeat", "amount": 10 }]` |
| `proficiency`, `bench` | `Proficiency` / `StorageContainers` names (`Campfire`, `WoodfireStove`, `WorkBench`, ...) |
| `craftingTime`, `awardedXp`, `levelRequired` | crafting meta |
| `craftingStationRequired` | require the bench |
| `isJunk`, `validRerollResult`, `groupId`, `orderInGroup` | vanilla recipe UI/discovery controls |
| `unlockedByDefault` | best-effort unlock on character load |

## Config (`BepInEx\config\com.custom.customitems.cfg`)

- `Enabled` (default true)
- `DebugLogging` (default false)
- `Debug.SpawnHotkey` (default F9): spawns one of each custom item at the player.

## Known limitations / pending verification

- In-game verification pending (build 2026-10-07): crafting-UI visibility of custom
  recipes (whether the game gates recipes behind unlock state), co-op behavior of the
  HoT on remote clients, and icon tint color fidelity.
- Saves store custom item ids as ints: characters carrying custom items need this mod
  installed, otherwise those slots show as unknown items.
- Item drop tables/shops are vanilla-driven; to make monsters drop custom items or
  shops sell them, name them in the corresponding vanilla configs/mods.
