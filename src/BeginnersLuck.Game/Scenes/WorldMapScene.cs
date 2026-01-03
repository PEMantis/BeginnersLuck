using System;
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

    // Encounter toast (kept, but not actively used here yet)
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
    private Dir _lastWorldMoveDir = Dir.North; // default doesn’t matter much

    public WorldMapScene(GameServices s)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
    }

    public override void Load(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _white = new Texture2D(graphicsDevice, 1, 1);
        _white.SetData(new[] { Color.White });

        // 1) Load generated world.json from repo-root Worlds folder
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

        // 2) Flatten terrain + flags from chunks
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
                _flagsFlat[i] = (ushort)ch.Flags[local]; // <-- IMPORTANT: keep ushort, do NOT cast to byte
            }
            
        }

        // 3) Build render map + collision
        const int tileSize = 16;
        _map = new TileMap(w, h, tileSize, new int[w * h]);
        if (_map.IsSolidCell(_playerCell.X, _playerCell.Y))
        {
            _playerCell = FindNearestWalkable(_playerCell, w, h);
        }

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = x + y * w;
                var tid = (TileId)_terrainFlat[i];
                var flags = (TileFlags)_flagsFlat[i];

                _map.Tiles[i] = WorldTilePalette.ToTileIndex(tid);

                // WORLD-MAP collision must consider FLAGS too (coast ring trap)
                bool solid =
                    WorldTilePalette.IsSolid(tid) ||
                    (flags & TileFlags.Coast) != 0 ||
                    (flags & TileFlags.Cliff) != 0; // if you use cliffs on world

                _map.SetSolidCell(x, y, solid);

            }


        // 4) Renderer
        var tex = _s.Raw.LoadTexture("Textures/tiles.png");
        _tileset = new TileSet(tex, tileSize);
        _mapRenderer = new TileMapRenderer(_tileset);

        // 5) Build a real WorldMap for LocalMapCache / LocalMapGenerator
        _worldMap = BuildWorldMap(_world);

        // 6) Start player near center on walkable cell
        _playerCell = new Point(w / 2, h / 2);

        // Require a spawn that isn't trapped in a sealed pocket.
        // minRegionSize: tune for your world size. These are sane starters.
        int minRegion = Math.Max(800, (w * h) / 50); // ~2% of map or 800
        int maxR = Math.Max(w, h);

        _playerCell = BeginnersLuck.Game.World.WorldSpawnResolver.FindPlayableSpawn(
            map: _map!,
            preferred: _playerCell,
            maxSearchRadius: maxR,
            minRegionSize: minRegion,
            requireTouchesEdge: true);

        _cam.Position = _map.CellToWorldCenter(_playerCell);


        // 7) Camera start
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
    }

    public override void Update(UpdateContext uc)
    {
        if (_map == null) return;

        var ks = Keyboard.GetState();
        var pad = GamePad.GetState(PlayerIndex.One);

        // Toast active: tick + block input
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

        // Pause (Esc/Start/Back)
        if (Pressed(ks, Keys.Escape) || Pressed(pad, Buttons.Start) || Pressed(pad, Buttons.Back))
        {
            _s.Scenes.Push(new PauseScene(_s));
            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        // Hub/Menu (Tab / Y)
        if (Pressed(ks, Keys.Tab) || Pressed(pad, Buttons.Y))
        {
            _s.Scenes.Push(new MenuHubScene(_s, ks, startTab: 0));
            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        // Movement (edge-triggered, grid step)
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

        if (dir != Point.Zero)
        {
            var next = _playerCell + dir;
            if ((uint)next.X < (uint)_map.Width && (uint)next.Y < (uint)_map.Height)
            {
                if (!_map.IsSolidCell(next.X, next.Y))
                {
                    _playerCell = next;

                    // Only update when the move actually happened
                    if (dir.X == 1) _lastWorldMoveDir = Dir.East;
                    else if (dir.X == -1) _lastWorldMoveDir = Dir.West;
                    else if (dir.Y == 1) _lastWorldMoveDir = Dir.South;
                    else if (dir.Y == -1) _lastWorldMoveDir = Dir.North;
                }
                else
                    _s.Toasts.Push("Blocked.", 0.35f);
            }
        }

        if (dir.X == 1) _lastWorldMoveDir = Dir.East;
        if (dir.X == -1) _lastWorldMoveDir = Dir.West;
        if (dir.Y == 1) _lastWorldMoveDir = Dir.South;
        if (dir.Y == -1) _lastWorldMoveDir = Dir.North;

        // DEV: jump into known-good local map (seed777 local_262_20)
        if (Pressed(ks, Keys.F9) || Pressed(pad, Buttons.RightStick))
        {
            int seed = _s.World.WorldSeed;
            int wx = 262;
            int wy = 20;
            var purpose = LocalMapPurpose.Town;

            string localBin = WorldPaths.LocalBin(seed, wx, wy);
            var spawn = new SpawnRequest(SpawnIntent.EnterFromRoad, IncomingDir: Opposite(_lastWorldMoveDir));

            if (!File.Exists(localBin))
                _s.Toasts.Push($"Missing: {localBin}", 1.4f);
            else if (!_s.Fade.Active)
                _s.Fade.Start(0.25f, () => _s.Scenes.Push(new LocalMapScene(_s, localBin, purpose, spawn)));

            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        // Enter / Generate Local Map (E / A)
        if (Pressed(ks, Keys.E) || Pressed(pad, Buttons.A))
        {
            if (_world == null || _flagsFlat == null || _worldMap == null)
            {
                _s.Toasts.Push("World not loaded.", 1.2f);
                _prevKs = ks; _prevPad = pad;
                return;
            }

            int wx = _playerCell.X;
            int wy = _playerCell.Y;

            int idx = wx + wy * _world.Width;
            var purpose = WorldTilePalette.PurposeFromFlags(_flagsFlat[idx]);

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

        // Camera follow
        _cam.Position = _map.CellToWorldCenter(_playerCell);

        _prevKs = ks;
        _prevPad = pad;
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

        // Player marker
        var pos = _map.CellToWorldTopLeft(_playerCell);
        sb.Draw(_white, new Rectangle((int)pos.X + 2, (int)pos.Y + 2, 12, 12), Color.Gold);

        sb.End();
    }

    protected override void DrawUI(RenderContext rc)
    {
        if (_white == null) return;

        var sb = rc.SpriteBatch;
        sb.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);

        // HUD panel
        var hud = new Rectangle(8, 8, 260, 52);
        sb.Draw(_white, hud, new Color(10, 10, 18) * 0.75f);

        _s.UiFont.Draw(sb, $"GOLD: {_s.Player.Gold}", new Vector2(hud.X + 8, hud.Y + 8), Color.White * 0.9f, 1);
        _s.UiFont.Draw(sb, $"XP:   {_s.Player.TotalXp}", new Vector2(hud.X + 8, hud.Y + 18), Color.White * 0.9f, 1);
        if (_world != null && _terrainFlat != null && _flagsFlat != null && _map != null)
        {
            int idx = _playerCell.X + _playerCell.Y * _world.Width;
            var tid = (TileId)_terrainFlat[idx];
            var flags = (TileFlags)_flagsFlat[idx];
            bool solid = _map.IsSolidCell(_playerCell.X, _playerCell.Y);

            _s.UiFont.Draw(sb,
                $"WORLD: {_playerCell.X},{_playerCell.Y}  {tid}  {_flagsFlat[idx]}  solid={_map.IsSolidCell(_playerCell.X, _playerCell.Y)}  tileIndex={idx}",
                new Vector2(hud.X + 8, hud.Y + 32),
                Color.White * 0.75f, 1);
        }


        sb.End();
    }

    private Point FindNearestWalkable(Point start, int w, int h)
    {
        if (_map == null) return start;

        if (!_map.IsSolidCell(start.X, start.Y))
            return start;

        for (int r = 1; r < 128; r++)
        {
            for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                int x = start.X + dx;
                int y = start.Y + dy;
                if ((uint)x >= (uint)w || (uint)y >= (uint)h) continue;

                if (!_map.IsSolidCell(x, y))
                    return new Point(x, y);
            }
        }

        return start;
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
        // IMPORTANT: match your Chunk ctor signature
        var c = new BeginnersLuck.WorldGen.Data.Chunk(cs, ch.Cx, ch.Cy);

        // Copy arrays (fast + safe)
        // Terrain + Flags + Biome are the critical ones for local generation.
        for (int i = 0; i < n; i++)
        {
            c.Terrain[i] = (TileId)ch.Terrain[i];
            c.Flags[i]   = (TileFlags)ch.Flags[i];

            c.Biome[i]   = (BiomeId)ch.Biome[i];

            // Region/SubRegion are ushort already in Chunk.
            c.Region[i]    = (ushort)ch.Region[i];
            c.SubRegion[i] = (ushort)ch.SubRegion[i];

            // If your DTO includes these, copy them too:
            // c.Elevation[i] = ch.Elevation[i];
            // c.Moisture[i] = ch.Moisture[i];
            // c.Temperature[i] = ch.Temperature[i];
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

    private bool Pressed(KeyboardState ks, Keys k) => ks.IsKeyDown(k) && !_prevKs.IsKeyDown(k);
    private bool Pressed(GamePadState pad, Buttons b) => pad.IsButtonDown(b) && !_prevPad.IsButtonDown(b);
}
