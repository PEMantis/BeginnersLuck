using BeginnersLuck.Engine.Input;
using Microsoft.Xna.Framework;

namespace BeginnersLuck.Engine.Update;

public readonly struct UpdateContext
{
    public UpdateContext(GameTime gameTime, InputSnapshot input, ActionMap actions)
    {
        GameTime = gameTime;
        Input = input;
        Actions = actions;
    }

    public GameTime GameTime { get; }
    public InputSnapshot Input { get; }
    public ActionMap Actions { get; }
}
