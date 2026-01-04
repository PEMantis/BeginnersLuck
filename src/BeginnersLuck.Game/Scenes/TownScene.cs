using System;
using BeginnersLuck.Engine.Graphics;
using BeginnersLuck.Engine.Rendering;
using BeginnersLuck.Engine.Scenes;
using BeginnersLuck.Engine.UI;
using BeginnersLuck.Engine.Update;
using BeginnersLuck.Game.Services;
using BeginnersLuck.Game.State;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BeginnersLuck.Game.Scenes;

public sealed class TownScene : SceneBase
{
    private readonly GameServices _s;
    private readonly Point _worldTile;

    private KeyboardState _prevKs;
    private GamePadState _prevPad;

    private int _sel;
    private TownState? _town;

    private const string PotionId = "potion";
    private const int PotionCost = 10;

    private const string SlimeGelId = "slime_gel";
    private const int SlimeGelSell = 5;

    private static readonly string[] Items =
    {
        "Buy Potion (10g)",
        "Use Potion",
        "Sell Slime Gel (+5g)",
        "Rest (heal to full)",
        "Leave"
    };

    public TownScene(GameServices s, Point worldTile)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
        _worldTile = worldTile;
    }

    public override void Load(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _sel = 0;
        _town = _s.World.GetTown(_worldTile);
    }

    public override void Unload()
    {
    }

    public override void Update(UpdateContext uc)
    {
        var ks = Keyboard.GetState();
        var pad = GamePad.GetState(PlayerIndex.One);

        // Back
        if (Pressed(pad, Buttons.B) || Pressed(ks, Keys.Escape))
        {
            _s.Scenes.Pop();
            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        // Nav
        if (Pressed(pad, Buttons.DPadUp) || Pressed(ks, Keys.Up) || Pressed(ks, Keys.W))
            _sel = (_sel - 1 + Items.Length) % Items.Length;

        if (Pressed(pad, Buttons.DPadDown) || Pressed(ks, Keys.Down) || Pressed(ks, Keys.S))
            _sel = (_sel + 1) % Items.Length;

        // Confirm
        if (Pressed(pad, Buttons.A) || Pressed(ks, Keys.Enter) || Pressed(ks, Keys.Space))
        {
            switch (_sel)
            {
                case 0:
                    BuyPotion();
                    break;

                case 1:
                    UsePotion();
                    break;

                case 2:
                    SellSlimeGel();
                    break;

                case 3:
                    DoRest();
                    break;

                case 4:
                    _s.Scenes.Pop();
                    break;
            }
        }

        _prevKs = ks;
        _prevPad = pad;
    }

    private TownState Town()
        => _town ??= _s.World.GetTown(_worldTile);

    private void DoRest()
    {
        var t = Town();

        int before = _s.Player.Hp;
        _s.Player.Heal(_s.Player.MaxHp); // heal-to-full using your existing API

        t.TimesRested++;

        int healed = _s.Player.Hp - before;
        _s.Toasts.Push(healed > 0 ? $"Rested. +{healed} HP" : "Rested (already full).", 0.8f);
    }

    private void BuyPotion()
    {
        var t = Town();

        if (!t.HasShop)
        {
            _s.Toasts.Push("No shop here.", 0.8f);
            return;
        }

        if (!_s.Items.TryGet(PotionId, out var def))
        {
            _s.Toasts.Push("Shop error: potion missing in ItemDb.", 1.2f);
            return;
        }

        if (_s.Player.Gold < PotionCost)
        {
            _s.Toasts.Push("Not enough gold.", 0.8f);
            return;
        }

        _s.Player.AddGold(-PotionCost);
        _s.Player.Inventory.Add(PotionId, 1);

        t.TimesPurchased++;

        _s.Toasts.Push($"Bought {def.Name}.", 0.8f);
    }

    private void UsePotion()
    {
        // Use system handles qty checks + removal + heal
        if (_s.ItemUse.TryUse(PotionId, out var msg))
            _s.Toasts.Push(msg, 0.9f);
        else
            _s.Toasts.Push(msg, 0.9f);
    }

    private void SellSlimeGel()
    {
        var t = Town();

        if (!t.HasShop)
        {
            _s.Toasts.Push("No shop here.", 0.8f);
            return;
        }

        if (!_s.Player.Inventory.Remove(SlimeGelId, 1))
        {
            _s.Toasts.Push("You have no Slime Gel.", 0.8f);
            return;
        }

        _s.Player.AddGold(SlimeGelSell);
        t.TimesPurchased++;

        _s.Toasts.Push($"+{SlimeGelSell}g (sold Slime Gel).", 0.8f);
    }

    protected override void DrawUI(RenderContext rc)
    {
        var sb = rc.SpriteBatch;

        sb.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);

        // Dim overlay
        sb.Draw(_s.PixelWhite,
            new Rectangle(0, 0, PixelRenderer.InternalWidth, PixelRenderer.InternalHeight),
            Color.Black * 0.55f);

        // Panel
        int panelW = 350;
        int panelH = 205;
        var panel = new Rectangle(
            (PixelRenderer.InternalWidth - panelW) / 2,
            (PixelRenderer.InternalHeight - panelH) / 2,
            panelW,
            panelH);

        sb.Draw(_s.PixelWhite, panel, new Color(10, 10, 18) * 0.92f);
        DrawBorder(sb, panel, Color.White * 0.25f);

        var town = Town();

        int potions = _s.Player.Inventory.CountOf(PotionId);
        int gels = _s.Player.Inventory.CountOf(SlimeGelId);

        _s.UiFont.Draw(sb, $"Town ({_worldTile.X},{_worldTile.Y})  seed:{town.Seed}",
            new Vector2(panel.X + 12, panel.Y + 10),
            Color.White * 0.95f, scale: 1);

        _s.UiFont.Draw(sb, $"HP: {_s.Player.Hp}/{_s.Player.MaxHp}   GOLD: {_s.Player.Gold}",
            new Vector2(panel.X + 12, panel.Y + 26),
            Color.White * 0.85f, scale: 1);

        _s.UiFont.Draw(sb, $"Potions: {potions}   Slime Gel: {gels}",
            new Vector2(panel.X + 12, panel.Y + 38),
            Color.White * 0.75f, scale: 1);

        // Show “can use potion?” hint
        string hint = "";
        if (!_s.ItemUse.CanUse(PotionId, out hint))
            hint = hint switch { "NONE" => "No potions.", "NOT USABLE" => "Potion unusable.", "UNKNOWN" => "Potion missing.", _ => hint };

        _s.UiFont.Draw(sb, potions > 0 ? "Potion: usable" : $"Potion: {hint}",
            new Vector2(panel.X + 12, panel.Y + 50),
            Color.White * 0.65f, scale: 1);

        _s.UiFont.Draw(sb, $"Rested:{town.TimesRested}  Trades:{town.TimesPurchased}",
            new Vector2(panel.X + 12, panel.Y + 62),
            Color.White * 0.65f, scale: 1);

        _s.UiFont.Draw(sb, "D-Pad: select   A: confirm   B: back",
            new Vector2(panel.X + 12, panel.Y + 76),
            Color.White * 0.7f, scale: 1);

        int y = panel.Y + 104;
        for (int i = 0; i < Items.Length; i++)
        {
            bool focused = i == _sel;

            // Disable “Use Potion” visually if cannot use
            bool enabled = true;
            if (i == 1)
                enabled = _s.ItemUse.CanUse(PotionId, out _);

            var itemR = new Rectangle(panel.X + 12, y, panelW - 24, 22);
            sb.Draw(_s.PixelWhite, itemR,
                focused ? new Color(70, 70, 120) * (enabled ? 0.9f : 0.55f)
                        : new Color(25, 25, 40) * (enabled ? 0.9f : 0.55f));

            DrawBorder(sb, itemR, Color.White * (focused ? 0.35f : 0.2f));

            _s.UiFont.Draw(sb, Items[i],
                new Vector2(itemR.X + 10, itemR.Y + 6),
                Color.White * (enabled ? (focused ? 0.95f : 0.8f) : 0.45f),
                scale: 1);

            y += 26;
        }

        sb.End();
    }

    private void DrawBorder(SpriteBatch sb, Rectangle r, Color c)
    {
        sb.Draw(_s.PixelWhite, new Rectangle(r.X, r.Y, r.Width, 1), c);
        sb.Draw(_s.PixelWhite, new Rectangle(r.X, r.Y + r.Height - 1, r.Width, 1), c);
        sb.Draw(_s.PixelWhite, new Rectangle(r.X, r.Y, 1, r.Height), c);
        sb.Draw(_s.PixelWhite, new Rectangle(r.X + r.Width - 1, r.Y, 1, r.Height), c);
    }

    private bool Pressed(KeyboardState ks, Keys k) => ks.IsKeyDown(k) && !_prevKs.IsKeyDown(k);
    private bool Pressed(GamePadState pad, Buttons b) => pad.IsButtonDown(b) && !_prevPad.IsButtonDown(b);
}
