using BeginnersLuck.Engine.Update;
using BeginnersLuck.Game.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Game.Menu;

public interface IMenuPage
{
    string Title { get; }
    string FooterHint { get; }

    // Called when tab becomes active/inactive
    void OnEnter(GameServices s);
    void OnExit(GameServices s);

    // Called every frame while active
    void Update(GameServices s, in UpdateContext uc);

    // Draw inside the content rectangle passed by MenuHubScene
    void Draw(GameServices s, SpriteBatch sb, Rectangle contentRect, float timeSeconds);
}
