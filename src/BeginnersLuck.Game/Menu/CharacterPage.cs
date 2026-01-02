using BeginnersLuck.Game.Services;
using BeginnersLuck.Engine.Update;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using BeginnersLuck.Engine.UI;

namespace BeginnersLuck.Game.Menu;

public sealed class CharacterPage : IMenuPage
{
    public string Title => "CHARACTER";

    public string FooterHint => "BLAH";

    public void OnEnter(GameServices s) { }
    public void OnExit(GameServices s) { }

    public void Update(GameServices s, in UpdateContext uc)
    {
        // Nothing interactive yet (later: stats list, equipment, etc.)
    }

    public void Draw(GameServices s, SpriteBatch sb, Rectangle contentRect, float timeSeconds)
    {
        var white = s.PixelWhite;
        if (white == null) return;

        // Layout
        int pad = 10;
        var inner = new Rectangle(
            contentRect.X + pad,
            contentRect.Y + pad,
            contentRect.Width - pad * 2,
            contentRect.Height - pad * 2
        );

        // Panels
        int gap = 10;
        int leftW = (int)(inner.Width * 0.55f);
        var left = new Rectangle(inner.X, inner.Y, leftW, inner.Height);
        var right = new Rectangle(inner.X + leftW + gap, inner.Y, inner.Width - leftW - gap, inner.Height);

        BeginnersLuck.Engine.UI.MenuRenderer.DrawPanel(sb, white, left, new Color(12, 12, 20) * 0.98f);
        BeginnersLuck.Engine.UI.MenuRenderer.DrawPanel(sb, white, right, new Color(12, 12, 20) * 0.98f);

        DrawVitals(s, sb, white, left);
        DrawSummary(s, sb, white, right);
    }

    private static void DrawVitals(GameServices s, SpriteBatch sb, Texture2D white, Rectangle r)
    {
        int x = r.X + 12;
        int y = r.Y + 12;

        // Name placeholder
        s.TitleFont.Draw(sb, "HERO", new Vector2(x, y), Color.White * 0.90f, 2);
        y += s.TitleFont.LineHeight(2) + 10;

        // HP
        s.UiFont.Draw(sb, $"HP  {s.Player.Hp}/{s.Player.MaxHp}", new Vector2(x, y), Color.White * 0.85f, 1);
        y += s.UiFont.LineHeight(1) + 6;

        var hpBar = new Rectangle(x, y, r.Width - 24, 12);
        DrawBar(sb, white, hpBar, s.Player.Hp, s.Player.MaxHp,
            back: new Color(30, 30, 45),
            fill: new Color(80, 220, 120));
        y += 20;

        // XP / Level
        s.UiFont.Draw(sb, $"LV  {s.Player.Level}", new Vector2(x, y), Color.White * 0.85f, 1);
        y += s.UiFont.LineHeight(1) + 6;

        int need = s.Player.XpToNextLevel();
        s.UiFont.Draw(sb, $"XP  {s.Player.Xp}/{need}", new Vector2(x, y), Color.White * 0.75f, 1);
        y += s.UiFont.LineHeight(1) + 6;

        var xpBar = new Rectangle(x, y, r.Width - 24, 12);
        int cur = s.Player.Xp;
        DrawBar(sb, white, xpBar, cur, need,
            back: new Color(30, 30, 45),
            fill: new Color(120, 160, 255));
    }

    private static void DrawSummary(GameServices s, SpriteBatch sb, Texture2D white, Rectangle r)
    {
        int x = r.X + 12;
        int y = r.Y + 12;

        s.TitleFont.Draw(sb, "SUMMARY", new Vector2(x, y), Color.White * 0.90f, 2);
        y += s.TitleFont.LineHeight(2) + 10;

        s.UiFont.Draw(sb, $"GOLD: {s.Player.Gold}", new Vector2(x, y), Color.White * 0.80f, 1);
        y += s.UiFont.LineHeight(1) + 6;

        // Count inventory items
        int distinct = s.Player.Inventory.Counts.Count;
        int total = 0;
        foreach (var kv in s.Player.Inventory.Counts) total += kv.Value;

        s.UiFont.Draw(sb, $"ITEMS: {distinct} TYPES", new Vector2(x, y), Color.White * 0.75f, 1);
        y += s.UiFont.LineHeight(1) + 6;

        s.UiFont.Draw(sb, $"TOTAL: {total}", new Vector2(x, y), Color.White * 0.75f, 1);
        y += s.UiFont.LineHeight(1) + 12;

        // Light placeholder
        s.UiFont.Draw(sb, "EQUIPMENT COMING SOON.", new Vector2(x, y), Color.White * 0.55f, 1);
    }

    private static void DrawBar(SpriteBatch sb, Texture2D white, Rectangle r, int value, int max, Color back, Color fill)
    {
        sb.Draw(white, r, back);

        if (max <= 0) return;

        float p = MathHelper.Clamp(value / (float)max, 0f, 1f);
        int w = (int)(r.Width * p);

        if (w > 0)
            sb.Draw(white, new Rectangle(r.X, r.Y, w, r.Height), fill);

        // thin highlight
        sb.Draw(white, new Rectangle(r.X, r.Y, r.Width, 1), Color.White * 0.08f);
    }
}
