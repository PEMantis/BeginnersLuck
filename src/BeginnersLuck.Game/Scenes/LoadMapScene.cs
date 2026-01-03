using System;
using System.Collections.Generic;
using System.Reflection;
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

        // Convert terrain -> tileIndex
        for (int i = 0; i < tiles.Length; i++)
            tiles[i] = LocalTilePalette.ToTileIndex(_local.Terrain[i]);

        _map = new TileMap(n, n, tileSize, tiles);

        // Authoritative collision: define solidity per-cell from Terrain + Flags
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

        // Compute escapable set: all cells reachable from ANY walkable edge tile
        var edgeReach = ComputeReachableFromEdge(_map);

        // Pick spawn INSIDE the escapable set (prevents trapped pockets)
        _playerCell = ResolveSpawnEscapable(_map, _local, _spawn, edgeReach);

        // Final safety: never spawn on solid
        if (_map.IsSolidCell(_playerCell.X, _playerCell.Y))
            _playerCell = FindNearestWalkableInMask(_map, _playerCell, edgeReach);

        int si = _playerCell.X + _playerCell.Y * n;
        Console.WriteLine(
            $"[LocalMapScene] Spawn: {_playerCell.X},{_playerCell.Y} " +
            $"Terrain={_local.Terrain[si]} Flags={_local.Flags[si]} " +
            $"Solid={_map.IsSolidCell(_playerCell.X,_playerCell.Y)} TileIndex={_map.GetTileId(_playerCell.X,_playerCell.Y)}");

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

        // Manual back (ESC/B) without travel
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

            // If stepping out of bounds, treat it as an edge-exit attempt.
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

                    // Fade is optional; feel free to pop immediately if you prefer.
                    if (!_s.Fade.Active)
                    {
                        _s.Fade.Start(0.15f, () => _s.Scenes.Pop());
                    }
                    else
                    {
                        _s.Scenes.Pop();
                    }

                    _prevKs = ks;
                    _prevPad = pad;
                    return;
                }
                else
                {
                    _s.Toasts.Push("No exit here.", 0.35f);
                }
            }
            else
            {
                // Normal in-bounds step
                if (!_map.IsSolidCell(next.X, next.Y))
                    _playerCell = next;
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

        // Overlays (roads/rivers) neighbor-aware
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

        sb.End();
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

    /// <summary>
    /// We "respect portals if present", but we don't hard-bind to any specific EdgePortals shape.
    /// If we can't read it, we default to allowing exit (dev-friendly).
    /// </summary>
    private static bool PortalAllowsExit(LocalMapData local, Dir d)
    {
        object portals = local.Portals;
        var t = portals.GetType();

        string name = d switch
        {
            Dir.North => "North",
            Dir.South => "South",
            Dir.East => "East",
            Dir.West => "West",
            _ => "North"
        };

        // Try property first
        var prop = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        if (prop != null)
        {
            var v = prop.GetValue(portals);
            if (v is bool b) return b;
            if (v is int i) return i != 0;
            if (v is byte bb) return bb != 0;
            if (v is short s) return s != 0;
            if (v is Enum e) return Convert.ToInt32(e) != 0;
        }

        // Try field
        var field = t.GetField(name, BindingFlags.Public | BindingFlags.Instance);
        if (field != null)
        {
            var v = field.GetValue(portals);
            if (v is bool b) return b;
            if (v is int i) return i != 0;
            if (v is byte bb) return bb != 0;
            if (v is short s) return s != 0;
            if (v is Enum e) return Convert.ToInt32(e) != 0;
        }

        // Unknown shape: allow exit (so dev loop always works)
        return true;
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

        // Seed queue with all walkable edge cells
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

        // BFS
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

        Point EdgeSeed(Dir dir) => dir switch
        {
            Dir.West => new Point(1, n / 2),
            Dir.East => new Point(n - 2, n / 2),
            Dir.North => new Point(n / 2, 1),
            Dir.South => new Point(n / 2, n - 2),
            _ => new Point(n / 2, n / 2),
        };

        Point start = spawn.Intent switch
        {
            SpawnIntent.EnterFromRoad when spawn.IncomingDir.HasValue => EdgeSeed(spawn.IncomingDir.Value),
            _ => new Point(n / 2, n / 2)
        };

        bool Ok(int x, int y, bool requireRoad)
        {
            if ((uint)x >= (uint)n || (uint)y >= (uint)n) return false;
            if (map.IsSolidCell(x, y)) return false;
            if (!edgeReach[map.Index(x, y)]) return false; // must be escapable
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
                    if (Ok(maxX, y, requireRoad)) return new Point(x: maxX, y: y);
                }
            }

            // Fallback scan: any escapable tile
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
}
