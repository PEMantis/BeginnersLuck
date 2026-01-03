using System;
using System.Collections.Generic;
using BeginnersLuck.Engine.Graphics;
using BeginnersLuck.Engine.Rendering;
using BeginnersLuck.Engine.Scenes;
using BeginnersLuck.Engine.Update;
using BeginnersLuck.Engine.World;
using BeginnersLuck.Game.Services;
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

    public LocalMapScene(GameServices s, string mapBinPath, LocalMapPurpose purpose, SpawnRequest spawn)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
        _mapBinPath = mapBinPath ?? throw new ArgumentNullException(nameof(mapBinPath));
        _purpose = purpose;
        _spawn = spawn;
    }

    public override void Load(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _local = LocalMapBinLoader.Load(_mapBinPath);
        int n = _local.Size;

        const int tileSize = 16;

        var tex = _s.Raw.LoadTexture("Textures/tiles.png");
        _tileset = new TileSet(tex, tileSize);
        _mapRenderer = new TileMapRenderer(_tileset);

        var tiles = new int[n * n];
        for (int i = 0; i < tiles.Length; i++)
            tiles[i] = LocalTilePalette.ToTileIndex(_local.Terrain[i]);

        _map = new TileMap(n, n, tileSize, tiles);

        // Authoritative collision: Terrain + Flags (no tileIndex fallback reliance)
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            int i = x + y * n;
            var tid = _local.Terrain[i];
            var flags = _local.Flags[i];

            bool solid =
                LocalTilePalette.IsSolid(tid) ||
                (flags & TileFlags.Cliff) != 0;

            // NOTE: Rivers are visual overlays for now (do NOT make them solid yet)
            _map.SetSolidCell(x, y, solid);
        }

        // Pick spawn in the largest connected walkable component (prevents trapped pockets)
        _playerCell = PickSpawnLargestComponent(_map, _local, _spawn);

        // Final safety: if somehow solid, snap to any walkable in largest component
        if (_map.IsSolidCell(_playerCell.X, _playerCell.Y))
            _playerCell = FindAnyWalkable(_map) ?? new Point(n / 2, n / 2);

        // Debug
        int si = _playerCell.X + _playerCell.Y * n;
        Console.WriteLine($"[LocalSpawn] spawn={_playerCell.X},{_playerCell.Y} terrain={_local.Terrain[si]} flags={_local.Flags[si]} solid={_map.IsSolidCell(_playerCell.X,_playerCell.Y)}");

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
        if (_map == null) return;

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
            if (!_map.IsSolidCell(next.X, next.Y))
                _playerCell = next;
        }

        _cam.Position = _map.CellToWorldCenter(_playerCell);

        _prevKs = ks;
        _prevPad = pad;
    }

    protected override void DrawWorld(RenderContext rc)
    {
        if (_map == null || _mapRenderer == null) return;

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
        sb.Draw(_s.PixelWhite, new Rectangle((int)pos.X + 10, (int)pos.Y + 10, 12, 12), Color.Gold);

        sb.End();
    }

    // --------------------------------------------------------------------
    // Spawn: choose largest connected walkable component, then pick a good cell in it.
    // --------------------------------------------------------------------
    private static Point PickSpawnLargestComponent(TileMap map, LocalMapData local, SpawnRequest spawn)
    {
        int n = local.Size;

        // Build components of walkable cells (4-neighbor)
        var compId = new int[n * n];
        Array.Fill(compId, -1);

        var compSizes = new List<int>();
        var compRoadCounts = new List<int>();

        int currentComp = 0;

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            int idx = x + y * n;
            if (compId[idx] != -1) continue;
            if (map.IsSolidCell(x, y)) continue;

            int size = 0;
            int roads = 0;

            var q = new Queue<Point>();
            q.Enqueue(new Point(x, y));
            compId[idx] = currentComp;

            while (q.Count > 0)
            {
                var p = q.Dequeue();
                size++;

                int i = p.X + p.Y * n;
                if ((local.Flags[i] & TileFlags.Road) != 0) roads++;

                Try(p.X + 1, p.Y);
                Try(p.X - 1, p.Y);
                Try(p.X, p.Y + 1);
                Try(p.X, p.Y - 1);
            }

            compSizes.Add(size);
            compRoadCounts.Add(roads);
            currentComp++;

            void Try(int xx, int yy)
            {
                if ((uint)xx >= (uint)n || (uint)yy >= (uint)n) return;
                int ii = xx + yy * n;
                if (compId[ii] != -1) return;
                if (map.IsSolidCell(xx, yy)) return;

                compId[ii] = currentComp;
                q.Enqueue(new Point(xx, yy));
            }
        }

        if (compSizes.Count == 0)
            return new Point(n / 2, n / 2);

        // Choose best component:
        // - Prefer biggest component
        // - If entering from road, prefer components with roads (tie-break)
        int best = 0;
        for (int c = 1; c < compSizes.Count; c++)
        {
            if (compSizes[c] > compSizes[best]) best = c;
            else if (compSizes[c] == compSizes[best] && spawn.Intent == SpawnIntent.EnterFromRoad)
            {
                if (compRoadCounts[c] > compRoadCounts[best]) best = c;
            }
        }

        // Choose a preferred seed (edge entry)
        Point seed = spawn.Intent == SpawnIntent.EnterFromRoad && spawn.IncomingDir.HasValue
            ? EdgeSeed(spawn.IncomingDir.Value, n)
            : new Point(n / 2, n / 2);

        // Find nearest cell in best component, prefer road if requested
        bool requireRoad = spawn.Intent == SpawnIntent.EnterFromRoad;

        Point bestCell = FindNearestInComponent(seed, best, requireRoad)
                      ?? FindNearestInComponent(seed, best, requireRoad2: false)
                      ?? FindAnyInComponent(best)
                      ?? new Point(n / 2, n / 2);

        return bestCell;

        Point? FindNearestInComponent(Point start, int comp, bool requireRoad2)
        {
            // expanding diamond around start
            for (int r = 0; r <= n; r++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    int dx = r - Math.Abs(dy);

                    if (Ok(start.X + dx, start.Y + dy, comp, requireRoad2, out var p)) return p;
                    if (Ok(start.X - dx, start.Y + dy, comp, requireRoad2, out p)) return p;
                }
            }
            return null;
        }

        bool Ok(int x, int y, int comp, bool requireRoad2, out Point p)
        {
            p = new Point(x, y);
            if ((uint)x >= (uint)n || (uint)y >= (uint)n) return false;

            int idx = x + y * n;
            if (compId[idx] != comp) return false;

            if (requireRoad2 && (local.Flags[idx] & TileFlags.Road) == 0) return false;
            return true;
        }

        Point? FindAnyInComponent(int comp)
        {
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                if (compId[x + y * n] == comp)
                    return new Point(x, y);
            return null;
        }
    }

    private static Point EdgeSeed(Dir dir, int n) => dir switch
    {
        Dir.West  => new Point(1, n / 2),
        Dir.East  => new Point(n - 2, n / 2),
        Dir.North => new Point(n / 2, 1),
        Dir.South => new Point(n / 2, n - 2),
        _ => new Point(n / 2, n / 2)
    };

    private static Point? FindAnyWalkable(TileMap map)
    {
        for (int y = 0; y < map.Height; y++)
        for (int x = 0; x < map.Width; x++)
            if (!map.IsSolidCell(x, y))
                return new Point(x, y);
        return null;
    }

    private bool Pressed(KeyboardState ks, Keys k) => ks.IsKeyDown(k) && !_prevKs.IsKeyDown(k);
    private bool Pressed(GamePadState pad, Buttons b) => pad.IsButtonDown(b) && !_prevPad.IsButtonDown(b);
}
