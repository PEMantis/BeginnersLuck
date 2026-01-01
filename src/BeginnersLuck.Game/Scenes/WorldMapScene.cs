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
    private EncounterDef _toastEncounter;
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
            _s.Scenes.Push(new PauseScene(_s, ks, pad));
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

        sb.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);

        // Debug panel background
        sb.Draw(_white, new Rectangle(10, 10, 240, 74), new Color(30, 30, 50) * 0.9f);
        sb.Draw(_white, new Rectangle(10, 10, 240, 2), Color.White * 0.5f);

        // ---- Debug overlay: zones + encounter chance ----
        if (_map != null && _s.Zones != null)
        {
            var z = _s.Zones.GetInfo(_playerCell.X, _playerCell.Y);
            float chance = _s.EncounterDirector.ComputeChancePerStep(z);
            int cd = _s.EncounterDirector.CooldownRemainingSteps;

            int y = 16;
            _s.Font.Draw(sb, $"CELL: {_playerCell.X},{_playerCell.Y}", new Vector2(16, y), Color.White * 0.9f, 1); y += 12;
            _s.Font.Draw(sb, $"ZONE: {z.Id}", new Vector2(16, y), Color.White * 0.9f, 1); y += 12;
            _s.Font.Draw(sb, $"DANGER: {z.Danger}", new Vector2(16, y), Color.White * 0.9f, 1); y += 12;

            var pct = (int)(chance * 100f + 0.5f);
            _s.Font.Draw(sb, $"CHANCE: {pct}%  CD:{cd}", new Vector2(16, y), Color.White * 0.9f, 1);
        }

        // ---- Encounter toast ----
        if (_toastActive)
        {
            float t = _toastT / MathF.Max(0.001f, _toastDuration);

            // punch-in / punch-out alpha
            float a;
            if (t < 0.2f) a = t / 0.2f;
            else if (t > 0.85f) a = (1f - t) / 0.15f;
            else a = 1f;

            a = MathHelper.Clamp(a, 0f, 1f);

            var r = new Rectangle(60, 110, 360, 50);

            sb.Draw(_white, r, new Color(10, 10, 18) * (0.85f * a));
            sb.Draw(_white, new Rectangle(r.X, r.Y, r.Width, 2), Color.White * (0.35f * a));
            sb.Draw(_white, new Rectangle(r.X, r.Bottom - 2, r.Width, 2), Color.White * (0.25f * a));

            DrawTextCentered8x8(sb, _s.Font, _toastText, r, Color.White * a, scale: 2);
        }

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

    private static void DrawTextCentered8x8(SpriteBatch sb, BitmapFont font, string text, Rectangle r, Color color, int scale)
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
