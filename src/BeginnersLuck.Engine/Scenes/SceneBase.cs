using BeginnersLuck.Engine.Rendering;
using BeginnersLuck.Engine.Update;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Engine.Scenes;

public abstract class SceneBase : IScene
{
    public abstract void Load(GraphicsDevice graphicsDevice, ContentManager content);
    public abstract void Update(UpdateContext uc);
    public abstract void Unload();

    public virtual void Draw(RenderContext rc)
    {
        DrawWorld(rc);
        DrawUI(rc);
    }

    protected virtual void DrawWorld(RenderContext rc) { }
    protected virtual void DrawUI(RenderContext rc) { }
}
