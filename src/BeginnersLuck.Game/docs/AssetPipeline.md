# Asset Pipeline

Beginner's Luck loads runtime art from `Content/Art` through the raw content helper. The legacy `ContentRaw` tree is still available as a fallback for older world-map overlays and other holdovers, but the manifest-backed art pack is now the primary path.

## Layout

- `Content/Art/Manifests/asset-manifest.json` declares stable sprite keys.
- `Content/Art/Packs/CainosTopDownBasic/` holds entity and world props art.
- `Content/Art/Packs/PixelCrawlerFree/Body_A/` holds the starter player sheets.
- `Content/Art/Packs/ItemIcons/Curated/` holds the curated starter item icons.
- `SpriteDb` resolves stable keys to file paths or sheet regions and caches loaded textures.

## Usage

- Prefer stable keys from `BeginnersLuck.Game.Assets.AssetKeys`.
- `ItemDb.IconId` should point at `AssetKeys.Items.*`.
- `GameEntity.SpriteId` should point at `AssetKeys.Entities.*`.
- Replace an icon by editing the manifest path or region, not by changing gameplay code.
- If a sprite is missing, the registry logs it once and draws a placeholder instead of crashing.

## Current Scope

This pass only wires the starter assets needed for the current game loop. It intentionally leaves broader terrain, animation, and full item-icon remapping for later.

## How to Choose Sprite Regions

When adding a new entity, prop, or tile sprite from a Cainos sheet:

1. **Open the Sprite Sheet Inspector**
   - While in the local map, press `F12` (debug mode only)
   - The inspector window shows available sprite sheets from the Cainos pack

2. **Select and Navigate the Sheet**
   - Use `PageUp` / `PageDown` to cycle between sheets
   - Use arrow keys to pan around the sheet
   - Use `+` / `-` keys or `R` / `T` / `Y` to zoom and adjust grid size

3. **Find Your Sprite**
   - Grid size defaults to 32x32 (standard for Cainos)
   - Change grid with:
     - `R` = 16x16 grid
     - `T` = 32x32 grid (default)
     - `Y` = 64x64 grid
   - Hover over a sprite cell to see coordinates

4. **Copy the Source Rectangle**
   - Click on or press Enter/Space over a sprite cell
   - A manifest-ready JSON snippet prints to the console
   - Example output:
     ```json
     {
       "key": "TODO.Props.2_1",
       "path": "Art/Packs/CainosTopDownBasic/TX Props.png",
       "x": 64,
       "y": 32,
       "width": 32,
       "height": 32,
       "scale": 1
     }
     ```

5. **Update the Manifest**
   - Open `Content/Art/Manifests/asset-manifest.json`
   - Paste the snippet (remove the trailing comma on the last entry)
   - Update the `"key"` to a meaningful identifier like `"entity.prop.mushroom"`
   - Verify the path matches the sheet filename
   - Restart the game to reload assets

6. **Wire the Entity**
   - Add the key constant to `BeginnersLuck.Game.Assets.AssetKeys`
   - Update the spawner or entity class to use the new constant
   - If the sprite fails to load, it will fall back to a color placeholder

**Note:** If a grid cell's sprite extends beyond 32x32, measure carefully. The inspector shows each cell's bounds; adjust `width` and `height` as needed.
