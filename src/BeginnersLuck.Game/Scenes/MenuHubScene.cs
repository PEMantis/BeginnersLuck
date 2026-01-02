using System;
using System.Collections.Generic;
using System.ComponentModel;
using BeginnersLuck.Engine.Input;
using BeginnersLuck.Engine.Rendering;
using BeginnersLuck.Engine.UI;
using BeginnersLuck.Engine.Update;
using BeginnersLuck.Game.Menu;
using BeginnersLuck.Game.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Game.Scenes;

public sealed class MenuHubScene : PanelSceneBase
{
    private enum HubFocus
    {
        Tabs,
        Page
    }

    private readonly List<IMenuPage> _pages = new();
    private int _tabIndex;

    // Layout inside ContentRect()
    private Rectangle _tabsRect;
    private Rectangle _contentRect;

    // Focus state
    private bool _focusTabs = true;
    private HubFocus _focus = HubFocus.Tabs;

    public MenuHubScene(GameServices s, Microsoft.Xna.Framework.Input.KeyboardState keyboard, int startTab = 0)
        : base(
            s,
            panelRect: new Rectangle(32, 22, 576, 316), // tuned for 640x360
            title: "MENU")
    {
        _tabIndex = startTab;

        TitleScale = 2;
        PanelPadding = 14;

        FooterHint = "UP/DOWN: TAB   BACK/B: CLOSE";
        ShowFooterHint = true;
    }

    protected override void OnLoad(GraphicsDevice graphicsDevice, Microsoft.Xna.Framework.Content.ContentManager content)
    {
        // Register pages (order matters)
        _pages.Clear();

        // ✅ Inventory removed from Hub
        _pages.Add(new InventoryPage());
        _pages.Add(new CharacterPage());
        _pages.Add(new StubPage("SKILLS", "Active + passive skills later."));
        _pages.Add(new StubPage("MAGIC", "Spells, mana, spellbook later."));
        _pages.Add(new StubPage("JOURNAL", "Quests + notes later."));

        // Default to CHARACTER if startTab was 0 (old inventory tab) or out of range
        if (_tabIndex <= 0) _tabIndex = 0;
        _tabIndex = Math.Clamp(_tabIndex, 0, Math.Max(0, _pages.Count - 1));

        if (_pages.Count > 0)
            _pages[_tabIndex].OnEnter(S);

        _focusTabs = true;
        _focus = HubFocus.Tabs;
    }

    protected override void OnUnload()
    {
        if (_pages.Count > 0 && _tabIndex >= 0 && _tabIndex < _pages.Count)
            _pages[_tabIndex].OnExit(S);

        _pages.Clear();
    }

    protected override void OnUpdate(UpdateContext uc)
    {
        if (_pages.Count == 0) return;

        // PanelSceneBase already handles Cancel -> Pop, so we DON'T duplicate it here.
        // We only do hub-specific navigation and page update.

        int prevTab = _tabIndex;

        // If you want “focus switching” later (Tabs <-> Page), wire it here.
        // For now: up/down always changes tabs, like your current behavior.

        var up = uc.Actions.Get(GameAction.MoveUp);
        if (up.Pressed || up.Repeated)
            _tabIndex = Math.Max(0, _tabIndex - 1);

        var dn = uc.Actions.Get(GameAction.MoveDown);
        if (dn.Pressed || dn.Repeated)
            _tabIndex = Math.Min(_pages.Count - 1, _tabIndex + 1);

        if (_tabIndex != prevTab)
        {
            _pages[prevTab].OnExit(S);
            _pages[_tabIndex].OnEnter(S);
        }

        _pages[_tabIndex].Update(S, uc);
    }

    protected override void DrawPanelContent(SpriteBatch sb, Rectangle content, RenderContext rc)
    {
        if (White == null || _pages.Count == 0) return;

        float t = (float)rc.GameTime.TotalGameTime.TotalSeconds;

        // Split content into left tabs + right page
        int gap = 10;
        int tabsW = 190;

        _tabsRect = new Rectangle(content.X, content.Y, tabsW, content.Height);
        _contentRect = new Rectangle(content.X + tabsW + gap, content.Y, content.Width - tabsW - gap, content.Height);

        // Backgrounds
        sb.Draw(White, _tabsRect, new Color(10, 10, 18) * 0.35f);
        sb.Draw(White, _contentRect, new Color(10, 10, 18) * 0.25f);

        // Focus outline cues
        var tabsOutline = _focusTabs ? Color.White * 0.55f : Color.White * 0.18f;
        var pageOutline = !_focusTabs ? Color.White * 0.55f : Color.White * 0.18f;

        MenuRenderer.DrawOutline(sb, White, _tabsRect, 2, tabsOutline);
        MenuRenderer.DrawOutline(sb, White, _contentRect, 2, pageOutline);

        // Tabs list
        int rowH = 24;
        int y = _tabsRect.Y + 12;

        for (int i = 0; i < _pages.Count; i++)
        {
            bool sel = i == _tabIndex;
            bool active = sel && _focusTabs;

            if (sel)
            {
                float a = active ? 0.28f : 0.16f;
                sb.Draw(White, new Rectangle(_tabsRect.X + 8, y - 3, _tabsRect.Width - 16, 20), new Color(80, 140, 255) * a);
                sb.Draw(White, new Rectangle(_tabsRect.X + 8, y - 3, 2, 20), new Color(140, 200, 255) * (active ? 0.70f : 0.45f));
            }

            S.TitleFont.Draw(
                sb,
                _pages[i].Title.ToUpperInvariant(),
                new Vector2(_tabsRect.X + 18, y),
                Color.White * (sel ? 0.95f : 0.70f),
                1);

            y += rowH;
        }

        // Page header
        S.TitleFont.Draw(
            sb,
            _pages[_tabIndex].Title.ToUpperInvariant(),
            new Vector2(_contentRect.X + 12, _contentRect.Y + 10),
            Color.White * 0.90f,
            2);

        // Page inner content rect (below header)
        var inner = new Rectangle(
            _contentRect.X + 10,
            _contentRect.Y + 36,
            _contentRect.Width - 20,
            _contentRect.Height - 46);

        _pages[_tabIndex].Draw(S, sb, inner, t);
    }
}
