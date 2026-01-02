using System;
using System.Collections.Generic;
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
    private readonly EncounterDef _encounter;
    private readonly Enemy[] _enemies;

    private Texture2D? _white;

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

    // Rewards
    private bool _rewardsRolled;
    private bool _rewardsApplied;
    private int _rewardXp;
    private int _rewardGold;
    private readonly List<(string ItemId, int Qty)> _rewardLoot = new();

    // Victory entry guard
    private bool _victoryEntered;

    // --------- Dynamic layout (computed from InternalWidth/Height) ----------
    private Rectangle _screen;
    private Rectangle _panelLeft;
    private Rectangle _panelRight;
    private Rectangle _panelCommands;
    private Rectangle _panelInfo;
    private Rectangle _btnAttack;
    private Rectangle _btnRun;

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

        _rewardsRolled = false;
        _rewardsApplied = false;
        _rewardXp = 0;
        _rewardGold = 0;
        _rewardLoot.Clear();

        _victoryEntered = false;
        _eatFirstUpdate = true;

        ComputeLayout();
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
                if (PressedUp(ks, pad)) _focus = 0;
                if (PressedDown(ks, pad)) _focus = 1;

                if (PressedConfirm(ks, pad))
                {
                    if (_focus == 0)
                    {
                        GoTo(Phase.PlayerResolve);
                    }
                    else
                    {
                        _message = "YOU FLED!";
                        _messageT = 0.8f;
                        GoTo(Phase.Exit);
                    }
                }

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

                if (AllEnemiesDown()) GoTo(Phase.Victory);
                else GoTo(Phase.EnemyResolve);

                break;
            }

            case Phase.EnemyResolve:
            {
                if (_phaseT < 0.20f) break;

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

                if (_playerHp <= 0) GoTo(Phase.Defeat);
                else GoTo(Phase.PlayerSelect);

                break;
            }

            case Phase.Victory:
            {
                RollRewardsIfNeeded();
                ApplyRewardsIfNeeded();

                if (!_victoryEntered)
                {
                    _victoryEntered = true;
                    _message = "VICTORY!";
                    _messageT = 999f;
                }

                if (PressedConfirm(ks, pad))
                    GoTo(Phase.Exit);

                break;
            }

            case Phase.Defeat:
            {
                if (_messageT <= 0f)
                {
                    _message = "DEFEAT...";
                    _messageT = 999f;
                }

                if (PressedConfirm(ks, pad))
                    GoTo(Phase.Exit);

                break;
            }

            case Phase.Exit:
            {
                if (_phaseT >= 0.15f && PressedConfirm(ks, pad))
                {
                    _s.Scenes.Pop();
                    return;
                }

                if (_phaseT >= 0.65f && (_message.StartsWith("VICTORY") || _message.StartsWith("YOU FLED")))
                {
                    _s.Scenes.Pop();
                    return;
                }

                break;
            }
        }

        _prevKs = ks;
        _prevPad = pad;
    }

    protected override void DrawWorld(RenderContext rc)
    {
        // UI-driven battle for now
    }

    protected override void DrawUI(RenderContext rc)
    {
        if (_white == null) return;

        // In case you ever change InternalWidth/Height later, keep layout in sync:
        if (_screen.Width != PixelRenderer.InternalWidth || _screen.Height != PixelRenderer.InternalHeight)
            ComputeLayout();

        var sb = rc.SpriteBatch;
        float t = (float)rc.GameTime.TotalGameTime.TotalSeconds;

        sb.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);

        // Full-screen backplate (prevents “blue void” if world isn't drawn)
        sb.Draw(_white, _screen, new Color(10, 12, 22) * 1.0f);

        // Subtle vignette/dim
        sb.Draw(_white, _screen, Color.Black * 0.22f);

        // Panels
        MenuRenderer.DrawPanel(sb, _white, _panelLeft, new Color(18, 18, 34) * 0.98f);
        MenuRenderer.DrawPanel(sb, _white, _panelRight, new Color(18, 18, 34) * 0.98f);
        MenuRenderer.DrawPanel(sb, _white, _panelCommands, new Color(12, 12, 20) * 0.98f);
        MenuRenderer.DrawPanel(sb, _white, _panelInfo, new Color(12, 12, 20) * 0.98f);

        DrawPlayerPanel(sb);
        DrawEnemiesPanel(sb);

        bool showCommands = _phase == Phase.PlayerSelect;

        if (showCommands)
        {
            MenuRenderer.DrawButton(
                sb, _white, _s.ButtonFont, _btnAttack, "ATTACK",
                focused: _focus == 0,
                enabled: true,
                timeSeconds: t,
                fontScale: 2);

            MenuRenderer.DrawButton(
                sb, _white, _s.ButtonFont, _btnRun, "RUN",
                focused: _focus == 1,
                enabled: true,
                timeSeconds: t,
                fontScale: 2);
        }
        else
        {
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

        if (_phase == Phase.Victory)
        {
            DrawVictorySummary(sb);
        }
        else
        {
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

        if (_messageT > 0f)
            DrawMessage(sb);

        sb.End();
    }

    // ---------------- Layout ----------------

    private void ComputeLayout()
    {
        int w = PixelRenderer.InternalWidth;
        int h = PixelRenderer.InternalHeight;

        _screen = new Rectangle(0, 0, w, h);

        // Tuning knobs
        int margin = 18;
        int gap = 16;

        // Two rows: top row (status panels), bottom row (commands/info)
        int usableW = w - margin * 2;
        int usableH = h - margin * 2;

        // Top row height + bottom row height
        int topH = (int)(usableH * 0.62f);
        int bottomH = usableH - topH - gap;

        // Two columns
        int colW = (usableW - gap) / 2;

        int x0 = margin;
        int x1 = margin + colW + gap;
        int y0 = margin;
        int y1 = margin + topH + gap;

        _panelLeft = new Rectangle(x0, y0, colW, topH);
        _panelRight = new Rectangle(x1, y0, colW, topH);

        _panelCommands = new Rectangle(x0, y1, colW, bottomH);
        _panelInfo = new Rectangle(x1, y1, colW, bottomH);

        // Buttons inside commands panel
        int pad = 14;
        int btnW = _panelCommands.Width - pad * 2;
        int btnH = 30;
        int btnGap = 10;

        int bx = _panelCommands.X + pad;
        int by = _panelCommands.Y + pad + 6;

        _btnAttack = new Rectangle(bx, by, btnW, btnH);
        _btnRun = new Rectangle(bx, by + btnH + btnGap, btnW, btnH);
    }

    // ---------------- Drawing helpers ----------------

    private void DrawPlayerPanel(SpriteBatch sb)
    {
        _s.TitleFont.Draw(sb, "HERO", new Vector2(_panelLeft.X + 12, _panelLeft.Y + 12), Color.White * 0.9f, 1);

        var hpText = $"HP {_playerHp}/{_playerMaxHp}";
        _s.UiFont.Draw(sb, hpText, new Vector2(_panelLeft.X + 12, _panelLeft.Y + 38), Color.White * 0.9f, 1);

        var bar = new Rectangle(_panelLeft.X + 12, _panelLeft.Y + 56, _panelLeft.Width - 24, 12);
        DrawBar(sb, bar, _playerHp, _playerMaxHp, back: new Color(30, 30, 45), fill: new Color(80, 220, 120));
    }

    private void DrawEnemiesPanel(SpriteBatch sb)
    {
        _s.TitleFont.Draw(sb, "ENEMIES", new Vector2(_panelRight.X + 12, _panelRight.Y + 12), Color.White * 0.9f, 1);

        int y = _panelRight.Y + 38;

        for (int i = 0; i < _enemies.Length; i++)
        {
            var e = _enemies[i];

            var name = e.Alive ? e.Name.ToUpperInvariant() : $"{e.Name.ToUpperInvariant()} (KO)";
            var c = e.Alive ? Color.White * 0.92f : Color.White * 0.35f;

            _s.UiFont.Draw(sb, name, new Vector2(_panelRight.X + 12, y), c, 1);

            var bar = new Rectangle(_panelRight.X + 12, y + 14, _panelRight.Width - 24, 10);
            DrawBar(sb, bar, e.Hp, e.MaxHp, back: new Color(30, 30, 45), fill: new Color(230, 90, 90));

            y += 34;
            if (y > _panelRight.Bottom - 24) break;
        }
    }

    private void DrawVictorySummary(SpriteBatch sb)
    {
        const int pad = 10;

        var inner = new Rectangle(
            _panelInfo.X + pad,
            _panelInfo.Y + pad,
            _panelInfo.Width - pad * 2,
            _panelInfo.Height - pad * 2);

        int x = inner.X;
        int y = inner.Y;

        int maxW = inner.Width;

        string Fit(string text)
            => TrimToWidth(_s.UiFont, text.ToUpperInvariant(), maxW, 1);

        void Line(string text, float alpha = 0.85f)
        {
            _s.UiFont.Draw(sb, Fit(text), new Vector2(x, y), Color.White * alpha, 1);
            y += _s.UiFont.LineHeight(1);
        }

        _s.TitleFont.Draw(sb, "REWARDS", new Vector2(x, y), Color.White * 0.9f, 1);
        y += _s.TitleFont.LineHeight(1);

        Line($"XP:   +{_rewardXp}", 0.85f);
        Line($"GOLD: +{_rewardGold}", 0.85f);

        y += 2;
        Line("LOOT:", 0.75f);

        if (_rewardLoot.Count == 0)
        {
            Line("(NONE)", 0.75f);
            return;
        }

        int remainingPx = inner.Bottom - y;
        int lh = _s.UiFont.LineHeight(1);
        int maxLines = Math.Max(1, remainingPx / lh);

        int shown = 0;
        for (int i = 0; i < _rewardLoot.Count && shown < maxLines; i++)
        {
            var (id, qty) = _rewardLoot[i];
            var name = _s.Items.NameOf(id);
            Line($"{name} x{qty}", 0.75f);
            shown++;
        }

        if (_rewardLoot.Count > shown && (inner.Bottom - y) >= lh)
            Line($"(+{_rewardLoot.Count - shown} MORE)", 0.60f);
    }

    private void DrawMessage(SpriteBatch sb)
    {
        // Width relative to screen now, centered
        int w = _screen.Width;
        var r = new Rectangle((w / 2) - 190, 8, 380, 18);

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
        int bw = (int)(r.Width * p);

        if (bw > 0)
            sb.Draw(_white!, new Rectangle(r.X, r.Y, bw, r.Height), fill);

        sb.Draw(_white!, new Rectangle(r.X, r.Y, r.Width, 1), Color.White * 0.08f);
    }

    // ---------------- Logic helpers ----------------

    private void GoTo(Phase p)
    {
        _phase = p;
        _phaseT = 0f;

        if (p != Phase.Victory)
            _victoryEntered = false;
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

    private bool Pressed(KeyboardState ks, Keys k) => ks.IsKeyDown(k) && !_prevKs.IsKeyDown(k);
    private bool Pressed(GamePadState pad, Buttons b) => pad.IsButtonDown(b) && !_prevPad.IsButtonDown(b);

    private bool PressedUp(KeyboardState ks, GamePadState pad)
        => Pressed(ks, Keys.Up) || Pressed(ks, Keys.W) || Pressed(pad, Buttons.DPadUp);

    private bool PressedDown(KeyboardState ks, GamePadState pad)
        => Pressed(ks, Keys.Down) || Pressed(ks, Keys.S) || Pressed(pad, Buttons.DPadDown);

    private bool PressedConfirm(KeyboardState ks, GamePadState pad)
        => Pressed(ks, Keys.Enter) || Pressed(ks, Keys.Space) || Pressed(pad, Buttons.A);

    private static void DrawTextCentered(SpriteBatch sb, IFont font, string text, Rectangle r, Color color, int scale)
    {
        var size = font.Measure(text, scale);
        var pos = new Vector2(
            r.X + (r.Width - size.X) * 0.5f,
            r.Y + (r.Height - size.Y) * 0.5f);

        font.Draw(sb, text, pos, color, scale);
    }

    private static string TrimToWidth(IFont font, string text, int maxWidth, int scale)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (font.Measure(text, scale).X <= maxWidth) return text;

        const string ell = "...";
        int ellW = font.Measure(ell, scale).X;
        int target = Math.Max(0, maxWidth - ellW);

        int lo = 0, hi = text.Length;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) / 2;
            var s = text.Substring(0, mid);
            if (font.Measure(s, scale).X <= target) lo = mid;
            else hi = mid - 1;
        }

        return (lo <= 0) ? ell : text.Substring(0, lo) + ell;
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
