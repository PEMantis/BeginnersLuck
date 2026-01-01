using Microsoft.Xna.Framework.Input;

namespace BeginnersLuck.Engine.Input;

public readonly record struct ActionBinding(Keys[] Keys, Buttons[] Buttons)
{
    public static ActionBinding Keyboard(params Keys[] keys) => new(keys, Array.Empty<Buttons>());
    public static ActionBinding Pad(params Buttons[] buttons) => new(Array.Empty<Keys>(), buttons);
    public static ActionBinding Both(Keys[] keys, Buttons[] buttons) => new(keys, buttons);
}
