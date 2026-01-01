using System;
using BeginnersLuck.Engine.Graphics;
using BeginnersLuck.Engine.Rendering;
using BeginnersLuck.Engine.Scenes;
using BeginnersLuck.Engine.UI;
using BeginnersLuck.Engine.Update;
using BeginnersLuck.Game.Encounters;
using BeginnersLuck.Game.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BeginnersLuck.Game.Scenes;

public sealed class BattleScene : SceneBase
{
    private enum Phase
    {
        Intro,
        PlayerSelect,
        PlayerResolve,
        EnemyResolve,
        Victory,
        Defeat,
        Exit
    }

    private sealed class Enemy
    {
        public string Id { get; }
        public string Name { get; }
        public int Hp { get; set; }
        public int MaxHp { get; }

        public bool Alive => Hp > 0;

        public Enemy(EnemyDef def)
        {
            Id = def.Id;
            Name = def.Name;
            Hp = def.Hp;
            MaxHp = def.Hp;
        }
    }

    private readonly GameServices _s;
    private Texture2D? _white;

    private readonly EncounterDef _encounter;
    private readonly Enemy[] _enemies;

    // Player (placeholder model for v1)
    private int _playerHp = 30;
    private int _playerMaxHp = 30;

    private Phase _phase = Phase.Intro;
    private float _phaseT;

    // Menu
    private int _focus; // 0=Attack, 1=Run

    // Messages
    private string _message = "";
    private float _messageT;

    // Input edge-trigger
    private KeyboardState _prevKs;
    private GamePadState _prevPad;
    private bool _eatFirstUpdate = true;

    // UI layout (virtual 480x270)
    private readonly Rectangle _panelLeft  = new(16, 28, 216, 150);
    private readonly Rectangle _panelRight = new(248, 28, 216, 150);
    private readonly Rectangle _panelCommands = new(16, 190, 220, 64);
    private readonly Rectangle _panelInfo = new(244, 190, 220, 64);

    private readonly Rectangle _btnAttack = new(26, 202, 180, 22);
    private readonly Rectangle _btnRun = new(26, 230, 180, 22);

    private bool _rewardsRolled;
    private bool _rewardsApplied;
    private int _rewardXp;
    private int _rewardGold;
    private readonly System.Collections.Generic.List<(string ItemId, int Qty)> _rewardLoot = new();


    public BattleScene(GameServices s, EncounterDef encounter, KeyboardState seedKs, GamePadState seedPad)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
        _encounter = encounter;

        _enemies = new Enemy[encounter.Enemies.Length];
        for (int i = 0; i < _enemies.Length; i++)
            _enemies[i] = new Enemy(encounter.Enemies[i]);

        _prevKs = seedKs;
        _prevPad = seedPad;

        _message = $"ENCOUNTER: {encounter.Name.ToUpperInvariant()}!";
        _messageT = 0.9f;
    }

    public override void Load(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _white = new Texture2D(graphicsDevice, 1, 1);
        _white.SetData(new[] { Color.White });

        _phase = Phase.Intro;
        _phaseT = 0f;
        _focus = 0;
    }

    public override void Unload()
    {
        _white?.Dispose();
        _white = null;
    }

    public override void Update(UpdateContext uc)
    {
        var ks = Keyboard.GetState();
        var pad = GamePad.GetState(PlayerIndex.One);

        if (_eatFirstUpdate)
        {
            _eatFirstUpdate = false;
            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        float dt = (float)uc.GameTime.ElapsedGameTime.TotalSeconds;
        _phaseT += dt;

        if (_messageT > 0f)
            _messageT = MathF.Max(0f, _messageT - dt);

        switch (_phase)
        {
            case Phase.Intro:
                if (_phaseT >= 0.55f || PressedConfirm(ks, pad))
                    GoTo(Phase.PlayerSelect);
                break;

            case Phase.PlayerSelect:
            {
                // Navigate menu
                if (PressedUp(ks, pad)) _focus = 0;
                if (PressedDown(ks, pad)) _focus = 1;

                // Confirm
                if (PressedConfirm(ks, pad))
                {
                    if (_focus == 0)
                    {
                        // Attack
                        GoTo(Phase.PlayerResolve);
                    }
                    else
                    {
                        // Run (exit battle immediately)
                        _message = "YOU FLED!";
                        _messageT = 0.8f;
                        GoTo(Phase.Exit);
                    }
                }

                // Optional: Cancel could also run in v1 (but avoid Esc/Back to prevent pause press-through)
                break;
            }

            case Phase.PlayerResolve:
            {
                if (_phaseT < 0.05f) break;

                var target = FirstLivingEnemy();
                if (target == null)
                {
                    GoTo(Phase.Victory);
                    break;
                }

                int dmg = _s.Rng.Next(3, 7); // 3-6
                target.Hp = Math.Max(0, target.Hp - dmg);

                _message = $"YOU HIT {target.Name.ToUpperInvariant()} FOR {dmg}!";
                _messageT = 0.9f;

                if (AllEnemiesDown())
                {
                    GoTo(Phase.Victory);
                }
                else
                {
                    GoTo(Phase.EnemyResolve);
                }
                break;
            }

            case Phase.EnemyResolve:
            {
                if (_phaseT < 0.20f) break;

                // One enemy attack for v1: first living enemy hits player
                var attacker = FirstLivingEnemy();
                if (attacker == null)
                {
                    GoTo(Phase.Victory);
                    break;
                }

                int dmg = _s.Rng.Next(2, 6); // 2-5
                _playerHp = Math.Max(0, _playerHp - dmg);

                _message = $"{attacker.Name.ToUpperInvariant()} HITS YOU FOR {dmg}!";
                _messageT = 0.9f;

                if (_playerHp <= 0)
                    GoTo(Phase.Defeat);
                else
                    GoTo(Phase.PlayerSelect);

                break;
            }

            case Phase.Victory:
                RollRewardsIfNeeded();
                ApplyRewardsIfNeeded();
                _message = "VICTORY!";
                _messageT = MathF.Max(_messageT, 999f); // keep visible
                if (PressedConfirm(ks, pad))
                    GoTo(Phase.Exit);
                break;

            case Phase.Defeat:
                _message = "DEFEAT...";
                _messageT = MathF.Max(_messageT, 999f);
                if (PressedConfirm(ks, pad))
                    GoTo(Phase.Exit);
                break;

            case Phase.Exit:
                // Use Confirm only to leave (avoid Esc/Back press-through)
                if (_phaseT >= 0.15f && PressedConfirm(ks, pad))
                {
                    _s.Scenes.Pop();
                    return;
                }

                // Auto-exit on Victory/Run after a short beat (optional)
                if (_phaseT >= 0.65f && (_message.StartsWith("VICTORY") || _message.StartsWith("YOU FLED")))
                {
                    _s.Scenes.Pop();
                    return;
                }
                break;
        }

        _prevKs = ks;
        _prevPad = pad;
    }

    protected override void DrawWorld(RenderContext rc)
    {
        // Battle is UI-driven for now; world layer can stay empty
    }

    protected override void DrawUI(RenderContext rc)
    {
        if (_white == null) return;

        var sb = rc.SpriteBatch;
        float t = (float)rc.GameTime.TotalGameTime.TotalSeconds;

        sb.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);

        // Background dim
        sb.Draw(_white, new Rectangle(0, 0, PixelRenderer.InternalWidth, PixelRenderer.InternalHeight), Color.Black * 0.75f);

        // Main panels
        MenuRenderer.DrawPanel(sb, _white, _panelLeft, new Color(18, 18, 34) * 0.98f);
        MenuRenderer.DrawPanel(sb, _white, _panelRight, new Color(18, 18, 34) * 0.98f);

        // Bottom row: COMMANDS + INFO
        MenuRenderer.DrawPanel(sb, _white, _panelCommands, new Color(12, 12, 20) * 0.98f);
        MenuRenderer.DrawPanel(sb, _white, _panelInfo, new Color(12, 12, 20) * 0.98f);

        // Left/Right content
        DrawPlayerPanel(sb);
        DrawEnemiesPanel(sb);

        // --- Bottom: Commands or Continue Hint ---
        bool showCommands = _phase == Phase.PlayerSelect;

        if (showCommands)
        {
            MenuRenderer.DrawButton(
                sb, _white, _s.UiFont, _btnAttack, "ATTACK",
                focused: _focus == 0,
                enabled: true,
                timeSeconds: t,
                fontScale: 2);

            MenuRenderer.DrawButton(
                sb, _white, _s.UiFont, _btnRun, "RUN",
                focused: _focus == 1,
                enabled: true,
                timeSeconds: t,
                fontScale: 2);
        }
        else
        {
            // Commands panel becomes a “Continue” area when not selecting actions
            var hint =
                _phase switch
                {
                    Phase.Intro => "ENTER/A: START",
                    Phase.Victory => "ENTER/A: CONTINUE",
                    Phase.Defeat => "ENTER/A: CONTINUE",
                    Phase.Exit => "ENTER/A: LEAVE",
                    _ => "ENTER/A: CONTINUE"
                };

            _s.TitleFont.Draw(
                sb,
                hint,
                new Vector2(_panelCommands.X + 18, _panelCommands.Y + 24),
                Color.White * 0.75f,
                1);
        }

        // --- Bottom-right: Info panel content ---
        if (_phase == Phase.Victory)
        {
            // Victory summary (XP / Gold / Loot)
            DrawVictorySummary(sb);
        }
        else
        {
            // Normal prompt / context
            string prompt =
                _phase switch
                {
                    Phase.Intro => "DEFEAT THEM!",
                    Phase.PlayerSelect => "CHOOSE AN ACTION",
                    Phase.PlayerResolve => "YOU ATTACK...",
                    Phase.EnemyResolve => "ENEMY ATTACKS...",
                    Phase.Defeat => "YOU FELL...",
                    Phase.Exit => "LEAVING BATTLE...",
                    _ => ""
                };

            if (!string.IsNullOrWhiteSpace(prompt))
            {
                _s.UiFont.Draw(
                    sb,
                    prompt.ToUpperInvariant(),
                    new Vector2(_panelInfo.X + 12, _panelInfo.Y + 22),
                    Color.White * 0.75f,
                    1);
            }
        }

        // Message toast (top-center)
        if (_messageT > 0f)
            DrawMessage(sb);

        sb.End();
    }



    private void DrawPlayerPanel(SpriteBatch sb)
    {
        // Header
        _s.UiFont.Draw(sb, "HERO", new Vector2(_panelLeft.X + 10, _panelLeft.Y + 10), Color.White * 0.9f, 2);

        // HP
        var hpText = $"HP {_playerHp}/{_playerMaxHp}";
        _s.UiFont.Draw(sb, hpText, new Vector2(_panelLeft.X + 10, _panelLeft.Y + 36), Color.White * 0.9f, 1);

        // HP bar
        var bar = new Rectangle(_panelLeft.X + 10, _panelLeft.Y + 52, _panelLeft.Width - 20, 10);
        DrawBar(sb, bar, _playerHp, _playerMaxHp, back: new Color(30, 30, 45), fill: new Color(80, 220, 120));
    }

    private void DrawEnemiesPanel(SpriteBatch sb)
    {
        _s.UiFont.Draw(sb, "ENEMIES", new Vector2(_panelRight.X + 10, _panelRight.Y + 10), Color.White * 0.9f, 2);

        int y = _panelRight.Y + 34;
        for (int i = 0; i < _enemies.Length; i++)
        {
            var e = _enemies[i];

            var name = e.Alive ? e.Name.ToUpperInvariant() : $"{e.Name.ToUpperInvariant()} (KO)";
            var c = e.Alive ? Color.White * 0.92f : Color.White * 0.35f;

            _s.UiFont.Draw(sb, name, new Vector2(_panelRight.X + 10, y), c, 1);

            var bar = new Rectangle(_panelRight.X + 10, y + 10, _panelRight.Width - 20, 8);
            DrawBar(sb, bar, e.Hp, e.MaxHp, back: new Color(30, 30, 45), fill: new Color(230, 90, 90));

            y += 22;
            if (y > _panelRight.Bottom - 18) break;
        }
    }
    private void DrawVictorySummary(SpriteBatch sb)
    {
    var font = _s.Font;
        // Inner padding
       var lh = font.LineH(1);
       var pad = font.LineH(1) * 2;

        // Define an inner content rect (safe drawing area)
        var inner = new Rectangle(
            _panelInfo.X + pad,
            _panelInfo.Y + pad,
            _panelInfo.Width - pad * 2,
            _panelInfo.Height - pad * 2);

        int x = inner.X;
        int y = inner.Y;

        // Header
        font.Draw(sb, "REWARDS", new Vector2(x, y), Color.White * 0.9f, scale: 2);
        y += font.LineH(1) * 2; // <-- correct spacing for scale 2

        // Body lines (scale 1)
        int maxW = inner.Width;

        // Helper for a single line that must fit
        void Line(string text, float alpha = 0.85f)
        {
            text = text.ToUpperInvariant();
            text = font.TrimToWidth(text, maxW, scale: 1);
            font.Draw(sb, text, new Vector2(x, y), Color.White * alpha, scale: 1);
            y += lh;
        }

        Line($"XP:   {_rewardXp}", 0.85f);
        Line($"GOLD: {_rewardGold}", 0.85f);

        y += 1; // tiny breathing room
        Line("LOOT:", 0.75f);

        if (_rewardLoot.Count == 0)
        {
            Line("(NONE)", 0.75f);
            return;
        }

        // How many loot lines can we show in the remaining space?
        int remainingPx = inner.Bottom - y;
        int maxLines = Math.Max(1, remainingPx / lh);

        int shown = 0;
        for (int i = 0; i < _rewardLoot.Count && shown < maxLines; i++)
        {
            var (id, qty) = _rewardLoot[i];
            var name = _s.Items.NameOf(id);
            Line($"{name} x{qty}", 0.75f);
            shown++;
        }

        // If we truncated, show a final hint if there's room
        if (_rewardLoot.Count > shown && (inner.Bottom - y) >= lh)
        {
            Line($"(+{_rewardLoot.Count - shown} MORE)", 0.60f);
        }
    }



    private void DrawMenu(SpriteBatch sb, float timeSeconds)
    {
        bool inMenu = _phase == Phase.PlayerSelect;

        // Buttons
        MenuRenderer.DrawButton(sb, _white!, _s.UiFont, _btnAttack, "ATTACK", focused: inMenu && _focus == 0, enabled: inMenu, timeSeconds: timeSeconds, fontScale: 1);
        MenuRenderer.DrawButton(sb, _white!, _s.UiFont, _btnRun,    "RUN",    focused: inMenu && _focus == 1, enabled: inMenu, timeSeconds: timeSeconds, fontScale: 1);

        // Prompt
        string prompt =
            _phase switch
            {
                Phase.Intro => "ENTER/A: START",
                Phase.PlayerSelect => "ENTER/A: SELECT",
                Phase.Victory => "ENTER/A: CONTINUE",
                Phase.Defeat => "ENTER/A: CONTINUE",
                Phase.Exit => "ENTER/A: LEAVE",
                _ => ""
            };

        if (!string.IsNullOrWhiteSpace(prompt))
            _s.UiFont.Draw(
     sb,
     prompt,
     new Vector2(_panelInfo.X + 12, _panelInfo.Y + 22),
     Color.White * 0.75f,
     1
 );

    }

    private void DrawMessage(SpriteBatch sb)
    {
        // Simple toast: small panel at top
        var r = new Rectangle(50, 6, 380, 18);

        sb.Draw(_white!, r, new Color(10, 10, 18) * 0.85f);
        sb.Draw(_white!, new Rectangle(r.X, r.Y, r.Width, 1), Color.White * 0.25f);
        sb.Draw(_white!, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), Color.White * 0.18f);

        DrawTextCentered(sb, _s.UiFont, _message, r, Color.White * 0.92f, scale: 1);
    }

    private void DrawBar(SpriteBatch sb, Rectangle r, int value, int max, Color back, Color fill)
    {
        sb.Draw(_white!, r, back);

        if (max <= 0) return;
        float p = MathHelper.Clamp(value / (float)max, 0f, 1f);
        int w = (int)(r.Width * p);

        if (w > 0)
            sb.Draw(_white!, new Rectangle(r.X, r.Y, w, r.Height), fill);

        // thin highlight line
        sb.Draw(_white!, new Rectangle(r.X, r.Y, r.Width, 1), Color.White * 0.08f);
    }

    private void GoTo(Phase p)
    {
        _phase = p;
        _phaseT = 0f;
    }

    private bool AllEnemiesDown()
    {
        for (int i = 0; i < _enemies.Length; i++)
            if (_enemies[i].Alive) return false;
        return true;
    }

    private Enemy? FirstLivingEnemy()
    {
        for (int i = 0; i < _enemies.Length; i++)
            if (_enemies[i].Alive) return _enemies[i];
        return null;
    }

    // ---- Input helpers (edge-triggered) ----
    private bool Pressed(KeyboardState ks, Keys k) => ks.IsKeyDown(k) && !_prevKs.IsKeyDown(k);
    private bool Pressed(GamePadState pad, Buttons b) => pad.IsButtonDown(b) && !_prevPad.IsButtonDown(b);

    private bool PressedUp(KeyboardState ks, GamePadState pad)
        => Pressed(ks, Keys.Up) || Pressed(ks, Keys.W) || Pressed(pad, Buttons.DPadUp);

    private bool PressedDown(KeyboardState ks, GamePadState pad)
        => Pressed(ks, Keys.Down) || Pressed(ks, Keys.S) || Pressed(pad, Buttons.DPadDown);

    private bool PressedConfirm(KeyboardState ks, GamePadState pad)
        => Pressed(ks, Keys.Enter) || Pressed(ks, Keys.Space) || Pressed(pad, Buttons.A);

    // ---- Text centering (8x8 font) ----
    private static void DrawTextCentered8x8(SpriteBatch sb, IFont font, string text, Rectangle r, Color color, int scale)
    {
        var size = MeasureText8x8(text, scale);
        var pos = new Vector2(
            r.X + (r.Width - size.X) * 0.5f,
            r.Y + (r.Height - size.Y) * 0.5f);

        font.Draw(sb, text, pos, color, scale);
    }

    private static void DrawTextCentered(SpriteBatch sb, IFont font, string text, Rectangle r, Color color, int scale)
    {
        var size = font.Measure(text, scale);
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

    private void RollRewardsIfNeeded()
    {
        if (_rewardsRolled) return;
        _rewardsRolled = true;

        _rewardXp = _encounter.Xp.Roll(_s.Rng);
        _rewardGold = _encounter.Gold.Roll(_s.Rng);

        _rewardLoot.Clear();

        foreach (var d in _encounter.Loot)
        {
            int roll = _s.Rng.Next(1, 101);
            if (roll > d.ChancePercent) continue;

            int qty = d.MinQty;
            if (d.MaxQty > d.MinQty)
                qty = _s.Rng.Next(d.MinQty, d.MaxQty + 1);

            _rewardLoot.Add((d.ItemId, qty));
        }
    }

    private void ApplyRewardsIfNeeded()
    {
        if (_rewardsApplied) return;
        _rewardsApplied = true;

        _s.Player.AddXp(_rewardXp);
        _s.Player.AddGold(_rewardGold);

        foreach (var (id, qty) in _rewardLoot)
            _s.Player.Inventory.Add(id, qty);
    }


}
