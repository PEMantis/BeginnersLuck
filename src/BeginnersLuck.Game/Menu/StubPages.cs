using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using BeginnersLuck.Game.Services;

namespace BeginnersLuck.Game.Menu;

public sealed class StubPage : IMenuPage
{
    public string Title { get; }

    private readonly string _body;

    public StubPage(string title, string body)
    {
        Title = title;
        _body = body;
    }

    public void OnEnter(GameServices s) { }
    public void OnExit(GameServices s) { }
    public void Update(GameServices s, float dt) { }

    public void Draw(GameServices s, SpriteBatch sb, Rectangle contentRect, float timeSeconds)
    {
        s.Font.Draw(sb, Title, new Vector2(contentRect.X + 12, contentRect.Y + 10), Color.White * 0.9f, 2);
        s.Font.Draw(sb, _body, new Vector2(contentRect.X + 12, contentRect.Y + 34), Color.White * 0.65f, 1);
    }
}
