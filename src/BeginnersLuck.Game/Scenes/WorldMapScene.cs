using System;
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BeginnersLuck.Game.Scenes;

public sealed class MapDto
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int TileSize { get; set; }
    public string Tileset { get; set; } = "";
    public int[] Solid { get; set; } = Array.Empty<int>();
    public int[] Tiles { get; set; } = Array.Empty<int>();
}

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
    private float _toastDuration = 0.55f;
    private string _toastText = "";
    private EncounterDef? _toastEncounter;
    private KeyboardState _toastSeedKs;
    private GamePadState _toastSeedPad;

    public WorldMapScene(GameServices s)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
    }

    public override void Load(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _white = new Texture2D(graphicsDevice, 1, 1);
        _white.SetData(new[] { Color.White });

        // Load map json (RawContent path is relative to ContentRaw root)
        var json = _s.Raw.LoadText("Data/map_test.json");
        var dto = JsonSerializer.Deserialize<MapDto>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        if (string.IsNullOrWhiteSpace(dto.Tileset))
            throw new InvalidOperationException("Map JSON missing 'tileset' (expected e.g. \"Textures/tiles.png\").");

        _map = new TileMap(dto.Width, dto.Height, dto.TileSize, dto.Tiles);

        foreach (var id in dto.Solid)
            _map.SetSolid(id, true);

        var tex = _s.Raw.LoadTexture(dto.Tileset);
        _tileset = new TileSet(tex, dto.TileSize);
        _mapRenderer = new TileMapRenderer(_tileset);

        // Zones derived from tiles (runtime, deterministic)
        _s.Zones = ZoneMap.GenerateFromTiles(
            _map.Width,
            _map.Height,
            (x, y) => _map.GetTileId(x, y),
            seed: 12345,
            zoneSizeCells: 8
        );

        // Start camera centered
        _cam.Position = _map.CellToWorldCenter(_playerCell);
    }

    public override void Unload()
    {
        _white?.Dispose();
        _white = null;

        // Note: we DO NOT dispose tileset texture here because RawContent may cache/reuse.
        // If you want explicit lifetime, we can add a texture cache with ownership rules.
        _tileset = null;
        _mapRenderer = null;
        _map = null;
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

                _s.Fade.Start(0.25f, () =>
                {
                    _s.Scenes.Push(new BattleScene(_s, _toastEncounter, _toastSeedKs, _toastSeedPad));
                });
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

        bool moved = false;

        if (dir != Point.Zero)
        {
            var next = _playerCell + dir;

            // Collision
            if (!_map.IsSolidCell(next.X, next.Y))
            {
                _playerCell = next;
                moved = true;
            }
        }

        // If moved, roll for encounter based on zone
        if (moved && _s.Zones != null)
        {
            var zone = _s.Zones.GetInfo(_playerCell.X, _playerCell.Y);
            var intent = _s.EncounterDirector.OnPlayerMoved(_playerCell, zone, _s.Rng);

            if (intent.HasValue && !_s.Fade.Active)
            {
                var i = intent.Value;
                StartEncounterToast(i.Encounter, $"ENCOUNTER: {i.Encounter.Name.ToUpperInvariant()}!", ks, pad);

                _prevKs = ks;
                _prevPad = pad;
                return;
            }
        }

        // Camera follow (center on player)
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

        // view rect centered on camera
        var view = new Rectangle(
            (int)(_cam.Position.X - PixelRenderer.InternalWidth * 0.5f),
            (int)(_cam.Position.Y - PixelRenderer.InternalHeight * 0.5f),
            PixelRenderer.InternalWidth,
            PixelRenderer.InternalHeight);

        _mapRenderer.Draw(sb, _map, view);

        // Player marker (top-left of cell, inset)
        var pos = _map.CellToWorldTopLeft(_playerCell);
        sb.Draw(_white, new Rectangle((int)pos.X + 10, (int)pos.Y + 10, 12, 12), Color.Gold);

        sb.End();
    }

    protected override void DrawUI(RenderContext rc)
    {
        if (_white == null) return;

        var sb = rc.SpriteBatch;

        sb.Begin(
            samplerState: SamplerState.PointClamp,
            blendState: BlendState.AlphaBlend);

        // HUD panel (top-left)
        var hud = new Rectangle(8, 8, 190, 34);
        sb.Draw(_white, hud, new Color(10, 10, 18) * 0.75f);
        sb.Draw(_white, new Rectangle(hud.X, hud.Y, hud.Width, 1), Color.White * 0.18f);
        sb.Draw(_white, new Rectangle(hud.X, hud.Bottom - 1, hud.Width, 1), Color.White * 0.12f);

        // Text
        // (BitmapFont is your 8x8; scale 1 keeps it crisp in 480x270)
        var gold = _s.Player.Gold;
        var xp = _s.Player.Xp;

        _s.UiFont.Draw(sb, $"GOLD: {gold}", new Vector2(hud.X + 8, hud.Y + 8), Color.White * 0.9f, scale: 1);
        _s.UiFont.Draw(sb, $"XP:   {xp}", new Vector2(hud.X + 8, hud.Y + 18), Color.White * 0.9f, scale: 1);

        // Optional: debug hint (remove anytime)
        // _s.Font.Draw(sb, "ESC/START: PAUSE", new Vector2(8, 48), Color.White * 0.5f, 1);

        sb.End();
    }


    private void StartEncounterToast(EncounterDef enc, string text, KeyboardState ks, GamePadState pad)
    {
        _toastActive = true;
        _toastT = 0f;
        _toastText = text;
        _toastEncounter = enc;

        // seed input so BattleScene won't press-through
        _toastSeedKs = ks;
        _toastSeedPad = pad;
    }

    private bool Pressed(KeyboardState ks, Keys k) => ks.IsKeyDown(k) && !_prevKs.IsKeyDown(k);
    private bool Pressed(GamePadState pad, Buttons b) => pad.IsButtonDown(b) && !_prevPad.IsButtonDown(b);

    private static void DrawTextCentered8x8(SpriteBatch sb, IFont font, string text, Rectangle r, Color color, int scale)
    {
        var size = MeasureText8x8(text, scale);
        var pos = new Vector2(
            r.X + (r.Width - size.X) * 0.5f,
            r.Y + (r.Height - size.Y) * 0.5f);

        font.Draw(sb, text, pos, color, scale);
    }

    private static Point MeasureText8x8(string text, int scale)
    {
        int maxLine = 0;
        int line = 0;
        int lines = 1;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                maxLine = Math.Max(maxLine, line);
                line = 0;
                lines++;
            }
            else
            {
                line++;
            }
        }

        maxLine = Math.Max(maxLine, line);
        return new Point(maxLine * 8 * scale, lines * 8 * scale);
    }
}
