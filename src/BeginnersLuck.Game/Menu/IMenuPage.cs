using BeginnersLuck.Engine.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using BeginnersLuck.Game.Services;

namespace BeginnersLuck.Game.Menu;

public interface IMenuPage
{
    string Title { get; }

    // called once when the hub is created (optional initialization)
    void OnEnter(GameServices s);

    // called when switching away (optional cleanup)
    void OnExit(GameServices s);

    // update page-specific logic (optional)
    void Update(GameServices s, float dt);

    // draw right-side content area
    void Draw(GameServices s, SpriteBatch sb, Rectangle contentRect, float timeSeconds);
}
