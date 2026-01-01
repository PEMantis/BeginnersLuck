using System;
using BeginnersLuck.Engine.Graphics;
using BeginnersLuck.Engine.Input;
using BeginnersLuck.Engine.Rendering;
using BeginnersLuck.Engine.Scenes;
using BeginnersLuck.Engine.UI;
using BeginnersLuck.Engine.Update;
using BeginnersLuck.Game.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Game.Scenes;

public sealed class PauseScene : SceneBase
{
    private Texture2D? _white;

    private readonly Rectangle _panel        = new(110, 58, 260, 154);
    private readonly Rectangle _btnResume    = new(130, 92,  220, 32);
    private readonly Rectangle _btnInventory = new(130, 132, 220, 32);
    private readonly Rectangle _btnMenu      = new(130, 172, 220, 32);

    // 0 Resume, 1 Inventory, 2 Menu
    private int _focus;

    private readonly GameServices _s;

    // This replaces your old seed + _eatFirstUpdate dance.
    // We consume input on first Update after pushing the scene.
    private bool _consumeOnFirstUpdate = true;

    public PauseScene(GameServices s)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
        _focus = 0;
    }

    public override void Load(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _white = new Texture2D(graphicsDevice, 1, 1);
        _white.SetData(new[] { Color.White });

        _consumeOnFirstUpdate = true;
    }

    public override void Unload()
    {
        _white?.Dispose();
        _white = null;
    }

    public override void Update(UpdateContext uc)
    {
        // Prevent “press-through” from the key/button that opened pause
        if (_consumeOnFirstUpdate)
        {
            _consumeOnFirstUpdate = false;
            uc.Actions.ConsumeAll();
            return;
        }

        // Close pause (Cancel)
        if (uc.Actions.Pressed(uc.Input, GameAction.Cancel))
        {
            _s.Scenes.Pop();
            uc.Actions.ConsumeAll();
            return;
        }

        // Navigate
        if (uc.Actions.Pressed(uc.Input, GameAction.MoveUp))
            _focus = Math.Max(0, _focus - 1);

        if (uc.Actions.Pressed(uc.Input, GameAction.MoveDown))
            _focus = Math.Min(2, _focus + 1);

        // Select
        if (uc.Actions.Pressed(uc.Input, GameAction.Confirm))
        {
            if (_focus == 0)
            {
                _s.Scenes.Pop(); // Resume
                uc.Actions.ConsumeAll();
                return;
            }

            if (_focus == 1)
            {
                // Inventory (your hub scene)
               _s.Scenes.Push(new MenuHubScene(_s, uc.Input.Keyboard, uc.Input.Pad, startTab: 0));
                uc.Actions.ConsumeAll();
                //_s.Scenes.ConsumeInput(uc.Actions);
                return;
            }

            // Menu
            _s.Scenes.Replace(new BootScene(_s));
            uc.Actions.ConsumeAll();
            return;
        }
    }

    protected override void DrawUI(RenderContext rc)
    {
        if (_white == null) return;

        var sb = rc.SpriteBatch;
        float t = (float)rc.GameTime.TotalGameTime.TotalSeconds;

        sb.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);

        // Dim screen
        sb.Draw(_white,
            new Rectangle(0, 0, PixelRenderer.InternalWidth, PixelRenderer.InternalHeight),
            Color.Black * 0.65f);

        // Panel
        MenuRenderer.DrawPanel(sb, _white, _panel, new Color(20, 20, 40) * 0.98f);

        _s.TitleFont.DrawShadow(sb, "PAUSED", new Vector2(_panel.X + 90, _panel.Y + 12), Color.White, scale: 2);

        // Buttons (UiFont here is fine, or ButtonFont if you prefer)
        MenuRenderer.DrawButton(sb, _white, _s.UiFont, _btnResume, "RESUME",
            focused: _focus == 0, enabled: true, timeSeconds: t, fontScale: 2);

        MenuRenderer.DrawButton(sb, _white, _s.UiFont, _btnInventory, "INVENTORY",
            focused: _focus == 1, enabled: true, timeSeconds: t, fontScale: 2);

        MenuRenderer.DrawButton(sb, _white, _s.UiFont, _btnMenu, "MENU",
            focused: _focus == 2, enabled: true, timeSeconds: t, fontScale: 2);

        // Hint line (your Option A spacing looked good)
        _s.UiFont.Draw(sb, "ENTER/A: SELECT   BACK/B: RETURN",
            new Vector2(_panel.X + 24, _panel.Bottom - 18),
            Color.White * 0.8f, scale: 1);

        sb.End();
    }
}
