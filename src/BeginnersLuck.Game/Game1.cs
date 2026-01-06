using BeginnersLuck.Engine.Graphics;
using BeginnersLuck.Engine.Scenes;
using BeginnersLuck.Engine.Transitions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using BeginnersLuck.Engine.Input;
using BeginnersLuck.Engine.Update;
using BeginnersLuck.Engine.Content;
using BeginnersLuck.Engine.UI;
using BeginnersLuck.Game.Services;
using BeginnersLuck.Game.Encounters;
using System;
using BeginnersLuck.Game.Items;
using BeginnersLuck.Game.State;
using BeginnersLuck.Game.Graphics;
namespace BeginnersLuck.Game;

public class Game1 : Microsoft.Xna.Framework.Game
{
    private readonly GraphicsDeviceManager _graphics;

    private PixelRenderer _pixel = null!;
    private SceneManager _scenes = null!;
    private FadeTransition _fade = null!;
    private InputHost _input = null!;
    private ActionMap _actions = null!;
    private RawContent _raw = null!;
    public IFont Font { get; private set; } = null!;
    private GameServices _services = null!;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);

        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.ApplyChanges();
    }

    protected override void Initialize()
    {
        _pixel = new PixelRenderer(GraphicsDevice);
        _pixel.OnBackBufferChanged(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);

        Window.ClientSizeChanged += (_, __) =>
        {
            _pixel.OnBackBufferChanged(
                GraphicsDevice.PresentationParameters.BackBufferWidth,
                GraphicsDevice.PresentationParameters.BackBufferHeight);
        };

        _input = new InputHost();
        _actions = ActionMap.CreateDefault();

        _scenes = new SceneManager();
        _fade = new FadeTransition();

        // ✅ Consume input whenever stack transitions (Replace/Push/Pop)
        _scenes.OnTransition = () => _actions.ConsumeAll();

        base.Initialize();
    }


    protected override void LoadContent()
    {
        _raw = new RawContent(GraphicsDevice);
       var ttf = _raw.LoadBytes("Fonts/ui.ttf");

        // Title: still big, but controlled
        var titleFont = new RuntimeTtfFont(GraphicsDevice, ttf, pixelHeight: 10);
        titleFont.ExtraSpacingX = 1;
        titleFont.ExtraSpacingY = 3;

        // UI: smaller, for menu items / body
        var uiFont = new RuntimeTtfFont(GraphicsDevice, ttf, pixelHeight: 8);
        uiFont.ExtraSpacingX = 1;
        uiFont.ExtraSpacingY = 2;

        var buttonFont = new RuntimeTtfFont(GraphicsDevice, ttf, pixelHeight: 6);
        buttonFont.ExtraSpacingX = 1;
        buttonFont.ExtraSpacingY = 1;

        var rng = new Random(12345);
        var encounterSource = new BasicEncounterSource();
        var encounterDirector = new EncounterDirector(encounterSource);
        var player = new BeginnersLuck.Game.State.CharacterState();
        var items = BeginnersLuck.Game.Items.DefaultItems.Create();
        var world = new WorldState { WorldSeed = 777 };

        var px = new Texture2D(GraphicsDevice, 1, 1);
        px.SetData(new[] { Color.White });

        var toasts = new BeginnersLuck.Game.UI.ToastQueue();
        var sprites = new BeginnersLuck.Game.Graphics.SpriteDb(_raw);
        sprites.Register("world.rock", "Sprites/World/rock_32x32.png");
        sprites.Register("world.tree", "Sprites/World/tree_32x48.png");
        sprites.Register("world.mountain", "Sprites/World/mountain_64x64.png");
        sprites.Register("world.ruin_pillar", "Sprites/World/ruin_pillar_32x48.png");
        sprites.Register("world.player", "Sprites/World/player_16x16.png");
        // Roads (32x32 overlay sprites)
        sprites.Register("world.road.dot", "Sprites/World/Road/road_dot_32x32.png");
        sprites.Register("world.road.end_n", "Sprites/World/Road/road_end_n_32x32.png");
        sprites.Register("world.road.end_e", "Sprites/World/Road/road_end_e_32x32.png");
        sprites.Register("world.road.end_s", "Sprites/World/Road/road_end_s_32x32.png");
        sprites.Register("world.road.end_w", "Sprites/World/Road/road_end_w_32x32.png");
        sprites.Register("world.road.straight_h", "Sprites/World/Road/road_straight_h_32x32.png");
        sprites.Register("world.road.straight_v", "Sprites/World/Road/road_straight_v_32x32.png");
        sprites.Register("world.road.corner_ne", "Sprites/World/Road/road_corner_ne_32x32.png");
        sprites.Register("world.road.corner_nw", "Sprites/World/Road/road_corner_nw_32x32.png");
        sprites.Register("world.road.corner_se", "Sprites/World/Road/road_corner_se_32x32.png");
        sprites.Register("world.road.corner_sw", "Sprites/World/Road/road_corner_sw_32x32.png");
        sprites.Register("world.road.t_n", "Sprites/World/Road/road_t_n_32x32.png");
        sprites.Register("world.road.t_e", "Sprites/World/Road/road_t_e_32x32.png");
        sprites.Register("world.road.t_s", "Sprites/World/Road/road_t_s_32x32.png");
        sprites.Register("world.road.t_w", "Sprites/World/Road/road_t_w_32x32.png");
        sprites.Register("world.road.cross", "Sprites/World/Road/road_cross_32x32.png");

        // your existing: rng, encounterDirector, player, itemDb, raw, font
        _services = new BeginnersLuck.Game.Services.GameServices(
            pixel: _pixel,
            scenes: _scenes,
            fade: _fade,
            raw: _raw,
            uiFont: uiFont,
            titleFont: titleFont,
            buttonFont: buttonFont,
            pixelWhite: px,
            toasts: toasts,
            rng: rng,
            encounters: encounterDirector,
            player: player,
            items: items,
            world: world,
            sprites: sprites
        );

        // avoid circular dependency by setting after
        _services.ItemUse = new BeginnersLuck.Game.Items.ItemUseSystem(_services);


        _scenes.Configure(GraphicsDevice, Content);
        _scenes.Replace(new Scenes.BootScene(_services));
        _scenes.OnTransition = () => _actions.ConsumeAll();

    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update(_pixel);

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _actions.Update(_input.Snapshot, dt);

        var uc = new UpdateContext(gameTime, _input.Snapshot, _actions);

        // Hard-exit routed through ActionMap (single vocabulary)
        if (uc.Actions.System.Quit.Pressed)
            Exit();

        _fade.Update(gameTime);
        _scenes.Update(uc);

        base.Update(gameTime);
    }


    protected override void Draw(GameTime gameTime)
    {
        _pixel.BeginWorld(Color.CornflowerBlue);
        _input.Update(_pixel);
        // Escape OR controller Back can be handled as Cancel/Pause depending on your preference
        // if (_actions.Pressed(_input.Snapshot, GameAction.Pause) ||
        //     _input.Snapshot.KeyPressed(Keys.Escape)) // keep this if you want hard-exit on PC
        // {
        //     // optional: bring up pause instead of Exit
        //     // Exit();
        // }

        var rc = new BeginnersLuck.Engine.Rendering.RenderContext(gameTime, _pixel, _pixel.SpriteBatch);

        _scenes.Current?.Draw(rc);

        // Transition overlays scene + UI
        _fade.Draw(rc);

        _pixel.EndWorldToScreen(Color.Black);

        base.Draw(gameTime);
    }


}
