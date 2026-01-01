using Microsoft.Xna.Framework.Input;

namespace BeginnersLuck.Engine.Input;

/// <summary>
/// Compatibility helpers so older code can keep calling
/// actions.Pressed(Keys.X), actions.Down(Buttons.A), etc.
/// Prefer using actions.Ui.* in new code.
/// </summary>
public static class ActionMapCompat
{
    // Keyboard
    public static bool Down(this ActionMap actions, InputSnapshot input, Keys k)
        => input.IsDown(k);

    public static bool Pressed(this ActionMap actions, InputSnapshot input, Keys k)
        => input.Pressed(k);

    public static bool Released(this ActionMap actions, InputSnapshot input, Keys k)
        => input.Released(k);

    // GamePad
    public static bool Down(this ActionMap actions, InputSnapshot input, Buttons b)
        => input.IsDown(b);

    public static bool Pressed(this ActionMap actions, InputSnapshot input, Buttons b)
        => input.Pressed(b);

    public static bool Released(this ActionMap actions, InputSnapshot input, Buttons b)
        => input.Released(b);
}
