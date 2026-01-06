using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BeginnersLuck.Engine.Graphics;
using BeginnersLuck.Engine.Rendering;
using BeginnersLuck.Engine.Scenes;
using BeginnersLuck.Engine.UI;
using BeginnersLuck.Engine.Update;
using BeginnersLuck.Engine.World;
using BeginnersLuck.Game.Encounters;
using BeginnersLuck.Game.Services;
using BeginnersLuck.Game.State;
using BeginnersLuck.Game.World;
using BeginnersLuck.WorldGen;
using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Local;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BeginnersLuck.Game.Scenes;

public sealed class WorldMapScene : SceneBase
{
    private readonly GameServices _s;

    private Texture2D? _white;
    private readonly Camera2D _cam = new();

    private TileMap? _map;
    private TileSet? _tileset;
    private TileMapRenderer? _mapRenderer;

    private Point _playerCell = new(2, 2);

    private KeyboardState _prevKs;
    private GamePadState _prevPad;

    // Encounter toast
    private bool _toastActive;
    private float _toastT;
    private readonly float _toastDuration = 0.55f;
    private EncounterDef? _toastEncounter;
    private KeyboardState _toastSeedKs;
    private GamePadState _toastSeedPad;

    // Loaded world DTO
    private WorldDto? _world;

    // Flattened arrays at WORLD resolution
    private byte[]? _terrainFlat;
    private ushort[]? _flagsFlat;

    // Actual WorldGen runtime world (needed for local generation)
    private BeginnersLuck.WorldGen.WorldMap? _worldMap;

    private Dir _lastWorldMoveDir = Dir.North;
    private readonly CameraZoom.State _zoom = new() { MinZoom = 0.5f, MaxZoom = 3.0f, Step = 0.12f };
    private float _lastEncounterChance;
    private ZoneInfo _lastZone;
    private int _debugMoves;
    private int _debugRolls;
    private int _debugStarts;
    private double _debugLastRoll;
    private bool _debugTriedThisStep;

    // ---- Props overlay (SpriteDb, no atlas) ----
    private readonly Dictionary<Point, List<WorldProp>> _propChunks = new();
    private readonly HashSet<Point> _propBlocked = new(); // collision set (tile coords)
    private const int PropChunkSizeTiles = 32;

    public WorldMapScene(GameServices s)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
    }

    public override void Load(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _white = new Texture2D(graphicsDevice, 1, 1);
        _white.SetData(new[] { Color.White });

        _propChunks.Clear();
        _propBlocked.Clear();
       

        int seed = _s.World.WorldSeed;
        string worldJson = WorldPaths.WorldJsonPath(seed);

        if (!File.Exists(worldJson))
            throw new FileNotFoundException($"world.json not found: {worldJson}");

        var json = File.ReadAllText(worldJson);
        _world = JsonSerializer.Deserialize<WorldDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to deserialize world.json.");

        _world.Validate();

        int w = _world.Width;
        int h = _world.Height;
        int cs = _world.ChunkSize;

        _terrainFlat = new byte[w * h];
        _flagsFlat = new ushort[w * h];

        foreach (var ch in _world.Chunks)
        {
            int baseX = ch.Cx * cs;
            int baseY = ch.Cy * cs;

            for (int ly = 0; ly < cs; ly++)
            for (int lx = 0; lx < cs; lx++)
            {
                int local = lx + ly * cs;
                int wx = baseX + lx;
                int wy = baseY + ly;

                if ((uint)wx >= (uint)w || (uint)wy >= (uint)h)
                    continue;

                int i = wx + wy * w;

                _terrainFlat[i] = (byte)ch.Terrain[local];
                _flagsFlat[i] = (ushort)ch.Flags[local];
                }
        }

        // After flattening arrays (terrainFlat + flagsFlat) and before building TileMap:
        WorldPoiPass.Apply(w, h, _terrainFlat!, _flagsFlat!, _s.Rng);

        const int tileSize = 32;
        _map = new TileMap(w, h, tileSize, new int[w * h]);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = x + y * w;
                var tid = (TileId)_terrainFlat[i];
                var flags = (TileFlags)_flagsFlat[i];

                _map.Tiles[i] = WorldTilePalette.ToTileIndex(tid);

                bool solid = WorldCollisionResolver.IsSolid(tid, flags);
                _map.SetSolidCell(x, y, solid);
            }

        var tex = _s.Raw.LoadTexture("Textures/tiles.png");
        _tileset = new TileSet(tex, tileSize);
        _mapRenderer = new TileMapRenderer(_tileset);

        _worldMap = BuildWorldMap(_world);

        _playerCell = new Point(w / 2, h / 2);

        int minRegion = Math.Max(800, (w * h) / 50);
        int maxR = Math.Max(w, h);

        _playerCell = WorldSpawnResolver.FindPlayableSpawn(
            map: _map!,
            preferred: _playerCell,
            maxSearchRadius: maxR,
            minRegionSize: minRegion,
            requireTouchesEdge: true);


        _cam.Position = _map.CellToWorldCenter(_playerCell);
    }

    public override void Unload()
    {
        _white?.Dispose();
        _white = null;

        _tileset = null;
        _mapRenderer = null;
        _map = null;

        _world = null;
        _terrainFlat = null;
        _flagsFlat = null;
        _worldMap = null;

        _propChunks.Clear();
        _propBlocked.Clear();
    }

    public override void Update(UpdateContext uc)
    {
        if (_map == null) return;

        ConsumePendingLocalExit();

        var ks = Keyboard.GetState();
        var pad = GamePad.GetState(PlayerIndex.One);
        _debugTriedThisStep = false;

        // Encounter toast active: tick + block input
        if (_toastActive)
        {
            _toastT += (float)uc.GameTime.ElapsedGameTime.TotalSeconds;

            if (_toastT >= _toastDuration && !_s.Fade.Active)
            {
                _toastActive = false;
                if (_toastEncounter != null)
                {
                    _s.Fade.Start(0.25f, () =>
                    {
                        _s.Scenes.Push(new BattleScene(_s, _toastEncounter, _toastSeedKs, _toastSeedPad));
                    });
                }
            }

            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        // Pause
        if (Pressed(ks, Keys.Escape) || Pressed(pad, Buttons.Start) || Pressed(pad, Buttons.Back))
        {
            _s.Scenes.Push(new PauseScene(_s));
            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        // Hub/Menu
        if (Pressed(ks, Keys.Tab) || Pressed(pad, Buttons.Y))
        {
            _s.Scenes.Push(new MenuHubScene(_s, ks, startTab: 0));
            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        // Movement (edge-triggered)
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

        bool moved = false;

        if (dir != Point.Zero)
        {
            var next = _playerCell + dir;

            if ((uint)next.X < (uint)_map.Width && (uint)next.Y < (uint)_map.Height)
            {
                if (!IsBlocked(next))
                {
                    _playerCell = next;
                    moved = true;
                    _debugMoves++;
                    _debugTriedThisStep = true;

                    int idx = _playerCell.X + _playerCell.Y * _world!.Width;
                    var tid = (TileId)_terrainFlat![idx];
                    var flags = (TileFlags)_flagsFlat![idx];

                    _lastZone = ZoneResolver.ResolveFrom(tid, flags);
                    _lastEncounterChance = _s.EncounterDirector.ComputeChancePerStep(_lastZone);

                    _debugRolls++;
                    _debugLastRoll = _s.Rng.NextDouble();

                    // Encounters: only roll when a move actually happened
                   var intent = _s.EncounterDirector.OnPlayerMoved(_playerCell, _lastZone, _s.Rng);

                    if (intent.HasValue)
                    {
                        StartEncounterToast(intent.Value.Encounter, ks, pad);

                        _prevKs = ks;
                        _prevPad = pad;
                        return;
                    }

                    if (dir.X == 1) _lastWorldMoveDir = Dir.East;
                    else if (dir.X == -1) _lastWorldMoveDir = Dir.West;
                    else if (dir.Y == 1) _lastWorldMoveDir = Dir.South;
                    else if (dir.Y == -1) _lastWorldMoveDir = Dir.North;
                }
                else
                {
                    _s.Toasts.Push("Blocked.", 0.35f);
                }

            }
        }

        // DEV: spawn a Ruins POI near the player (F6 / LeftShoulder)
        if (Pressed(ks, Keys.F6) || Pressed(pad, Buttons.LeftShoulder))
        {
            if (!DevSpawnRuinsNearPlayer(maxRadius: 14))
                _s.Toasts.Push("DEV: Couldn't place ruins near player.", 1.0f);

            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        // DEV: stamp ROAD forward from player (F7 / LeftTrigger) (optional if you added it)
        if (Pressed(ks, Keys.F7) || Pressed(pad, Buttons.LeftTrigger))
        {
            StampRoadFromPlayer(length: 12);

            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        // Enter / Interact (E / A)
        if (Pressed(ks, Keys.E) || Pressed(pad, Buttons.A))
        {
            if (_world == null || _flagsFlat == null || _worldMap == null || _map == null)
            {
                _s.Toasts.Push("World not loaded.", 1.2f);
                _prevKs = ks; _prevPad = pad;
                return;
            }

            int wx = _playerCell.X;
            int wy = _playerCell.Y;

            int idx = wx + wy * _world.Width;
            var flags = (TileFlags)_flagsFlat[idx];

            var purpose = WorldTilePalette.PurposeFromFlags(_flagsFlat[idx]);

            if ((flags & TileFlags.Ruins) != 0)
                _s.Toasts.Push("Entering Ruins...", 0.6f);
            else if ((flags & TileFlags.Town) != 0)
                _s.Toasts.Push("Entering Town...", 0.6f);
            else if ((flags & TileFlags.Road) != 0)
                _s.Toasts.Push("Following Road...", 0.6f);

            int seed = _s.World.WorldSeed;

            string localBin = LocalMapCache.EnsureLocalExists(
                world: _worldMap,
                worldSeed: seed,
                wx: wx,
                wy: wy,
                localSize: 128,
                purpose: purpose
            );

            var spawn = new SpawnRequest(SpawnIntent.EnterFromRoad, IncomingDir: Opposite(_lastWorldMoveDir));

            if (!_s.Fade.Active)
                _s.Fade.Start(0.25f, () => _s.Scenes.Push(new LocalMapScene(_s, localBin, purpose, spawn)));

            _prevKs = ks;
            _prevPad = pad;
            return;
        }



        CameraZoom.ApplyMouseWheel(_cam, _zoom, PixelRenderer.InternalWidth, PixelRenderer.InternalHeight);
        CameraZoom.ApplyBumpers(_cam, _zoom, pad, _prevPad, PixelRenderer.InternalWidth, PixelRenderer.InternalHeight);

        _cam.Position = _map.CellToWorldCenter(_playerCell);

        // Ensure visible area caches are ready (nice for immediate draw)
        EnsurePropChunkForCell(_playerCell);

        _prevKs = ks;
        _prevPad = pad;
    }

    private void ConsumePendingLocalExit()
    {
        var pending = _s.World.Travel.PendingLocalExit;
        if (pending == null) return;

        _s.World.Travel.PendingLocalExit = null;

        var from = new Point(pending.FromWorldX, pending.FromWorldY);

        var step = pending.ExitDir switch
        {
            Dir.North => new Point(0, -1),
            Dir.South => new Point(0, 1),
            Dir.East => new Point(1, 0),
            Dir.West => new Point(-1, 0),
            _ => Point.Zero
        };

        var dest = from + step;

        if (_map != null)
        {
            EnsurePropChunkForCell(dest);

            if ((uint)dest.X >= (uint)_map.Width || (uint)dest.Y >= (uint)_map.Height || IsBlocked(dest))
                dest = from;
        }

        _playerCell = dest;
        _lastWorldMoveDir = pending.ExitDir;

        if (_map != null)
            _cam.Position = _map.CellToWorldCenter(_playerCell);

        _s.Toasts.Push($"Exit {pending.ExitDir} -> WORLD {_playerCell.X},{_playerCell.Y}", 0.6f);
    }

    protected override void DrawWorld(RenderContext rc)
    {
        if (_map == null || _mapRenderer == null || _white == null) return;

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

        // Roads and similar tile-driven overlays
        DrawWorldRoads(sb, view);

        // props first (behind POI markers)
        DrawWorldProps(sb, view);

        // POIs next so they stand out over props
        DrawWorldPois(sb, view);

        // player last
        DrawWorldPlayer(sb);

        sb.End();
    }

    protected override void DrawUI(RenderContext rc)
    {
        if (_white == null) return;

        var sb = rc.SpriteBatch;
        sb.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);

        var hud = new Rectangle(8, 8, 300, 64);
        sb.Draw(_white, hud, new Color(10, 10, 18) * 0.75f);

        _s.UiFont.Draw(sb, $"GOLD: {_s.Player.Gold}", new Vector2(hud.X + 8, hud.Y + 8), Color.White * 0.9f, 1);
        _s.UiFont.Draw(sb, $"XP:   {_s.Player.TotalXp}", new Vector2(hud.X + 8, hud.Y + 18), Color.White * 0.9f, 1);

        if (_world != null && _terrainFlat != null && _flagsFlat != null && _map != null)
        {
            int idx = _playerCell.X + _playerCell.Y * _world.Width;
            var tid = (TileId)_terrainFlat[idx];
            bool propBlocked = _propBlocked.Contains(_playerCell);
            

            _s.UiFont.Draw(sb,
                $"WORLD: {_playerCell.X},{_playerCell.Y} {tid} flags={_flagsFlat[idx]} solid={_map.IsSolidCell(_playerCell.X,_playerCell.Y)} propBlock={propBlocked}",
                new Vector2(hud.X + 8, hud.Y + 32),
                Color.White * 0.75f, 1);
        }

        if(_lastZone != null)
        _s.UiFont.Draw(sb,
            $"ZONE: {_lastZone.Id} danger={_lastZone.Danger} table={_lastZone.EncounterTableId} chance={_lastEncounterChance:P0}",
            new Vector2(hud.X + 8, hud.Y + 44),
            Color.White * 0.7f, 1);

        var poi = CurrentPoiType();
        if (poi != PoiType.None)
        {
            // A simple bottom-left tooltip box
            var r = new Rectangle(8, PixelRenderer.InternalHeight - 34, 420, 26);
            sb.Draw(_white!, r, new Color(10, 10, 18) * 0.78f);

            string label = WorldPoiResolver.DisplayName(poi);
            string prompt = WorldPoiResolver.Prompt(poi);

            _s.UiFont.Draw(sb, $"{label}  -  {prompt}", new Vector2(r.X + 8, r.Y + 7), Color.White * 0.90f, 1);
        }


        sb.End();
    }

    // ---------------- POI / Enter logic ----------------

 


    // ---------------- Collision ----------------

    private bool IsBlocked(Point cell)
    {
        if (_map == null) return true;

        if (_map.IsSolidCell(cell.X, cell.Y))
            return true;

        return _propBlocked.Contains(cell);
    }


    private void EnsurePropChunkForCell(Point cell)
    {
        int cx = cell.X / PropChunkSizeTiles;
        int cy = cell.Y / PropChunkSizeTiles;
        var key = new Point(cx, cy);

        if (_propChunks.ContainsKey(key))
            return;

        var props = GeneratePropChunk(cx, cy);
        _propChunks[key] = props;

        // Add blocking props to collision set
        for (int i = 0; i < props.Count; i++)
        {
            if (props[i].BlocksMove)
                _propBlocked.Add(props[i].Tile);
        }
    }

    // ---------------- Drawing: Player / Props / POIs ----------------

    private void DrawWorldPlayer(SpriteBatch sb)
    {
        if (_map == null) return;

        var tl = _map.CellToWorldTopLeft(_playerCell);

        if (_s.Sprites.TryGet("world.player", out var tex, out var origin))
        {
            var pos = new Vector2(tl.X + 16, tl.Y + 32); // bottom-center of 32x32 tile
            sb.Draw(tex, pos, null, Color.White, 0f, origin, 1f, SpriteEffects.None, 0f);
        }
        else
        {
            sb.Draw(_white!, new Rectangle((int)tl.X + 2, (int)tl.Y + 2, 12, 12), Color.Gold);
        }
    }

    private void DrawWorldProps(SpriteBatch sb, Rectangle viewWorldRect)
    {
        if (_map == null || _world == null || _terrainFlat == null || _flagsFlat == null)
            return;

        const int tileSize = 32;

        int minTx = Math.Max(0, viewWorldRect.Left / tileSize);
        int minTy = Math.Max(0, viewWorldRect.Top / tileSize);
        int maxTx = Math.Min(_map.Width - 1, viewWorldRect.Right / tileSize + 1);
        int maxTy = Math.Min(_map.Height - 1, viewWorldRect.Bottom / tileSize + 1);

        int minCx = minTx / PropChunkSizeTiles;
        int minCy = minTy / PropChunkSizeTiles;
        int maxCx = maxTx / PropChunkSizeTiles;
        int maxCy = maxTy / PropChunkSizeTiles;

        for (int cy = minCy; cy <= maxCy; cy++)
        for (int cx = minCx; cx <= maxCx; cx++)
        {
            var key = new Point(cx, cy);

            if (!_propChunks.TryGetValue(key, out var props))
            {
                props = GeneratePropChunk(cx, cy);
                _propChunks[key] = props;

                for (int i = 0; i < props.Count; i++)
                    if (props[i].BlocksMove) _propBlocked.Add(props[i].Tile);
            }

            for (int i = 0; i < props.Count; i++)
            {
                var p = props[i];

                if (p.Tile.X < minTx || p.Tile.X > maxTx || p.Tile.Y < minTy || p.Tile.Y > maxTy)
                    continue;

                if (!_s.Sprites.TryGet(p.SpriteId, out var tex, out var origin))
                    continue;

                var tl = _map.CellToWorldTopLeft(p.Tile);

                // sit on ground: bottom-center of tile
                var pos = new Vector2(tl.X + 16, tl.Y + 32) + Jitter(p.Tile);

                sb.Draw(tex, pos, null, Color.White, 0f, origin, 1f, SpriteEffects.None, 0f);
            }
        }
    }

    private void DrawWorldPois(SpriteBatch sb, Rectangle view)
    {
        if (_world == null || _flagsFlat == null || _map == null || _white == null)
            return;

        int w = _world.Width;
        int h = _world.Height;

        // Convert view rectangle (world pixels) to cell bounds
        int minX = Math.Max(0, view.Left / _map.TileSize);
        int minY = Math.Max(0, view.Top / _map.TileSize);
        int maxX = Math.Min(w - 1, (view.Right / _map.TileSize) + 1);
        int maxY = Math.Min(h - 1, (view.Bottom / _map.TileSize) + 1);

        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                int i = x + y * w;
                var flags = (TileFlags)_flagsFlat[i];

                if ((flags & TileFlags.Ruins) == 0)
                    continue;

                // Draw a visible marker for now (until you swap to ruin_pillar sprite)
                var topLeft = _map.CellToWorldTopLeft(new Point(x, y));

                // A small "ruins" stamp (purple-ish) that you can't miss
                var r = new Rectangle((int)topLeft.X + 10, (int)topLeft.Y + 10, 12, 12);
                sb.Draw(_white, r, Color.MediumPurple);

                // Optional: tiny bright pixel highlight
                sb.Draw(_white, new Rectangle(r.X + 3, r.Y + 3, 2, 2), Color.White * 0.85f);
            }
    }

    private void DrawWorldRoads(SpriteBatch sb, Rectangle viewWorldRect)
    {
        if (_map == null || _world == null || _flagsFlat == null) return;

        int w = _world.Width;
        int h = _world.Height;

        int minX = Math.Max(0, viewWorldRect.Left / _map.TileSize);
        int minY = Math.Max(0, viewWorldRect.Top / _map.TileSize);
        int maxX = Math.Min(w - 1, (viewWorldRect.Right / _map.TileSize) + 1);
        int maxY = Math.Min(h - 1, (viewWorldRect.Bottom / _map.TileSize) + 1);

        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                int i = x + y * w;
                var flags = (TileFlags)_flagsFlat[i];

                if (!WorldRoadAutoTiler.HasRoad(flags))
                    continue;

                bool n = y > 0 && WorldRoadAutoTiler.HasRoad((TileFlags)_flagsFlat[x + (y - 1) * w]);
                bool e = x < w - 1 && WorldRoadAutoTiler.HasRoad((TileFlags)_flagsFlat[(x + 1) + y * w]);
                bool s = y < h - 1 && WorldRoadAutoTiler.HasRoad((TileFlags)_flagsFlat[x + (y + 1) * w]);
                bool ww = x > 0 && WorldRoadAutoTiler.HasRoad((TileFlags)_flagsFlat[(x - 1) + y * w]);

                string spriteId = WorldRoadAutoTiler.Resolve(n, e, s, ww);

                if (!_s.Sprites.TryGet(spriteId, out var tex, out _))
                    continue;

                var tl = _map.CellToWorldTopLeft(new Point(x, y));
                sb.Draw(tex, new Vector2(tl.X, tl.Y), Color.White);
            }
    }



    private static string PickRoadSpriteId(bool n, bool e, bool s, bool w)
    {
        int count = (n ? 1 : 0) + (e ? 1 : 0) + (s ? 1 : 0) + (w ? 1 : 0);

        if (count == 0) return "world.road.dot";
        if (count == 4) return "world.road.cross";

        if (count == 3)
        {
            if (!n) return "world.road.t_s"; // missing N => open to S/E/W, so T points S
            if (!e) return "world.road.t_w";
            if (!s) return "world.road.t_n";
            return "world.road.t_e"; // missing W
        }

        if (count == 2)
        {
            if (n && s) return "world.road.straight_v";
            if (e && w) return "world.road.straight_h";

            if (n && e) return "world.road.corner_ne";
            if (n && w) return "world.road.corner_nw";
            if (s && e) return "world.road.corner_se";
            return "world.road.corner_sw"; // s && w
        }

        // count == 1 (end)
        if (n) return "world.road.end_n";
        if (e) return "world.road.end_e";
        if (s) return "world.road.end_s";
        return "world.road.end_w";
    }

    // ---------------- Generation: Props ----------------

    private List<WorldProp> GeneratePropChunk(int chunkX, int chunkY)
    {
        if (_map == null || _world == null || _terrainFlat == null || _flagsFlat == null)
            return new List<WorldProp>(0);

        int w = _world.Width;
        int h = _world.Height;

        int startX = chunkX * PropChunkSizeTiles;
        int startY = chunkY * PropChunkSizeTiles;

        int endX = Math.Min(startX + PropChunkSizeTiles, w);
        int endY = Math.Min(startY + PropChunkSizeTiles, h);

        int seed = Hash(_s.World.WorldSeed, chunkX, chunkY);
        var props = new List<WorldProp>(96);

        for (int y = startY; y < endY; y++)
        for (int x = startX; x < endX; x++)
        {
            // Avoid solid terrain cells
            if (_map.IsSolidCell(x, y))
                continue;

                int idx = x + y * w;
                var tid = (TileId)_terrainFlat[idx];
                var flags = (TileFlags)_flagsFlat[idx];
                // Avoid placing blocking props on POI tiles (POIs need to be walkable/enterable)
                // Avoid placing blocking props on POI tiles (ruins/town/road)
                if ((flags & (TileFlags.Ruins | TileFlags.Town | TileFlags.Road)) != 0)
                    continue;



                var zone = ZoneResolver.ResolveFrom(tid, flags);

            int tileSeed = Hash(seed, x, y);
            var rng = new Random(tileSeed);

            string? sprite = null;
            bool blocks = false;

            switch (zone.Id)
            {
                case ZoneId.Forest:
                {
                    int r = rng.Next(100);
                    if (r < 18) { sprite = "world.tree"; blocks = true; }
                    else if (r < 23) { sprite = "world.rock"; blocks = false; }
                    break;
                }

                case ZoneId.Grasslands:
                case ZoneId.Plains:
                {
                    int r = rng.Next(100);
                    if (r < 5) { sprite = "world.tree"; blocks = true; }
                    else if (r < 8) { sprite = "world.rock"; blocks = false; }
                    break;
                }

                case ZoneId.Mountains:
                {
                    int r = rng.Next(100);
                    if (r < 14) { sprite = "world.mountain"; blocks = true; }
                    else if (r < 22) { sprite = "world.rock"; blocks = true; }
                    break;
                }

                case ZoneId.Ruins:
                {
                    int r = rng.Next(100);
                    if (r < 10) { sprite = "world.ruin_pillar"; blocks = true; }
                    else if (r < 14) { sprite = "world.rock"; blocks = false; }
                    break;
                }

                default:
                    break;
            }

            if (sprite != null)
                props.Add(new WorldProp(sprite, new Point(x, y), blocks));
        }

        // back-to-front draw (lower Y drawn later looks "in front")
        props.Sort((a, b) => a.Tile.Y.CompareTo(b.Tile.Y));
        return props;
    }

    // ---------------- Generation: POIs ----------------

  

    // ---------------- Utility ----------------

    private static Vector2 Jitter(Point tile)
    {
        int h = Hash(1337, tile.X, tile.Y);
        int jx = (h & 0x3) - 1;
        int jy = ((h >> 2) & 0x3) - 1;
        return new Vector2(jx, jy);
    }

    private static int Hash(int a, int b, int c)
    {
        unchecked
        {
            int h = a;
            h = h * 31 + b;
            h = h * 31 + c;
            h ^= (h << 13);
            h ^= (h >> 17);
            h ^= (h << 5);
            return h;
        }
    }

    private static BeginnersLuck.WorldGen.WorldMap BuildWorldMap(WorldDto dto)
    {
        var wm = new BeginnersLuck.WorldGen.WorldMap
        {
            Width = dto.Width,
            Height = dto.Height,
            ChunkSize = dto.ChunkSize,
            Seed = dto.Seed,
            GeneratorVersion = dto.GeneratorVersion
        };

        int cs = dto.ChunkSize;
        int n = cs * cs;

        foreach (var ch in dto.Chunks)
        {
            var c = new BeginnersLuck.WorldGen.Data.Chunk(cs, ch.Cx, ch.Cy);

            for (int i = 0; i < n; i++)
            {
                c.Terrain[i] = (TileId)ch.Terrain[i];
                c.Flags[i] = (TileFlags)ch.Flags[i];
                c.Biome[i] = (BiomeId)ch.Biome[i];

                c.Region[i] = (ushort)ch.Region[i];
                c.SubRegion[i] = (ushort)ch.SubRegion[i];
            }

            wm.SetChunk(ch.Cx, ch.Cy, c);
        }

        return wm;
    }

    private void StartEncounterToast(EncounterDef enc, KeyboardState ks, GamePadState pad)
    {
        _toastActive = true;
        _toastT = 0f;
        _toastEncounter = enc;
        _toastSeedKs = ks;
        _toastSeedPad = pad;
    }

    private static Dir Opposite(Dir d) => d switch
    {
        Dir.North => Dir.South,
        Dir.South => Dir.North,
        Dir.East => Dir.West,
        Dir.West => Dir.East,
        _ => Dir.North
    };
    private bool DevSpawnRuinsNearPlayer(int maxRadius = 12)
    {
        if (_world == null || _map == null || _flagsFlat == null || _terrainFlat == null)
            return false;

        int w = _world.Width;
        int h = _world.Height;

        // Find a nearby land cell that isn't blocked by current "solid" rules.
        if (!TryFindNearbyPlaceableCell(_playerCell, maxRadius, out var p))
            return false;

        int i = p.X + p.Y * w;

        // Stamp the Ruins flag
        var flags = (TileFlags)_flagsFlat[i];
        flags |= TileFlags.Ruins;

        // Optional: clear cliff/coast if you treat them as solid and you want this to be enterable
        flags &= ~TileFlags.Cliff;
        flags &= ~TileFlags.Coast;

        _flagsFlat[i] = (ushort)flags;

        // Recompute world solidity for this cell (and optionally neighbors)
        RefreshSolidAround(p, radius: 1);

        _s.Toasts.Push($"DEV: RUINS @ {p.X},{p.Y}", 0.9f);
        return true;
    }

    private bool TryFindNearbyPlaceableCell(Point origin, int maxRadius, out Point found)
    {
        found = origin;

        for (int r = 1; r <= maxRadius; r++)
        {
            // Scan perimeter of a square ring around origin (cheap + deterministic)
            for (int dx = -r; dx <= r; dx++)
            {
                if (TryCandidate(origin.X + dx, origin.Y - r, out found)) return true; // top
                if (TryCandidate(origin.X + dx, origin.Y + r, out found)) return true; // bottom
            }

            for (int dy = -r + 1; dy <= r - 1; dy++)
            {
                if (TryCandidate(origin.X - r, origin.Y + dy, out found)) return true; // left
                if (TryCandidate(origin.X + r, origin.Y + dy, out found)) return true; // right
            }
        }

        return false;

        bool TryCandidate(int x, int y, out Point p)
        {
            p = default;
            if (_world == null || _map == null || _terrainFlat == null) return false;

            if ((uint)x >= (uint)_world.Width || (uint)y >= (uint)_world.Height)
                return false;

            // Must be land
            int idx = x + y * _world.Width;
            var tid = (TileId)_terrainFlat[idx];
            if (tid is TileId.DeepWater or TileId.Ocean or TileId.ShallowWater)
                return false;

            // Must be walkable now (so you can interact)
            if (_map.IsSolidCell(x, y))
                return false;

            p = new Point(x, y);
            return true;
        }
    }

    private void RefreshSolidAround(Point center, int radius)
    {
        if (_world == null || _map == null || _terrainFlat == null || _flagsFlat == null)
            return;

        int w = _world.Width;
        int h = _world.Height;

        for (int y = center.Y - radius; y <= center.Y + radius; y++)
            for (int x = center.X - radius; x <= center.X + radius; x++)
            {
                if ((uint)x >= (uint)w || (uint)y >= (uint)h)
                    continue;

                int i = x + y * w;
                var tid = (TileId)_terrainFlat[i];
                var flags = (TileFlags)_flagsFlat[i];

                bool solid = WorldCollisionResolver.IsSolid(tid, flags);
                _map.SetSolidCell(x, y, solid);

            }
    }
    private void StampRoadFromPlayer(int length = 10)
    {
        if (_world == null || _flagsFlat == null || _map == null)
            return;

        int w = _world.Width;
        int h = _world.Height;

        // Use last movement direction so it's intuitive
        Point step = _lastWorldMoveDir switch
        {
            Dir.North => new Point(0, -1),
            Dir.South => new Point(0, 1),
            Dir.East => new Point(1, 0),
            Dir.West => new Point(-1, 0),
            _ => new Point(1, 0)
        };

        int x = _playerCell.X;
        int y = _playerCell.Y;

        int stamped = 0;

        for (int i = 0; i < length; i++)
        {
            if ((uint)x >= (uint)w || (uint)y >= (uint)h)
                break;

            // Only stamp on walkable cells, but keep marching anyway
            if (!_map.IsSolidCell(x, y))
            {
                int idx = x + y * w;

                _flagsFlat[idx] = (ushort)((TileFlags)_flagsFlat[idx] | TileFlags.Road);
                WriteFlagBackToDto(x, y, TileFlags.Road, set: true);

                stamped++;
            }

            x += step.X;
            y += step.Y;
        }

        _s.Toasts.Push($"DEV: stamped ROAD len={stamped}", 0.9f);
    }

    private void StampRuinsNearPlayer(int radius = 2, int maxPlacements = 8)
    {
        if (_world == null || _flagsFlat == null || _map == null)
            return;

        int w = _world.Width;
        int h = _world.Height;

        // Try a handful of nearby cells so we don't stamp onto water/solid
        int placed = 0;

        for (int tries = 0; tries < 40 && placed < maxPlacements; tries++)
        {
            int dx = _s.Rng.Next(-radius, radius + 1);
            int dy = _s.Rng.Next(-radius, radius + 1);

            int x = _playerCell.X + dx;
            int y = _playerCell.Y + dy;

            if ((uint)x >= (uint)w || (uint)y >= (uint)h)
                continue;

            // Must be walkable ground on world map
            if (_map.IsSolidCell(x, y))
                continue;

            int idx = x + y * w;

            // Stamp ruins flag
            _flagsFlat[idx] = (ushort)((TileFlags)_flagsFlat[idx] | TileFlags.Ruins);
            WriteFlagBackToDto(x, y, TileFlags.Ruins, set: true);

            placed++;
        }

        _s.Toasts.Push(placed > 0
            ? $"DEV: stamped RUINS x{placed}"
            : "DEV: could not place ruins (all blocked)",
            0.9f);
    }

       /// <summary>
    /// Keeps the loaded _world DTO in sync with _flagsFlat for the specific cell.
    /// This matters if you later rebuild _flagsFlat from _world.Chunks again.
    /// Safe no-op if _world is null.
    /// </summary>
    private void WriteFlagBackToDto(int wx, int wy, TileFlags flag, bool set)
    {
        if (_world == null) return;

        int cs = _world.ChunkSize;
        int cx = wx / cs;
        int cy = wy / cs;
        int lx = wx - cx * cs;
        int ly = wy - cy * cs;
        int local = lx + ly * cs;

        // Find the chunk dto
        // (You can optimize this later by caching a dictionary, but this is fine for dev stamps.)
        for (int i = 0; i < _world.Chunks.Count; i++)
        {
            var ch = _world.Chunks[i];
            if (ch.Cx != cx || ch.Cy != cy)
                continue;

            ushort f = ch.Flags[local];
            var tf = (TileFlags)f;

            tf = set ? (tf | flag) : (tf & ~flag);

            ch.Flags[local] = (ushort)tf;
            return;
        }
    }

    
    private PoiType CurrentPoiType()
    {
        if (_world == null || _terrainFlat == null || _flagsFlat == null) return PoiType.None;

        int idx = _playerCell.X + _playerCell.Y * _world.Width;
        var tid = (TileId)_terrainFlat[idx];
        var flags = (TileFlags)_flagsFlat[idx];
        return WorldPoiResolver.Resolve(tid, flags);
    }

    private bool Pressed(KeyboardState ks, Keys k) => ks.IsKeyDown(k) && !_prevKs.IsKeyDown(k);
    private bool Pressed(GamePadState pad, Buttons b) => pad.IsButtonDown(b) && !_prevPad.IsButtonDown(b);

    [Flags]
    private enum RoadMask : byte
    {
        None = 0,
        N = 1 << 0,
        E = 1 << 1,
        S = 1 << 2,
        W = 1 << 3
    }

    private bool HasRoad(int x, int y)
    {
        if (_world == null || _flagsFlat == null) return false;
        if ((uint)x >= (uint)_world.Width || (uint)y >= (uint)_world.Height) return false;

        int i = x + y * _world.Width;
        return (((TileFlags)_flagsFlat[i]) & TileFlags.Road) != 0;
    }

    private RoadMask GetRoadMask(int x, int y)
    {
        RoadMask m = RoadMask.None;
        if (HasRoad(x, y - 1)) m |= RoadMask.N;
        if (HasRoad(x + 1, y)) m |= RoadMask.E;
        if (HasRoad(x, y + 1)) m |= RoadMask.S;
        if (HasRoad(x - 1, y)) m |= RoadMask.W;
        return m;
    }

    private static readonly Dictionary<RoadMask, string> RoadSpriteByMask = new()
    {
        // 0 neighbors
        [RoadMask.None] = "road.dot",

        // 1 neighbor (end-caps)
        [RoadMask.N] = "road.end_n",
        [RoadMask.E] = "road.end_e",
        [RoadMask.S] = "road.end_s",
        [RoadMask.W] = "road.end_w",

        // 2 neighbors
        [RoadMask.N | RoadMask.S] = "road.ns",
        [RoadMask.E | RoadMask.W] = "road.ew",

        [RoadMask.N | RoadMask.E] = "road.ne",
        [RoadMask.E | RoadMask.S] = "road.se",
        [RoadMask.S | RoadMask.W] = "road.sw",
        [RoadMask.W | RoadMask.N] = "road.nw",

        // 3 neighbors (T-junctions)
        [RoadMask.N | RoadMask.E | RoadMask.S] = "road.tee_e", // missing W, open to E side “stem” is W; naming varies
        [RoadMask.E | RoadMask.S | RoadMask.W] = "road.tee_s",
        [RoadMask.S | RoadMask.W | RoadMask.N] = "road.tee_w",
        [RoadMask.W | RoadMask.N | RoadMask.E] = "road.tee_n",

        // 4 neighbors
        [RoadMask.N | RoadMask.E | RoadMask.S | RoadMask.W] = "road.cross",
    };

}
