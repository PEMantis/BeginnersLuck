using System;
using System.Collections.Generic;
using BeginnersLuck.Engine.Graphics;
using BeginnersLuck.Engine.Rendering;
using BeginnersLuck.Engine.Scenes;
using BeginnersLuck.Engine.UI;
using BeginnersLuck.Engine.Update;
using BeginnersLuck.Game.Battles;
using BeginnersLuck.Game.Encounters;
using BeginnersLuck.Game.Services;
using BeginnersLuck.Game.UI;
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
        LevelUp,
        VictorySummary,
        Defeat,
        Exit
    }

    private enum ExitReason { None, Fled, Victory, Defeat }

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

    // Battle result + UI state
    private BattleResult? _result;
    private ExitReason _exitReason = ExitReason.None;

    private int _lootScroll;
    private float _payoutT;

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

        _result = null;
        _exitReason = ExitReason.None;

        _lootScroll = 0;
        _payoutT = 0f;

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
            {
                if (_phaseT >= 0.55f || PressedConfirm(ks, pad))
                    GoTo(Phase.PlayerSelect);
                break;
            }

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

                        EnsureBattleResultBuilt(BattleOutcome.Fled);
                        _exitReason = ExitReason.Fled;

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
                    EnterVictoryFlow();
                    break;
                }

                int dmg = _s.Rng.Next(3, 7); // 3-6
                target.Hp = Math.Max(0, target.Hp - dmg);

                _message = $"YOU HIT {target.Name.ToUpperInvariant()} FOR {dmg}!";
                _messageT = 0.9f;

                if (AllEnemiesDown())
                    EnterVictoryFlow();
                else
                    GoTo(Phase.EnemyResolve);

                break;
            }

            case Phase.EnemyResolve:
            {
                if (_phaseT < 0.20f) break;

                var attacker = FirstLivingEnemy();
                if (attacker == null)
                {
                    EnterVictoryFlow();
                    break;
                }

                int dmg = _s.Rng.Next(2, 6); // 2-5
                _playerHp = Math.Max(0, _playerHp - dmg);

                _message = $"{attacker.Name.ToUpperInvariant()} HITS YOU FOR {dmg}!";
                _messageT = 0.9f;

                if (_playerHp <= 0)
                {
                    EnsureBattleResultBuilt(BattleOutcome.Defeat);
                    _exitReason = ExitReason.Defeat;
                    GoTo(Phase.Defeat);
                }
                else
                {
                    GoTo(Phase.PlayerSelect);
                }

                break;
            }

            case Phase.LevelUp:
            {
                if (PressedConfirm(ks, pad))
                    GoTo(Phase.VictorySummary);
                break;
            }

            case Phase.VictorySummary:
            {
                // purely visual
                _payoutT += dt;

                var loot = _result?.Loot ?? Array.Empty<LootLine>();

                if (PressedUp(ks, pad)) _lootScroll = Math.Max(0, _lootScroll - 1);
                if (PressedDown(ks, pad)) _lootScroll = Math.Min(Math.Max(0, loot.Count - 1), _lootScroll + 1);

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
                // Give a tiny grace period so accidental double-press doesn’t insta-pop
                if (_phaseT >= 0.20f && PressedConfirm(ks, pad))
                {
                    _s.Scenes.Pop();
                    return;
                }

                // Auto-exit after a moment for non-victory reasons (flee/defeat)
                if (_phaseT >= 0.75f && _exitReason != ExitReason.Victory)
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

    private void EnterVictoryFlow()
    {
        EnsureBattleResultBuilt(BattleOutcome.Victory);
        BattleResultApplier.Apply(_s, _result!);

        _exitReason = ExitReason.Victory;

        _message = "VICTORY!";
        _messageT = 999f;

        // If the apply generated a level-up report, interrupt first
        if (_result!.XpReport != null && _result.XpReport.LevelsGained > 0)
            GoTo(Phase.LevelUp);
        else
            GoTo(Phase.VictorySummary);
    }

    protected override void DrawWorld(RenderContext rc)
    {
        // UI-driven battle for now
    }

    protected override void DrawUI(RenderContext rc)
    {
        if (_white == null) return;

        if (_screen.Width != PixelRenderer.InternalWidth || _screen.Height != PixelRenderer.InternalHeight)
            ComputeLayout();

        var sb = rc.SpriteBatch;
        float t = (float)rc.GameTime.TotalGameTime.TotalSeconds;

        sb.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);

        // Backplate + vignette
        sb.Draw(_white, _screen, new Color(10, 12, 22) * 1.0f);
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
                    Phase.LevelUp => "ENTER/A: CONTINUE",
                    Phase.VictorySummary => "ENTER/A: LEAVE  |  UP/DOWN: LOOT",
                    Phase.Defeat => "ENTER/A: CONTINUE",
                    Phase.Exit => "ENTER/A: LEAVE",
                    _ => "ENTER/A: CONTINUE"
                };

            int scale = 1;
            var size = _s.TitleFont.Measure(hint, scale);

            int x = _panelCommands.X + 18;
            int y = _panelCommands.Bottom - 12 - size.Y; // 12px padding from bottom

            _s.TitleFont.Draw(sb, hint, new Vector2(x, y), Color.White * 0.75f, scale);
        }

        if (_phase == Phase.LevelUp)
        {
            DrawLevelUpPanel(sb);
            if (_messageT > 0f) DrawMessage(sb);
            sb.End();
            return;
        }

        if (_phase == Phase.VictorySummary)
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

        int margin = 18;
        int gap = 16;

        int usableW = w - margin * 2;
        int usableH = h - margin * 2;

        int topH = (int)(usableH * 0.62f);
        int bottomH = usableH - topH - gap;

        int colW = (usableW - gap) / 2;

        int x0 = margin;
        int x1 = margin + colW + gap;
        int y0 = margin;
        int y1 = margin + topH + gap;

        _panelLeft = new Rectangle(x0, y0, colW, topH);
        _panelRight = new Rectangle(x1, y0, colW, topH);

        _panelCommands = new Rectangle(x0, y1, colW, bottomH);
        _panelInfo = new Rectangle(x1, y1, colW, bottomH);

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

    private void DrawLevelUpPanel(SpriteBatch sb)
    {
        const int pad = 16;

        var r = new Rectangle(
            _panelInfo.X + pad,
            _panelInfo.Y + pad,
            _panelInfo.Width - pad * 2,
            _panelInfo.Height - pad * 2);

        MenuRenderer.DrawPanel(sb, _white!, r, new Color(22, 18, 36) * 0.98f);

        int x = r.X + 14;
        int y = r.Y + 12;

        _s.TitleFont.Draw(sb, "LEVEL UP!", new Vector2(x, y), Color.Gold * 0.95f, 1);
        y += _s.TitleFont.LineHeight(1) + 6;

        var rep = _result?.XpReport;
        if (rep == null)
        {
            _s.UiFont.Draw(sb, "LEVEL INCREASED", new Vector2(x, y), Color.White * 0.9f, 1);
            return;
        }

        _s.UiFont.Draw(sb, $"LEVEL {rep.OldLevel} → {rep.NewLevel}", new Vector2(x, y), Color.White * 0.9f, 1);
        y += _s.UiFont.LineHeight(1) + 4;

        _s.UiFont.Draw(sb, $"LEVELS GAINED: {rep.LevelsGained}", new Vector2(x, y), Color.White * 0.75f, 1);

        y = r.Bottom - 26;
        _s.UiFont.Draw(sb, "ENTER / A : CONTINUE", new Vector2(x, y), Color.White * 0.65f, 1);
    }

    private void DrawVictorySummary(SpriteBatch sb)
    {
        if (_result == null) return;

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

        float xpK = MathHelper.Clamp(_payoutT / 0.6f, 0f, 1f);
        float goldK = MathHelper.Clamp((_payoutT - 0.6f) / 0.6f, 0f, 1f);

        int shownXp = (int)MathF.Round(_result.Xp * xpK);
        int shownGold = (int)MathF.Round(_result.Gold * goldK);

        Line($"XP:   +{shownXp}", 0.85f);
        Line($"GOLD: +{shownGold}", 0.85f);

        y += 2;

        if (_result.XpReport != null && _result.XpReport.LevelsGained > 0)
        {
            Line($"LEVEL UP! +{_result.XpReport.LevelsGained}", 0.90f);
            Line($"NOW LEVEL: {_result.XpReport.NewLevel}", 0.80f);
            y += 2;
        }

        Line("LOOT:", 0.75f);

        var loot = _result.Loot;
        if (loot.Count == 0)
        {
            Line("(NONE)", 0.75f);
            return;
        }

        int start = Math.Clamp(_lootScroll, 0, Math.Max(0, loot.Count - 1));

        int remainingPx = inner.Bottom - y;
        int lh = _s.UiFont.LineHeight(1);
        int maxLines = Math.Max(1, remainingPx / lh);

        int shown = 0;
        for (int i = start; i < loot.Count && shown < maxLines; i++)
        {
            var (id, qty) = loot[i];

            var name = _s.Items.DisplayNameOf(id);
            var rarity = _s.Items.RarityOf(id);
            var color = RarityColors.For(rarity);
            var label = RarityColors.Label(rarity);

            string text = $"{name} x{qty}{(string.IsNullOrEmpty(label) ? "" : " " + label)}";
            string fitted = Fit(text);

            if (rarity >= BeginnersLuck.Game.Items.ItemRarity.Rare)
            {
                var glow = new Rectangle(inner.X - 2, y - 1, inner.Width + 4, lh);
                sb.Draw(_white!, glow, color * 0.10f);
            }

            _s.UiFont.Draw(sb, fitted, new Vector2(x, y), color * 0.90f, 1);

            y += lh;
            shown++;
        }

        if (start > 0 && (inner.Bottom - y) >= lh)
            Line("(MORE ABOVE)", 0.55f);

        if (start + shown < loot.Count && (inner.Bottom - y) >= lh)
            Line("(MORE BELOW)", 0.55f);
    }

    private void DrawPlayerPanel(SpriteBatch sb)
    {
        _s.TitleFont.Draw(sb, "PERRY", new Vector2(_panelLeft.X + 12, _panelLeft.Y + 12), Color.White * 0.9f, 1);


        var hpText = $"HP {_playerHp}/{_playerMaxHp}";
        _s.UiFont.Draw(sb, hpText, new Vector2(_panelLeft.X + 12, _panelLeft.Y + 38), Color.White * 0.9f, 1);

        var bar = new Rectangle(_panelLeft.X + 12, _panelLeft.Y + 56, _panelLeft.Width - 24, 12);
        DrawBar(sb, bar, _playerHp, _playerMaxHp, back: new Color(30, 30, 45), fill: new Color(80, 220, 120));
    }

    private void DrawEnemiesPanel(SpriteBatch sb)
    {
        _s.TitleFont.Draw(sb, _encounter.Name.ToUpperInvariant(), new Vector2(_panelRight.X + 12, _panelRight.Y + 12), Color.White * 0.9f, 1);


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

    private void DrawMessage(SpriteBatch sb)
    {
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

        if (p != Phase.VictorySummary)
            _payoutT = 0f;
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

    private void EnsureBattleResultBuilt(BattleOutcome outcome)
    {
        if (_result != null) return;

        int xp = 0;
        int gold = 0;
        var loot = new List<LootLine>();

        if (outcome == BattleOutcome.Victory)
        {
            xp = _encounter.Xp.Roll(_s.Rng);
            gold = _encounter.Gold.Roll(_s.Rng);

            foreach (var d in _encounter.Loot)
            {
                if (!_s.Items.TryGet(d.ItemId, out _))
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine(
                        $"[Loot] Unknown item id '{d.ItemId}' in encounter '{_encounter.Id}'");
#endif
                    continue;
                }

                int roll = _s.Rng.Next(1, 101);
                if (roll > d.ChancePercent) continue;

                int qty = d.MinQty;
                if (d.MaxQty > d.MinQty)
                    qty = _s.Rng.Next(d.MinQty, d.MaxQty + 1);

                loot.Add(new LootLine(d.ItemId, qty));
            }

            loot.Sort((a, b) => _s.Items.RarityOf(b.ItemId).CompareTo(_s.Items.RarityOf(a.ItemId)));
        }

        _result = new BattleResult
        {
            EncounterId = _encounter.Id,
            EncounterName = _encounter.Name,
            Outcome = outcome,
            Xp = xp,
            Gold = gold,
            Loot = loot
        };
    }
}
