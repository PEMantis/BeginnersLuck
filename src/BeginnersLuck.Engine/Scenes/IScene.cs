using BeginnersLuck.Engine.Rendering;
using BeginnersLuck.Engine.Update;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Engine.Scenes;

public interface IScene
{
    void Load(GraphicsDevice graphicsDevice, ContentManager content);
    void Update(UpdateContext uc);
    void Draw(RenderContext rc);
    void Unload();
}
