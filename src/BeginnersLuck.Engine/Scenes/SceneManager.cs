using BeginnersLuck.Engine.Rendering;
using BeginnersLuck.Engine.Update;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Engine.Scenes;

public sealed class SceneManager
{
    private readonly List<IScene> _stack = new();

    private GraphicsDevice _graphicsDevice = null!;
    private ContentManager _content = null!;

    // Optional hook invoked after any stack transition (Replace/Push/Pop).
    // Game1 can set this to consume input, play sounds, etc.
    public Action? OnTransition { get; set; }

    public void Configure(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _graphicsDevice = graphicsDevice;
        _content = content;
    }

    public IScene? Current => _stack.Count > 0 ? _stack[^1] : null;
    public IReadOnlyList<IScene> Stack => _stack;

    // Backwards compatible name (your old code calls Switch)
    public void Switch(IScene scene) => Replace(scene);

    public void Replace(IScene scene)
    {
        // unload everything
        for (int i = _stack.Count - 1; i >= 0; i--)
            _stack[i].Unload();

        _stack.Clear();

        _stack.Add(scene);
        scene.Load(_graphicsDevice, _content);

        OnTransition?.Invoke();
    }

    public void Push(IScene scene)
    {
        _stack.Add(scene);
        scene.Load(_graphicsDevice, _content);

        OnTransition?.Invoke();
    }

    public void Pop()
    {
        if (_stack.Count == 0) return;

        var top = _stack[^1];
        top.Unload();
        _stack.RemoveAt(_stack.Count - 1);

        OnTransition?.Invoke();
    }

    public void Update(UpdateContext uc)
    {
        // Update only the top scene
        Current?.Update(uc);
    }

    public void Draw(RenderContext rc)
    {
        // Draw from bottom -> top so overlays render last
        for (int i = 0; i < _stack.Count; i++)
            _stack[i].Draw(rc);
    }
}
