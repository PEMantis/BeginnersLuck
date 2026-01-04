using System;
using System.Collections.Generic;
using BeginnersLuck.Engine.Graphics;
using BeginnersLuck.Engine.Rendering;
using BeginnersLuck.Engine.Scenes;
using BeginnersLuck.Engine.UI;
using BeginnersLuck.Engine.Update;
using BeginnersLuck.Engine.World;
using BeginnersLuck.Game.Services;
using BeginnersLuck.Game.State;
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

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            int i = x + y * n;

            var tid = _local.Terrain[i];
            var flags = _local.Flags[i];

            bool solid =
                tid is TileId.DeepWater or TileId.ShallowWater or TileId.Ocean or TileId.Coast or TileId.Mountain
                || (flags & TileFlags.Cliff) != 0;

            _map.SetSolidCell(x, y, solid);
        }

        var edgeReach = ComputeReachableFromEdge(_map);

        _playerCell = ResolveSpawnEscapable(_map, _local, _spawn, edgeReach);

        _townCenter = _local.TownCenter.HasValue
            ? new Point(_local.TownCenter.Value.X, _local.TownCenter.Value.Y)
            : (_purpose == LocalMapPurpose.Town ? ResolveFallbackTownCenter(_local, _map) : (Point?)null);

        if (_map.IsSolidCell(_playerCell.X, _playerCell.Y))
            _playerCell = FindNearestWalkableInMask(_map, _playerCell, edgeReach);

        _cam.Position = _map.CellToWorldCenter(_playerCell);
    }

    public override void Unload()
    {
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

        if (Pressed(ks, Keys.Escape) || Pressed(pad, Buttons.B))
        {
            _s.Scenes.Pop();
            _prevKs = ks;
            _prevPad = pad;
            return;
        }

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

            if (IsOutOfBounds(next, _map.Width, _map.Height))
            {
                var exitDir = DirFromStep(dir);

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

            if (!_map.IsSolidCell(next.X, next.Y))
                _playerCell = next;
        }

        if (_townCenter.HasValue)
        {
            var tc = _townCenter.Value;
            if (_playerCell.X == tc.X && _playerCell.Y == tc.Y)
            {
                if (Pressed(pad, Buttons.A))
                {
                    _s.Scenes.Push(new TownScene(_s, new Point(_local.WorldX, _local.WorldY)));
                    _prevKs = ks;
                    _prevPad = pad;
                    return;
                }
            }
        }

        // Zoom
        CameraZoom.ApplyMouseWheel(_cam, _zoom, PixelRenderer.InternalWidth, PixelRenderer.InternalHeight);
        CameraZoom.ApplyBumpers(_cam, _zoom, pad, _prevPad, PixelRenderer.InternalWidth, PixelRenderer.InternalHeight);

        // Follow
        _cam.Position = _map.CellToWorldCenter(_playerCell);

        _prevKs = ks;
        _prevPad = pad;
    }

    protected override void DrawWorld(RenderContext rc)
    {
        if (_map == null || _mapRenderer == null || _local == null) return;

        var sb = rc.SpriteBatch;

        sb.Begin(
            samplerState: SamplerState.PointClamp,
            blendState: BlendState.AlphaBlend,
            transformMatrix: _cam.GetViewMatrix());

        // ✅ Correct: viewWorldPixels must be zoom-aware because transform scales the world.
        var view = ComputeViewWorldPixels(_cam);
        _mapRenderer.Draw(sb, _map, view);

        // Town center marker
        if (_townCenter.HasValue)
        {
            var tc = _townCenter.Value;
            var tl = _map.CellToWorldTopLeft(tc);
            sb.Draw(_s.PixelWhite, new Rectangle((int)tl.X + 6, (int)tl.Y + 6, 20, 20), Color.LimeGreen * 0.85f);
        }

        // Player marker
        var pos = _map.CellToWorldTopLeft(_playerCell);
        sb.Draw(_s.PixelWhite, new Rectangle((int)pos.X + 10, (int)pos.Y + 10, 12, 12), Color.Gold);

        sb.End();
    }

    protected override void DrawUI(RenderContext rc)
    {
        if (_local == null || _map == null) return;

        var sb = rc.SpriteBatch;
        sb.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);

        int n = _local.Size;
        int i = _playerCell.X + _playerCell.Y * n;

        _s.UiFont.Draw(sb, $"LOCAL {_local.Size}x{_local.Size} ({_local.WorldX},{_local.WorldY})  {_purpose}",
            new Vector2(8, 8), Color.White * 0.9f, scale: 1);

        _s.UiFont.Draw(sb, "ESC/B: back  |  Walk off edge to exit",
            new Vector2(8, 18), Color.White * 0.7f, scale: 1);

        _s.UiFont.Draw(sb,
            $"P: {_playerCell.X},{_playerCell.Y} solid={_map.IsSolidCell(_playerCell.X,_playerCell.Y)} tileIndex={_map.GetTileId(_playerCell.X,_playerCell.Y)} terrain={_local.Terrain[i]} flags={_local.Flags[i]}",
            new Vector2(8, 28),
            Color.White * 0.85f,
            scale: 1);

        _s.UiFont.Draw(sb,
            _townCenter.HasValue ? $"TownCenter: {_townCenter.Value.X},{_townCenter.Value.Y} (Stand + A)"
                                 : "TownCenter: (none)",
            new Vector2(8, 38),
            Color.White * 0.75f, 1);

        sb.End();
    }

    // --- helpers ---

    private static Rectangle ComputeViewWorldPixels(Camera2D cam)
    {
        float z = cam.Zoom;
        float vw = PixelRenderer.InternalWidth / z;
        float vh = PixelRenderer.InternalHeight / z;

        return new Rectangle(
            (int)(cam.Position.X - vw * 0.5f),
            (int)(cam.Position.Y - vh * 0.5f),
            (int)vw,
            (int)vh);
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
