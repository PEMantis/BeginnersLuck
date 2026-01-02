using System;
using BeginnersLuck.Engine.UI;
using BeginnersLuck.Engine.Update;
using BeginnersLuck.Game.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Game.Menu;

public sealed class StubPage : IMenuPage
{
    public string Title { get; }
    public string FooterHint => "BACK/B: CLOSE";

    private readonly string _body;

    public StubPage(string title, string body)
    {
        Title = title;
        _body = body;
    }

    public void OnEnter(GameServices s) { }
    public void OnExit(GameServices s) { }

    public void Draw(GameServices s, SpriteBatch sb, Rectangle r, float t)
    {
        s.UiFont.Draw(sb, _body.ToUpperInvariant(), new Vector2(r.X + 6, r.Y + 6), Color.White * 0.75f, 1);
    }

    public void Update(GameServices s, in UpdateContext uc)
    {
    }
}
