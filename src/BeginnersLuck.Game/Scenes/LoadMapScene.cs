using System;
using BeginnersLuck.Engine.Graphics;
using BeginnersLuck.Engine.Rendering;
using BeginnersLuck.Engine.Scenes;
using BeginnersLuck.Engine.UI;
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

    private LocalMapData? _local;

    private readonly Camera2D _cam = new();
    private TileMap? _map;
    private TileSet? _tileset;
    private TileMapRenderer? _mapRenderer;

    private Point _playerCell = new(8, 8);

    private KeyboardState _prevKs;
    private GamePadState _prevPad;

    public LocalMapScene(GameServices s, string mapBinPath, LocalMapPurpose purpose)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
        _mapBinPath = mapBinPath ?? throw new ArgumentNullException(nameof(mapBinPath));
        _purpose = purpose;
    }

    public override void Load(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _local = LocalMapBinLoader.Load(_mapBinPath);

        int tileSize = 16;

        var tex = _s.Raw.LoadTexture("Textures/tiles.png");
        _tileset = new TileSet(tex, tileSize);
        _mapRenderer = new TileMapRenderer(_tileset);

        int n = _local.Size;
        var tiles = new int[n * n];

        // Convert terrain -> tileIndex (single pass)
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            int i = x + y * n;
            tiles[i] = LocalTilePalette.ToTileIndex(_local.Terrain[i]);
        }

        _map = new TileMap(n, n, tileSize, tiles);

        // Mark SOLID BY TILE INDEX (reliable)
        // (So if DeepWater maps to tileIndex=1, we just block index=1 everywhere.)
        for (int i = 0; i < tiles.Length; i++)
        {
            int ti = tiles[i];
            if (LocalTilePalette.IsSolidTileIndex(ti))
                _map.SetSolid(ti, true);
        }

        // Debug counts: walkables
        int walkable = 0;
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            if (!_map.IsSolidCell(x, y)) walkable++;
        }
        Console.WriteLine($"[LocalMapScene] Walkable cells: {walkable} / {n*n}");

        // Spawn: if current spawn is blocked, find nearest walkable
        if (_map.IsSolidCell(_playerCell.X, _playerCell.Y))
        {
            _playerCell = FindNearestWalkable(_map, _playerCell);
        }

        int spawnI = _playerCell.X + _playerCell.Y * n;
        Console.WriteLine($"[LocalMapScene] Spawn: {_playerCell.X},{_playerCell.Y} Terrain={_local.Terrain[spawnI]} Solid={_map.IsSolidCell(_playerCell.X,_playerCell.Y)} TileIndex={_map.GetTileId(_playerCell.X,_playerCell.Y)}");

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

        _s.UiFont.Draw(sb, "E/A: (later) interact   ESC/B: back",
            new Vector2(8, 18), Color.White * 0.7f, scale: 1);

        _s.UiFont.Draw(sb,
            $"P: {_playerCell.X},{_playerCell.Y}  solid={_map.IsSolidCell(_playerCell.X,_playerCell.Y)} tileIndex={_map.GetTileId(_playerCell.X,_playerCell.Y)} terrain={_local.Terrain[i]}",
            new Vector2(8, 28),
            Color.White * 0.85f,
            scale: 1);

        sb.End();
    }

    private static Point FindNearestWalkable(TileMap map, Point start)
    {
        // simple expanding diamond search
        for (int r = 0; r < 64; r++)
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

        // If the entire map is solid, just give up and keep start
        return start;
    }

    private bool Pressed(KeyboardState ks, Keys k) => ks.IsKeyDown(k) && !_prevKs.IsKeyDown(k);
    private bool Pressed(GamePadState pad, Buttons b) => pad.IsButtonDown(b) && !_prevPad.IsButtonDown(b);
}
