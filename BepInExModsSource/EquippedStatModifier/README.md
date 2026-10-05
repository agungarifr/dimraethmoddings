# Equipped Stat Modifier (Standalone & DimraethModPack Ready)

A live, in-game equipment secondary stat editor for **Dimraeth**.
Detects all currently equipped gear items (Sash/Belt, Undercoat, Charm, Rings, Amulets, Badges, Bracers, Rune), and allows modifying both **which secondary stat** (affix type) is on the item and its **numeric magnitude** in real time.

---

## Features

- **Live Equipment Detection**:
  Directly scans the player's 12 equipped slots (`player.EquipmentWorn`) in raw memory.
- **Full Stat Catalog**:
  Pick from any of the ~70 game stat affixes (`BleedDuration`, `HealthRegen`, `Armor`, `AttackDamage`, `CriticalChance`, etc.) via an interactive, searchable picker.
- **Value Modification**:
  Type exact numbers (e.g. `10`, `100`, `25.5`) or use quick steppers (`-10`, `-1`, `+1`, `+10`).
- **Affix Management**:
  Add new secondary stat affixes (up to the maximum cap) or delete unwanted affixes.
- **Real-time Recalculation**:
  Recalculates character stat bonuses immediately (`player.Runes.UpdateRuneBonuses()`) so stat changes take effect the moment you click **Apply**.
- **HMAC Anti-Cheat Safe**:
  Automatically re-signs the edited rune HMAC and re-baselines `_runeSignatureStore` so the game's anti-cheat integrity check accepts the changes.
- **Save Migration Safe**:
  Enforces `SaveSystem.CurrentRuneFormulaVersion` so the game's save-time migration does not overwrite custom magnitudes.
- **Instant Save**:
  Includes a one-click **Save Character** button that triggers native `player.SaveGame()` to commit your edits to the `.jrf` character file.

---

## How to Use (In-Game)

1. Load into your character in-game.
2. Press **`F8`** (configurable) to toggle the **Equipped Stat Modifier** window.
3. Select an equipped slot from the left list (e.g., **`Slot 2: Belt (Sash)`**).
4. For any secondary stat affix:
   - Click the stat name button (`▼ BleedDuration`) to open the search picker and change which stat is equipped.
   - Adjust the number using the text box or the `-10` / `-1` / `+1` / `+10` stepper buttons.
   - Use `+ Add Secondary Stat` to add more affixes or `✕` to delete an affix.
5. Click **`✓ Apply to Item`** to write the modifications directly into memory and recalculate your bonuses.
6. Click **`💾 Save Character`** to save the changes to your local character file immediately.

---

## Standalone Configuration (`BepInEx/config/com.dimraeth.equippedstatmodifier.cfg`)

| Key | Default | Description |
|---|---|---|
| `ModEnabled` | `true` | Master toggle for the mod |
| `MenuKey` | `F8` | Keyboard shortcut to toggle the editor UI |

---

## DimraethModPack Integration

This mod is designed to be fully compatible with and integrated into **DimraethModPack**:
- An integrated module `EquippedStatModifierModule` is included in `DimraethModPack.Modules.Stats`.
- Users can open the editor either via the shortcut **`F8`** or by clicking **`▶ Open Stat Editor`** in the DimraethModPack menu (`~` / BackQuote).

---

## Building

```bash
modding\dotnet-sdk\dotnet.exe build modding\BepInExModsSource\EquippedStatModifier\EquippedStatModifier.csproj -c Release
```

The output `EquippedStatModifier.dll` is automatically deployed to `BepInEx/plugins/`.
