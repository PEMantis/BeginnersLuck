using System;
using System.Collections.Generic;
using System.Diagnostics;
using BeginnersLuck.Engine.Graphics;
using BeginnersLuck.Engine.Rendering;
using BeginnersLuck.Engine.Scenes;
using BeginnersLuck.Engine.UI;
using BeginnersLuck.Engine.Update;
using BeginnersLuck.Engine.World;
using BeginnersLuck.Game.Assets;
using BeginnersLuck.Game.Services;
using BeginnersLuck.Game.Graphics;
using BeginnersLuck.Game.State;
using BeginnersLuck.Game.UI;
using BeginnersLuck.Game.World;
using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Local;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BeginnersLuck.Game.Scenes;

public sealed class LocalMapScene : SceneBase
{
    private readonly GameServices _s;
    private readonly string _mapBinPath;
    private readonly LocalMapPurpose _purpose;
    private readonly SpawnRequest _spawn;

    private LocalMapData? _local;

    private readonly Camera2D _cam = new();
    private TileMap? _map;
    private TileSet? _tileset;
    private TileMapRenderer? _mapRenderer;

    private Point _playerCell;

    private KeyboardState _prevKs;
    private GamePadState _prevPad;

    private readonly CameraZoom.State _zoom = new() { MinZoom = 0.5f, MaxZoom = 4.0f, Step = 0.12f };
    private Point? _townCenter;

    private readonly EntityManager _entities = new();
    private readonly LocalMapCombatSystem _combat = new();
    private readonly SimpleEnemyAISystem _enemyAi = new();
    private readonly MessageLog _messageLog = new(capacity: 9);
    private readonly HashSet<string> _missingEntitySpriteLogged = new(StringComparer.OrdinalIgnoreCase);

    private Point _facing = new(0, 1);
    private bool _showInventory;
    private int _inventoryScroll;

    public LocalMapScene(GameServices s, string mapBinPath, LocalMapPurpose purpose, SpawnRequest spawn)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
        _mapBinPath = mapBinPath ?? throw new ArgumentNullException(nameof(mapBinPath));
        _purpose = purpose;
        _spawn = spawn;

        _playerCell = new Point(8, 8);
    }

    public override void Load(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _local = LocalMapBinLoader.Load(_mapBinPath);

        const int tileSize = 32;

        var tex = _s.Raw.LoadTexture("Textures/tiles.png");
        _tileset = new TileSet(tex, tileSize);
        _mapRenderer = new TileMapRenderer(_tileset);

        int n = _local.Size;
        var tiles = new int[n * n];

        for (int i = 0; i < tiles.Length; i++)
            tiles[i] = LocalTilePalette.ToTileIndex(_local.Terrain[i]);

        _map = new TileMap(n, n, tileSize, tiles);

        // Collision
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int i = x + y * n;

                var tid = _local.Terrain[i];
                var flags = _local.Flags[i];
                bool solid =
                    WorldTilePalette.IsSolid(tid) ||
                    (flags & TileFlags.Coast) != 0 ||
                    (flags & TileFlags.Cliff) != 0 ||
                    (flags & TileFlags.River) != 0;          // NEW: pillars block


                _map.SetSolidCell(x, y, solid);
            }

        var edgeReach = ComputeReachableFromEdge(_map);

        _playerCell = ResolveSpawnEscapable(_map, _local, _spawn, edgeReach);

        _townCenter = _local.TownCenter.HasValue
            ? new Point(_local.TownCenter.Value.X, _local.TownCenter.Value.Y)
            : (_purpose == LocalMapPurpose.Town ? ResolveFallbackTownCenter(_local, _map) : (Point?)null);

        if (_map.IsSolidCell(_playerCell.X, _playerCell.Y))
            _playerCell = FindNearestWalkableInMask(_map, _playerCell, edgeReach);

        _facing = new Point(0, 1);
        _showInventory = false;
        _inventoryScroll = 0;
        _entities.Clear();
        _messageLog.Add("Entered local area.");

        LocalEntitySpawner.SpawnDefaults(
            entities: _entities,
            map: _map,
            playerTile: _playerCell,
            townCenter: _townCenter,
            seed: _local.Seed ^ (_local.WorldX * 397) ^ _local.WorldY);

        _messageLog.Add("World Interaction v1 active.");

        _cam.Position = _map.CellToWorldCenter(_playerCell);
    }

    public override void Unload()
    {
        _entities.Clear();

        _local = null;
        _map = null;
        _tileset = null;
        _mapRenderer = null;
    }

    public override void Update(UpdateContext uc)
    {
        if (_map == null || _local == null) return;

        var ks = Keyboard.GetState();
        var pad = GamePad.GetState(PlayerIndex.One);


        // Debug: Sprite sheet inspector (always available)
        if (Pressed(ks, Keys.F12))
        {
            _s.Scenes.Push(new SpriteSheetInspectorScene(_s));
            _prevKs = ks;
            _prevPad = pad;
            return;
        }
        // Back without travel
        if (Pressed(ks, Keys.Escape) || Pressed(pad, Buttons.B))
        {
            _s.Scenes.Pop();
            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        // Movement
        Point dir = Point.Zero;

        if (Pressed(ks, Keys.W) || Pressed(ks, Keys.Up)) dir = new Point(0, -1);
        else if (Pressed(ks, Keys.S) || Pressed(ks, Keys.Down)) dir = new Point(0, 1);
        else if (Pressed(ks, Keys.A) || Pressed(ks, Keys.Left)) dir = new Point(-1, 0);
        else if (Pressed(ks, Keys.D) || Pressed(ks, Keys.Right)) dir = new Point(1, 0);

        if (dir == Point.Zero)
        {
            if (Pressed(pad, Buttons.DPadUp)) dir = new Point(0, -1);
            else if (Pressed(pad, Buttons.DPadDown)) dir = new Point(0, 1);
            else if (Pressed(pad, Buttons.DPadLeft)) dir = new Point(-1, 0);
            else if (Pressed(pad, Buttons.DPadRight)) dir = new Point(1, 0);
        }

        bool interactPressed =
            Pressed(pad, Buttons.A) ||
            Pressed(ks, Keys.Enter) ||
            Pressed(ks, Keys.Space) ||
            Pressed(ks, Keys.E);

        bool attackPressed = Pressed(ks, Keys.F) || Pressed(pad, Buttons.X);
        bool toggleInventoryPressed = Pressed(ks, Keys.I) || Pressed(ks, Keys.Tab) || Pressed(pad, Buttons.Y);

        if (toggleInventoryPressed)
        {
            _showInventory = !_showInventory;
            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        if (_showInventory)
        {
            if (Pressed(ks, Keys.Up) || Pressed(ks, Keys.W) || Pressed(pad, Buttons.DPadUp))
                _inventoryScroll = Math.Max(0, _inventoryScroll - 1);

            if (Pressed(ks, Keys.Down) || Pressed(ks, Keys.S) || Pressed(pad, Buttons.DPadDown))
                _inventoryScroll++;

            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        bool playerTurnConsumed = false;

        if (dir != Point.Zero)
        {
            _facing = dir;

            var next = _playerCell + dir;

            // Edge exit attempt
            if (IsOutOfBounds(next, _map.Width, _map.Height))
            {
                var exitDir = DirFromStep(dir);

                if (PortalAllowsExit(_local, exitDir))
                {
                    var exit = new LocalExitResult(
                        FromWorldX: _local.WorldX,
                        FromWorldY: _local.WorldY,
                        ExitDir: exitDir,
                        Purpose: _purpose,
                        LocalBinPath: _mapBinPath,
                        LocalExitCell: _playerCell
                    );

                    _s.World.Travel.PendingLocalExit = exit;

                    if (!_s.Fade.Active)
                        _s.Fade.Start(0.15f, () => _s.Scenes.Pop());
                    else
                        _s.Scenes.Pop();

                    _prevKs = ks;
                    _prevPad = pad;
                    return;
                }

                _s.Toasts.Push("No exit here.", 0.35f);
            }
            else if (!_map.IsSolidCell(next.X, next.Y) && !_entities.IsTileBlocked(next))
            {
                _playerCell = next;
                playerTurnConsumed = true;
            }
            else
            {
                _s.Toasts.Push("Blocked.", 0.35f);
            }
        }

        // Interaction targeting: current tile, facing tile, then adjacent fallback.
        if (interactPressed)
        {
            if (_townCenter.HasValue && _playerCell == _townCenter.Value)
            {
                _s.Scenes.Push(new TownScene(_s, new Point(_local.WorldX, _local.WorldY)));
                _prevKs = ks;
                _prevPad = pad;
                return;
            }

            if (InteractionSystem.TryInteract(
                entities: _entities,
                playerTile: _playerCell,
                facing: _facing,
                player: _s.Player,
                items: _s.Items,
                rng: _s.Rng,
                log: Log,
                out bool consumedInteractTurn))
            {
                playerTurnConsumed |= consumedInteractTurn;
            }
            else
            {
                Log("Nothing to interact with.");
            }
        }

        if (attackPressed)
        {
            if (_combat.TryPlayerAttack(_entities, _playerCell, _facing, _s.Player, _s.Items, _s.Rng, Log))
                playerTurnConsumed = true;
            else
                Log("No enemy in range.");
        }

#if DEBUG
        if (Pressed(ks, Keys.F8))
        {
            _s.Player.Inventory.AddItem("berries", 3, _s.Items);
            _s.Player.Inventory.AddItem("wood", 2, _s.Items);
            _s.Player.Inventory.AddItem("health_herb", 1, _s.Items);
            Log("DEBUG: Granted test item bundle.");
        }
#endif

        if (playerTurnConsumed)
        {
            _enemyAi.RunTurn(_entities, _map, _playerCell, _s.Player, _s.Rng, Log);

            if (_s.Player.Hp <= 0)
            {
                Log("You were defeated. Recovering...");
                _s.Player.HealToFull();
                _playerCell = ResolveSpawnEscapable(_map, _local, _spawn, ComputeReachableFromEdge(_map));
            }
        }

        _cam.Position = _map.CellToWorldCenter(_playerCell);

        CameraZoom.ApplyMouseWheel(_cam, _zoom, PixelRenderer.InternalWidth, PixelRenderer.InternalHeight);
        CameraZoom.ApplyBumpers(_cam, _zoom, pad, _prevPad, PixelRenderer.InternalWidth, PixelRenderer.InternalHeight);

        _prevKs = ks;
        _prevPad = pad;
    }

    protected override void DrawWorld(RenderContext rc)
    {
        if (_map == null || _mapRenderer == null || _local == null) return;

        var targetInfo = GetCurrentTargetInfo();

        var sb = rc.SpriteBatch;

        sb.Begin(
            samplerState: SamplerState.PointClamp,
            blendState: BlendState.AlphaBlend,
            transformMatrix: _cam.GetViewMatrix());

        var view = new Rectangle(
            (int)(_cam.Position.X - PixelRenderer.InternalWidth * 0.5f),
            (int)(_cam.Position.Y - PixelRenderer.InternalHeight * 0.5f),
            PixelRenderer.InternalWidth,
            PixelRenderer.InternalHeight);

        _mapRenderer.Draw(sb, _map, view);
        // TODO: Terrain Sprite Rendering v1 (disabled for now).
        // Cainos sheet source rectangles need proper atlas inspection before re-enabling.
        // Until then, terrain falls back to debug color rendering from TileMapRenderer.
        // if (UseAssetTerrainTiles) DrawTerrainSprites(sb, view);

        // Overlays (roads/rivers)
        int n = _local.Size;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int i = x + y * n;
                var f = _local.Flags[i];
                if (f == TileFlags.None) continue;

                var tl = _map.CellToWorldTopLeft(new Point(x, y));

                if ((f & TileFlags.Road) != 0)
                {
                    var mask = NeighborMask(_local, x, y, TileFlags.Road);

                    sb.Draw(_s.PixelWhite, new Rectangle((int)tl.X + 6, (int)tl.Y + 6, 4, 4), Color.SaddleBrown);

                    if (mask.HasFlag(NMask.North)) sb.Draw(_s.PixelWhite, new Rectangle((int)tl.X + 7, (int)tl.Y + 0, 2, 6), Color.SaddleBrown);
                    if (mask.HasFlag(NMask.South)) sb.Draw(_s.PixelWhite, new Rectangle((int)tl.X + 7, (int)tl.Y + 10, 2, 6), Color.SaddleBrown);
                    if (mask.HasFlag(NMask.West)) sb.Draw(_s.PixelWhite, new Rectangle((int)tl.X + 0, (int)tl.Y + 7, 6, 2), Color.SaddleBrown);
                    if (mask.HasFlag(NMask.East)) sb.Draw(_s.PixelWhite, new Rectangle((int)tl.X + 10, (int)tl.Y + 7, 6, 2), Color.SaddleBrown);
                }

                if ((f & TileFlags.River) != 0)
                {
                    var mask = NeighborMask(_local, x, y, TileFlags.River);

                    sb.Draw(_s.PixelWhite, new Rectangle((int)tl.X + 6, (int)tl.Y + 6, 4, 4), Color.CornflowerBlue);

                    if (mask.HasFlag(NMask.North)) sb.Draw(_s.PixelWhite, new Rectangle((int)tl.X + 6, (int)tl.Y + 0, 4, 6), Color.CornflowerBlue);
                    if (mask.HasFlag(NMask.South)) sb.Draw(_s.PixelWhite, new Rectangle((int)tl.X + 6, (int)tl.Y + 10, 4, 6), Color.CornflowerBlue);
                    if (mask.HasFlag(NMask.West)) sb.Draw(_s.PixelWhite, new Rectangle((int)tl.X + 0, (int)tl.Y + 6, 6, 4), Color.CornflowerBlue);
                    if (mask.HasFlag(NMask.East)) sb.Draw(_s.PixelWhite, new Rectangle((int)tl.X + 10, (int)tl.Y + 6, 6, 4), Color.CornflowerBlue);
                }
            }

        // Town center marker (readable plaza tile)
        if (_townCenter.HasValue)
        {
            var tc = _townCenter.Value;
            var tl = _map.CellToWorldTopLeft(tc);
            var r = new Rectangle((int)tl.X, (int)tl.Y, _map.TileSize, _map.TileSize);

            sb.Draw(_s.PixelWhite, new Rectangle(r.X + 3, r.Y + 3, r.Width - 6, r.Height - 6), new Color(22, 22, 35) * 0.85f);

            // border
            sb.Draw(_s.PixelWhite, new Rectangle(r.X, r.Y, r.Width, 1), Color.Gold * 0.65f);
            sb.Draw(_s.PixelWhite, new Rectangle(r.X, r.Y + r.Height - 1, r.Width, 1), Color.Gold * 0.65f);
            sb.Draw(_s.PixelWhite, new Rectangle(r.X, r.Y, 1, r.Height), Color.Gold * 0.65f);
            sb.Draw(_s.PixelWhite, new Rectangle(r.X + r.Width - 1, r.Y, 1, r.Height), Color.Gold * 0.65f);

            // center dot
            sb.Draw(_s.PixelWhite, new Rectangle(r.X + r.Width / 2 - 2, r.Y + r.Height / 2 - 2, 4, 4), Color.Gold * 0.95f);
        }

        DrawInteractionHighlight(sb, targetInfo);

        // Entities
        DrawEntities(sb, targetInfo);

        // Player marker
        var pos = _map.CellToWorldTopLeft(_playerCell);
        sb.Draw(_s.PixelWhite, new Rectangle((int)pos.X + 10, (int)pos.Y + 10, 12, 12), Color.Gold);

        sb.End();
    }

    protected override void DrawUI(RenderContext rc)
    {
        if (_local == null || _map == null) return;

        var targetInfo = GetCurrentTargetInfo();

        var sb = rc.SpriteBatch;
        sb.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);

        _s.UiFont.Draw(sb, $"LOCAL {_local.Size}x{_local.Size} ({_local.WorldX},{_local.WorldY})  {_purpose}",
            new Vector2(8, 8), Color.White * 0.9f, 1);

        _s.UiFont.Draw(sb, "ESC/B: back  |  E/A: interact  |  F/X: attack",
            new Vector2(8, 18), Color.White * 0.7f, 1);

        _s.UiFont.Draw(sb,
            $"P: {_playerCell.X},{_playerCell.Y} HP={_s.Player.Hp}/{_s.Player.MaxHp} entities={_entities.Entities.Count}",
            new Vector2(8, 28), Color.White * 0.85f, 1);

        var front = _playerCell + _facing;
        _s.UiFont.Draw(sb,
            $"Facing: {_facing.X},{_facing.Y} front={front.X},{front.Y}",
            new Vector2(8, 38), Color.White * 0.75f, 1);

        _s.UiFont.Draw(sb, "I/TAB/Y: Inventory", new Vector2(8, 48), Color.White * 0.68f, 1);

        var hpHud = new Rectangle(PixelRenderer.InternalWidth - 170, 8, 162, 40);
        sb.Draw(_s.PixelWhite, hpHud, new Color(10, 10, 18) * 0.78f);
        _s.UiFont.Draw(sb, $"HP: {_s.Player.Hp}/{_s.Player.MaxHp}", new Vector2(hpHud.X + 8, hpHud.Y + 8), Color.White * 0.95f, 1);
        _s.UiFont.Draw(sb, $"XP: {_s.Player.TotalXp}", new Vector2(hpHud.X + 8, hpHud.Y + 18), Color.White * 0.80f, 1);

        if (targetInfo.HasTarget)
        {
            var promptRect = new Rectangle(8, PixelRenderer.InternalHeight - 112, 360, 24);
            sb.Draw(_s.PixelWhite, promptRect, new Color(10, 10, 18) * 0.86f);

            var promptColor = targetInfo.IsAttackPrompt ? Color.OrangeRed * 0.95f : Color.Gold * 0.95f;
            _s.UiFont.Draw(sb, targetInfo.Prompt, new Vector2(promptRect.X + 8, promptRect.Y + 8), promptColor, 1);

            var nameRect = new Rectangle(promptRect.X, promptRect.Bottom + 2, 240, 20);
            sb.Draw(_s.PixelWhite, nameRect, new Color(10, 10, 18) * 0.72f);
            _s.UiFont.Draw(sb, $"Target: {targetInfo.Label}", new Vector2(nameRect.X + 8, nameRect.Y + 6), Color.White * 0.88f, 1);
        }

        var logArea = new Rectangle(8, PixelRenderer.InternalHeight - 86, PixelRenderer.InternalWidth - 16, 78);
        _messageLog.Draw(sb, _s.PixelWhite, _s.UiFont, logArea, scale: 1);

        if (_showInventory)
            DrawInventoryOverlay(sb);

        sb.End();
    }

    private void DrawInventoryOverlay(SpriteBatch sb)
    {
        var stacks = _s.Player.Inventory.GetAllStacks(_s.Items);

        var panel = new Rectangle(PixelRenderer.InternalWidth - 238, 54, 230, 212);
        sb.Draw(_s.PixelWhite, panel, new Color(10, 10, 18) * 0.92f);

        _s.UiFont.Draw(sb, "INVENTORY", new Vector2(panel.X + 8, panel.Y + 8), Color.Gold * 0.95f, 1);

        if (stacks.Count == 0)
        {
            _s.UiFont.Draw(sb, "Inventory is empty.", new Vector2(panel.X + 8, panel.Y + 28), Color.White * 0.75f, 1);
            return;
        }

        int rowH = _s.UiFont.LineHeight(1);
        int visibleRows = Math.Max(1, (panel.Height - 34) / rowH);
        int maxScroll = Math.Max(0, stacks.Count - visibleRows);
        _inventoryScroll = Math.Clamp(_inventoryScroll, 0, maxScroll);

        int y = panel.Y + 28;
        for (int i = 0; i < visibleRows; i++)
        {
            int idx = _inventoryScroll + i;
            if (idx >= stacks.Count)
                break;

            var stack = stacks[idx];
            string name = _s.Items.DisplayNameOf(stack.ItemId);
            var iconRect = new Rectangle(panel.X + 8, y + 1, 16, 16);
            string iconKey = _s.Items.IconIdOf(stack.ItemId);
            if (!_s.Sprites.TryDraw(sb, iconKey, iconRect, Color.White * 0.95f))
                SpriteDb.DrawMissingPlaceholder(sb, _s.PixelWhite, iconRect, Color.Magenta);

            _s.UiFont.Draw(sb, $"{name} x{stack.Quantity}", new Vector2(panel.X + 28, y), Color.White * 0.84f, 1);
            y += rowH;
        }
    }

    private static bool IsOutOfBounds(Point p, int w, int h)
        => p.X < 0 || p.Y < 0 || p.X >= w || p.Y >= h;

    private static Dir DirFromStep(Point step)
    {
        if (step.X == 1) return Dir.East;
        if (step.X == -1) return Dir.West;
        if (step.Y == 1) return Dir.South;
        return Dir.North;
    }

    private static bool PortalAllowsExit(LocalMapData local, Dir d) => true;

    // TODO: Terrain Sprite Rendering v1 (disabled, see DrawWorld comment).
    // private const bool UseAssetTerrainTiles = false;
    // Terrain sprite rendering would call DrawTerrainSprites(sb, view) when re-enabled,
    // but source rectangles need proper inspection first.

    private void DrawEntities(SpriteBatch sb, InteractionTargetInfo targetInfo)
    {
        if (_map == null) return;

        for (int i = 0; i < _entities.Entities.Count; i++)
        {
            var e = _entities.Entities[i];
            if (!e.IsAlive) continue;

            var tl = _map.CellToWorldTopLeft(e.Tile);
            var rect = new Rectangle((int)tl.X + 8, (int)tl.Y + 8, 16, 16);

            switch (e.Type)
            {
                case GameEntityType.ResourceNode:
                    rect = new Rectangle((int)tl.X + 9, (int)tl.Y + 9, 14, 14);
                    break;
                case GameEntityType.Chest:
                    rect = new Rectangle((int)tl.X + 7, (int)tl.Y + 11, 18, 12);
                    break;
                case GameEntityType.Door:
                    rect = new Rectangle((int)tl.X + 10, (int)tl.Y + 6, 12, 20);
                    break;
                case GameEntityType.Enemy:
                    rect = new Rectangle((int)tl.X + 8, (int)tl.Y + 8, 16, 16);
                    break;
            }

            bool drewSprite = false;
            if (!string.IsNullOrWhiteSpace(e.SpriteId))
            {
                if (_s.Sprites.TryResolve(e.SpriteId, out var sprite))
                {
                    sb.Draw(sprite.Texture, rect, sprite.Source, Color.White, 0f, sprite.Origin, SpriteEffects.None, 0f);
                    drewSprite = true;
                }
                else
                {
                    LogMissingEntitySprite(e);
                }
            }

            // Always draw a visible fallback if sprite failed or was not set.
            if (!drewSprite)
            {
                var fallbackColor = ComputeEntityFallbackColor(e);
                sb.Draw(_s.PixelWhite, rect, fallbackColor);
            }

            if (e.Type == GameEntityType.Enemy && IsEnemyAggro(e))
            {
                var aggro = new Rectangle(rect.X + (rect.Width / 2) - 2, rect.Y - 8, 4, 4);
                sb.Draw(_s.PixelWhite, aggro, Color.OrangeRed * 0.95f);
            }

            if (targetInfo.HighlightEntity != null && targetInfo.HighlightEntity.Id == e.Id)
            {
                var focusRect = new Rectangle(rect.X - 1, rect.Y - 1, rect.Width + 2, rect.Height + 2);
                sb.Draw(_s.PixelWhite, new Rectangle(focusRect.X, focusRect.Y, focusRect.Width, 1), Color.Gold * 0.9f);
                sb.Draw(_s.PixelWhite, new Rectangle(focusRect.X, focusRect.Bottom - 1, focusRect.Width, 1), Color.Gold * 0.9f);
                sb.Draw(_s.PixelWhite, new Rectangle(focusRect.X, focusRect.Y, 1, focusRect.Height), Color.Gold * 0.9f);
                sb.Draw(_s.PixelWhite, new Rectangle(focusRect.Right - 1, focusRect.Y, 1, focusRect.Height), Color.Gold * 0.9f);
            }

            if (e.Type == GameEntityType.Enemy && e.MaxHp > 0)
            {
                int barW = 16;
                int hpW = (int)MathF.Round(barW * Math.Clamp(e.Hp / (float)e.MaxHp, 0f, 1f));
                sb.Draw(_s.PixelWhite, new Rectangle(rect.X, rect.Y - 3, barW, 2), Color.Black * 0.8f);
                sb.Draw(_s.PixelWhite, new Rectangle(rect.X, rect.Y - 3, Math.Max(0, hpW), 2), Color.LimeGreen);
            }
        }
    }

    private void DrawInteractionHighlight(SpriteBatch sb, InteractionTargetInfo targetInfo)
    {
        if (!targetInfo.HasTarget || _map == null)
            return;

        var tl = _map.CellToWorldTopLeft(targetInfo.HighlightTile);
        var tile = new Rectangle((int)tl.X, (int)tl.Y, _map.TileSize, _map.TileSize);

        var color = targetInfo.IsAttackPrompt ? Color.OrangeRed : Color.Gold;
        sb.Draw(_s.PixelWhite, tile, color * 0.18f);
        sb.Draw(_s.PixelWhite, new Rectangle(tile.X, tile.Y, tile.Width, 1), color * 0.95f);
        sb.Draw(_s.PixelWhite, new Rectangle(tile.X, tile.Bottom - 1, tile.Width, 1), color * 0.95f);
        sb.Draw(_s.PixelWhite, new Rectangle(tile.X, tile.Y, 1, tile.Height), color * 0.95f);
        sb.Draw(_s.PixelWhite, new Rectangle(tile.Right - 1, tile.Y, 1, tile.Height), color * 0.95f);
    }

    private InteractionTargetInfo GetCurrentTargetInfo()
        => InteractionPromptBuilder.Build(_entities, _playerCell, _facing);

    private bool IsEnemyAggro(GameEntity enemy)
    {
        if (enemy.Type != GameEntityType.Enemy || !enemy.IsAlive)
            return false;

        int dist = Math.Abs(enemy.Tile.X - _playerCell.X) + Math.Abs(enemy.Tile.Y - _playerCell.Y);
        return dist <= _enemyAi.AggroRange;
    }

    private void Log(string message)
    {
        _messageLog.Add(message);
        _s.Toasts.Push(message, 0.9f);
    }

    private void LogMissingEntitySprite(GameEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.SpriteId))
            return;

        if (!_missingEntitySpriteLogged.Add(entity.SpriteId))
            return;

        var message = $"[Assets] entity sprite missing: {entity.DisplayName} | {entity.Type} | {entity.SpriteId}";
        Debug.WriteLine(message);
        Console.WriteLine(message);
    }

    private static Color ComputeEntityFallbackColor(GameEntity entity)
    {
        return entity.Type switch
        {
            GameEntityType.ResourceNode => new Color(160, 220, 100), // greenish
            GameEntityType.Chest => new Color(180, 140, 80),         // brownish gold
            GameEntityType.Enemy => new Color(220, 80, 100),         // reddish
            GameEntityType.Door => new Color(140, 140, 160),         // grayish
            _ => new Color(120, 100, 80),                             // dark brown
        };
    }

    private bool Pressed(KeyboardState ks, Keys k) => ks.IsKeyDown(k) && !_prevKs.IsKeyDown(k);
    private bool Pressed(GamePadState pad, Buttons b) => pad.IsButtonDown(b) && !_prevPad.IsButtonDown(b);

    private static bool[] ComputeReachableFromEdge(TileMap map)
    {
        int w = map.Width;
        int h = map.Height;
        var reach = new bool[w * h];
        var q = new Queue<Point>();

        void EnqueueIfWalkable(int x, int y)
        {
            if ((uint)x >= (uint)w || (uint)y >= (uint)h) return;
            int i = map.Index(x, y);
            if (reach[i]) return;
            if (map.IsSolidCell(x, y)) return;

            reach[i] = true;
            q.Enqueue(new Point(x, y));
        }

        for (int x = 0; x < w; x++)
        {
            EnqueueIfWalkable(x, 0);
            EnqueueIfWalkable(x, h - 1);
        }
        for (int y = 0; y < h; y++)
        {
            EnqueueIfWalkable(0, y);
            EnqueueIfWalkable(w - 1, y);
        }

        while (q.Count > 0)
        {
            var p = q.Dequeue();
            EnqueueIfWalkable(p.X + 1, p.Y);
            EnqueueIfWalkable(p.X - 1, p.Y);
            EnqueueIfWalkable(p.X, p.Y + 1);
            EnqueueIfWalkable(p.X, p.Y - 1);
        }

        return reach;
    }

    private static Point ResolveSpawnEscapable(TileMap map, LocalMapData local, SpawnRequest spawn, bool[] edgeReach)
    {
        int n = local.Size;

        bool HasRoad(int x, int y)
            => (local.Flags[x + y * n] & TileFlags.Road) != 0;

        Point EdgeSeed(Dir dir)
        {
            var p = local.Portals;
            int clamp(int v) => Math.Clamp(v, 1, n - 2);

            return dir switch
            {
                Dir.West => new Point(1, clamp(p.RoadWPos)),
                Dir.East => new Point(n - 2, clamp(p.RoadEPos)),
                Dir.North => new Point(clamp(p.RoadNPos), 1),
                Dir.South => new Point(clamp(p.RoadSPos), n - 2),
                _ => new Point(n / 2, n / 2),
            };
        }

        Point start = spawn.Intent switch
        {
            SpawnIntent.EnterFromRoad when spawn.IncomingDir.HasValue => EdgeSeed(spawn.IncomingDir.Value),
            _ => new Point(n / 2, n / 2)
        };

        bool Ok(int x, int y, bool requireRoad)
        {
            if ((uint)x >= (uint)n || (uint)y >= (uint)n) return false;
            if (map.IsSolidCell(x, y)) return false;
            if (!edgeReach[map.Index(x, y)]) return false;
            if (requireRoad && !HasRoad(x, y)) return false;
            return true;
        }

        Point? FindNearest(Point s, bool requireRoad)
        {
            if (Ok(s.X, s.Y, requireRoad)) return s;

            for (int r = 1; r <= 160; r++)
            {
                int minX = s.X - r, maxX = s.X + r;
                int minY = s.Y - r, maxY = s.Y + r;

                for (int x = minX; x <= maxX; x++)
                {
                    if (Ok(x, minY, requireRoad)) return new Point(x, minY);
                    if (Ok(x, maxY, requireRoad)) return new Point(x, maxY);
                }

                for (int y = minY; y <= maxY; y++)
                {
                    if (Ok(minX, y, requireRoad)) return new Point(minX, y);
                    if (Ok(maxX, y, requireRoad)) return new Point(maxX, y);
                }
            }

            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    if (Ok(x, y, requireRoad))
                        return new Point(x, y);

            return null;
        }

        if (spawn.Intent == SpawnIntent.EnterFromRoad)
        {
            var road = FindNearest(start, requireRoad: true);
            if (road.HasValue) return road.Value;
        }

        return FindNearest(start, requireRoad: false) ?? start;
    }

    private static Point FindNearestWalkableInMask(TileMap map, Point start, bool[] mask)
    {
        for (int r = 0; r < 96; r++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                int dx = r - Math.Abs(dy);

                var a = new Point(start.X + dx, start.Y + dy);
                if ((uint)a.X < (uint)map.Width && (uint)a.Y < (uint)map.Height)
                {
                    int i = map.Index(a.X, a.Y);
                    if (mask[i] && !map.IsSolidCell(a.X, a.Y)) return a;
                }

                var b = new Point(start.X - dx, start.Y + dy);
                if ((uint)b.X < (uint)map.Width && (uint)b.Y < (uint)map.Height)
                {
                    int i = map.Index(b.X, b.Y);
                    if (mask[i] && !map.IsSolidCell(b.X, b.Y)) return b;
                }
            }
        }

        return FindNearestWalkableAnywhere(map, start);
    }

    private static Point FindNearestWalkableAnywhere(TileMap map, Point start)
    {
        for (int r = 0; r < 96; r++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                int dx = r - Math.Abs(dy);

                var a = new Point(start.X + dx, start.Y + dy);
                if (!map.IsSolidCell(a.X, a.Y)) return a;

                var b = new Point(start.X - dx, start.Y + dy);
                if (!map.IsSolidCell(b.X, b.Y)) return b;
            }
        }
        return start;
    }

    [Flags]
    private enum NMask
    {
        None = 0,
        North = 1 << 0,
        East = 1 << 1,
        South = 1 << 2,
        West = 1 << 3
    }

    private static NMask NeighborMask(LocalMapData local, int x, int y, TileFlags flag)
    {
        int n = local.Size;
        int idx(int xx, int yy) => xx + yy * n;

        NMask m = NMask.None;

        if (y > 0 && (local.Flags[idx(x, y - 1)] & flag) != 0) m |= NMask.North;
        if (x < n - 1 && (local.Flags[idx(x + 1, y)] & flag) != 0) m |= NMask.East;
        if (y < n - 1 && (local.Flags[idx(x, y + 1)] & flag) != 0) m |= NMask.South;
        if (x > 0 && (local.Flags[idx(x - 1, y)] & flag) != 0) m |= NMask.West;

        return m;
    }

    private static Point ResolveFallbackTownCenter(LocalMapData local, TileMap map)
    {
        int n = local.Size;
        var center = new Point(n / 2, n / 2);

        for (int r = 0; r < 64; r++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                int dx = r - Math.Abs(dy);

                if (Try(center.X + dx, center.Y + dy, out var p)) return p;
                if (Try(center.X - dx, center.Y + dy, out p)) return p;
            }
        }

        for (int y = 1; y < n - 1; y++)
            for (int x = 1; x < n - 1; x++)
                if (!map.IsSolidCell(x, y))
                    return new Point(x, y);

        return center;

        bool Try(int x, int y, out Point p)
        {
            p = default;
            if ((uint)x >= (uint)n || (uint)y >= (uint)n) return false;
            if (map.IsSolidCell(x, y)) return false;

            int i = x + y * n;
            if ((local.Flags[i] & TileFlags.Road) == 0) return false;

            p = new Point(x, y);
            return true;
        }
    }
}
