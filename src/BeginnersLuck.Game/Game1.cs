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
    public BitmapFont Font { get; private set; } = null!;
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

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _raw = new RawContent(GraphicsDevice);
        var fontTex = _raw.LoadTexture("Fonts/font8x8.png");
        var font = new BitmapFont(fontTex, 8, 8, 16, 32);

        var rng = new Random(12345);
        var encounterSource = new BasicEncounterSource();
        var encounterDirector = new EncounterDirector(encounterSource);
        var player = new BeginnersLuck.Game.State.PlayerState();
        var items = BeginnersLuck.Game.Items.DefaultItems.Create();

        var px = new Texture2D(GraphicsDevice, 1, 1);
        px.SetData(new[] { Color.White });

        var toasts = new BeginnersLuck.Game.UI.ToastQueue();

        // your existing: rng, encounterDirector, player, itemDb, raw, font
        _services = new BeginnersLuck.Game.Services.GameServices(
            pixel: _pixel,
            scenes: _scenes,
            fade: _fade,
            raw: _raw,
            font: font,
            pixelWhite: px,
            toasts: toasts,
            rng: rng,
            encounters: encounterDirector,
            player: player,
            items: items
        );

        // avoid circular dependency by setting after
        _services.ItemUse = new BeginnersLuck.Game.Items.ItemUseSystem(_services);


        _scenes.Configure(GraphicsDevice, Content);
        _scenes.Replace(new Scenes.BootScene(_services));

    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update(_pixel);

        // Hard-exit (optional). Or map this to Pause/Cancel.
        if (_input.Snapshot.KeyPressed(Keys.F10))
            Exit();

        var uc = new UpdateContext(gameTime, _input.Snapshot, _actions);

        _fade.Update(gameTime);
        _scenes.Update(uc);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _pixel.BeginWorld(Color.CornflowerBlue);
        _input.Update(_pixel);
        // Escape OR controller Back can be handled as Cancel/Pause depending on your preference
        if (_actions.Pressed(_input.Snapshot, GameAction.Pause) ||
            _input.Snapshot.KeyPressed(Keys.Escape)) // keep this if you want hard-exit on PC
        {
            // optional: bring up pause instead of Exit
            // Exit();
        }

        var rc = new BeginnersLuck.Engine.Rendering.RenderContext(gameTime, _pixel, _pixel.SpriteBatch);

        _scenes.Current?.Draw(rc);

        // Transition overlays scene + UI
        _fade.Draw(rc);

        _pixel.EndWorldToScreen(Color.Black);

        base.Draw(gameTime);
    }


}
