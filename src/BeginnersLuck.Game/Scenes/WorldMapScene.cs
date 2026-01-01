using BeginnersLuck.Engine;
using BeginnersLuck.Engine.Graphics;
using BeginnersLuck.Engine.Rendering;
using BeginnersLuck.Engine.Scenes;
using BeginnersLuck.Engine.Transitions;
using BeginnersLuck.Engine.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using BeginnersLuck.Engine.Input;
using BeginnersLuck.Engine.Update;
using System.Text.Json;
using BeginnersLuck.Engine.World;
using System;
using System.Collections.Generic;
using BeginnersLuck.Game.Services;

namespace BeginnersLuck.Game.Scenes;

public sealed class MapDto
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int TileSize { get; set; }
    public string Tileset { get; set; } = "";
    public int[] Solid { get; set; } = Array.Empty<int>();
    public int[] Tiles { get; set; } = Array.Empty<int>();

    public TriggerDto[] Triggers { get; set; } = Array.Empty<TriggerDto>();
}

public sealed class TriggerDto
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Type { get; set; } = ""; // "town", "encounter", ...
    public string Id { get; set; } = "";
}


public sealed class WorldMapScene : SceneBase
{
    private Texture2D? _white;
    private readonly Camera2D _cam = new();

    public WorldMapScene(GameServices s) { _s = s; }
    private readonly GameServices _s;

    private TileMap? _map;
    private TileSet? _tileset;
    private TileMapRenderer? _mapRenderer;

    private Point _playerCell = new(2, 2);
    private KeyboardState _prevKs;
    private GamePadState _prevPad;

    private readonly Dictionary<Point, MapTrigger> _triggers = new();

    // Encounter toast
    private bool _toastActive;
    private float _toastT;
    private float _toastDuration = 0.55f;
    private string _toastText = "";
    private BeginnersLuck.Game.Encounters.EncounterDef _toastEncounter;
    private KeyboardState _toastSeedKs;
    private GamePadState _toastSeedPad;

    private bool Pressed(KeyboardState ks, Keys k) => ks.IsKeyDown(k) && !_prevKs.IsKeyDown(k);

    private bool Pressed(GamePadState pad, Buttons b)
        => pad.IsButtonDown(b) && !_prevPad.IsButtonDown(b);

    public override void Load(GraphicsDevice graphicsDevice, ContentManager content)
    {
        // OLD
        _white = new Texture2D(graphicsDevice, 1, 1);
        _white.SetData(new[] { Color.White });
        // _cam.Position = Vector2.Zero;
        var json = _s.Raw.LoadText("Data/map_test.json");
        var dto = JsonSerializer.Deserialize<MapDto>(json,
             new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        if (string.IsNullOrWhiteSpace(dto.Tileset))
            throw new InvalidOperationException("Map JSON missing 'tileset' (expected e.g. \"Textures/tiles.png\").");

        _map = new TileMap(dto.Width, dto.Height, dto.TileSize, dto.Tiles);

        foreach (var id in dto.Solid)
            _map.SetSolid(id, true);

        _s.Zones = BeginnersLuck.Game.World.ZoneMap.GenerateFromTiles(
            _map.Width,
            _map.Height,
            (x, y) => _map.GetTileId(x, y),
            seed: 12345,
            zoneSizeCells: 8
        );

        // load tileset texture
        var tex = _s.Raw.LoadTexture(dto.Tileset);
        _tileset = new TileSet(tex, dto.TileSize);
        _mapRenderer = new TileMapRenderer(_tileset);
        _triggers.Clear();

        foreach (var t in dto.Triggers)
        {
            if (string.IsNullOrWhiteSpace(t.Type)) continue;

            var type = t.Type.Trim().ToLowerInvariant() switch
            {
                "town" => TriggerType.Town,
                "encounter" => TriggerType.Encounter,
                "teleport" => TriggerType.Teleport,
                "message" => TriggerType.Message,
                _ => TriggerType.Message
            };

            var p = new Point(t.X, t.Y);
            _triggers[p] = new MapTrigger(t.X, t.Y, type, t.Id ?? "");
        }

        _cam.Position = Vector2.Zero;
    }

    public override void Unload()
    {
        _white?.Dispose();
        _white = null;
    }

    public override void Update(UpdateContext uc)
    {
        if (_map == null) return;

        var ks = Keyboard.GetState();
        var pad = GamePad.GetState(PlayerIndex.One);

        // 1) Pause (keyboard Esc or controller Start/Back) - edge-trigger
        if (Pressed(ks, Keys.Escape) || Pressed(pad, Buttons.Start) || Pressed(pad, Buttons.Back))
        {
            _s.Scenes.Push(new PauseScene(_s, ks, pad));
            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        // 2) Direction input (edge-trigger, one step)
        Point dir = Point.Zero;

        // keyboard edge-trigger
        if (Pressed(ks, Keys.W) || Pressed(ks, Keys.Up)) dir = new Point(0, -1);
        else if (Pressed(ks, Keys.S) || Pressed(ks, Keys.Down)) dir = new Point(0, 1);
        else if (Pressed(ks, Keys.A) || Pressed(ks, Keys.Left)) dir = new Point(-1, 0);
        else if (Pressed(ks, Keys.D) || Pressed(ks, Keys.Right)) dir = new Point(1, 0);

        // controller dpad edge-trigger (only if keyboard didn't pick a dir)
        if (dir == Point.Zero)
        {
            if (Pressed(pad, Buttons.DPadUp)) dir = new Point(0, -1);
            else if (Pressed(pad, Buttons.DPadDown)) dir = new Point(0, 1);
            else if (Pressed(pad, Buttons.DPadLeft)) dir = new Point(-1, 0);
            else if (Pressed(pad, Buttons.DPadRight)) dir = new Point(1, 0);
        }

        // If toast is active, tick it and block world input
        if (_toastActive)
        {
            _toastT += (float)uc.GameTime.ElapsedGameTime.TotalSeconds;

            if (_toastT >= _toastDuration && !_s.Fade.Active)
            {
                _toastActive = false;

                // Fade into battle right after toast
                _s.Fade.Start(0.25f, () =>
                {
                    _s.Scenes.Push(new BattleScene(_s, _toastEncounter, _toastSeedKs, _toastSeedPad));
                });
            }

            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        bool moved = false;

        if (dir != Point.Zero)
        {
            var next = _playerCell + dir;

            // collision
            if (!_map.IsSolidCell(next.X, next.Y))
            {
                _playerCell = next;
                moved = true;
            }
        }

        // 3) POI triggers (optional)
        // If you want these to be “static” POIs (town entrances, dungeon doors),
        // keep them here. We run them BEFORE random encounters so you don't step
        // onto a town tile and get ambushed first.
        if (moved && _triggers != null && _triggers.TryGetValue(_playerCell, out var trig))
        {
            HandleTrigger(trig, ks, pad);
            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        // 4) Zone-based encounter roll (only after successful move)
        if (moved && _s.Zones != null)
        {
            var zone = _s.Zones.GetInfo(_playerCell.X, _playerCell.Y);

            var intent = _s.EncounterDirector.OnPlayerMoved(_playerCell, zone, _s.Rng);
            if (intent.HasValue && !_s.Fade.Active)
            {
                var i = intent.Value;
                _s.Fade.Start(0.25f, () =>
                {
                    _s.Scenes.Push(new BattleScene(_s, i.Encounter, ks, pad));
                });

                _prevKs = ks;
                _prevPad = pad;
                return;
            }
        }


        // 5) Camera follow
        _cam.Position = _map.CellToWorldCenter(_playerCell);

        // 6) Update input history once
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

        // Build a view rectangle in WORLD PIXELS.
        // Since Camera2D likely uses translation based on Position, simplest is:
        var view = new Rectangle(
            (int)(_cam.Position.X - PixelRenderer.InternalWidth * 0.5f),
            (int)(_cam.Position.Y - PixelRenderer.InternalHeight * 0.5f),
            PixelRenderer.InternalWidth,
            PixelRenderer.InternalHeight);


        _mapRenderer.Draw(sb, _map, view);

        // Player marker
        //if (_white != null)
        //    sb.Draw(_white!, new Rectangle(0, 0, 12, 12), Color.Gold);
        var pos = _map.CellToWorldTopLeft(_playerCell);
        sb.Draw(_white!, new Rectangle((int)pos.X + 10, (int)pos.Y + 10, 12, 12), Color.Gold);

        sb.End();
    }

    protected override void DrawUI(RenderContext rc)
    {
        if (_white == null) return;

        var sb = rc.SpriteBatch;

        sb.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);

        // Existing UI panel (keep yours)
        sb.Draw(_white, new Rectangle(10, 10, 220, 70), new Color(30, 30, 50) * 0.9f);
        sb.Draw(_white, new Rectangle(10, 10, 220, 2), Color.White * 0.5f);

        // ---- Debug overlay ----
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

        sb.End();
    }


    private void HandleTrigger(MapTrigger trig, KeyboardState ks, GamePadState pad)
    {
        switch (trig.Type)
        {
            case TriggerType.Town:
                if (!_s.Fade.Active)
                {
                    _s.Fade.Start(0.25f, () =>
                    {
                        // Push so you can return to map easily
                        _s.Scenes.Push(new TownScene(_s, trig.Id, ks, pad));
                    });
                }
                break;

            default:
                // Later: encounter, teleport, message
                break;
        }
    }

    private void StartEncounterToast(
        BeginnersLuck.Game.Encounters.EncounterDef enc,
        string text,
        KeyboardState ks,
        GamePadState pad)
    {
        _toastActive = true;
        _toastT = 0f;
        _toastText = text;
        _toastEncounter = enc;

        // seed input so BattleScene won't press-through
        _toastSeedKs = ks;
        _toastSeedPad = pad;
    }

}
