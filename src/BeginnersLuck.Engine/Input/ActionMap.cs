using Microsoft.Xna.Framework.Input;

namespace BeginnersLuck.Engine.Input;

public sealed class ActionMap
{
    private readonly Dictionary<GameAction, ActionBinding> _bindings = new();

    public ActionMap Bind(GameAction action, ActionBinding binding)
    {
        _bindings[action] = binding;
        return this;
    }

    public bool Down(in InputSnapshot input, GameAction action)
    {
        if (!_bindings.TryGetValue(action, out var b)) return false;

        foreach (var k in b.Keys)
            if (input.Keyboard.IsKeyDown(k)) return true;

        foreach (var btn in b.Buttons)
            if (input.Pad.IsButtonDown(btn)) return true;

        return false;
    }

    public bool Pressed(in InputSnapshot input, GameAction action)
    {
        if (!_bindings.TryGetValue(action, out var b)) return false;

        foreach (var k in b.Keys)
            if (input.KeyPressed(k)) return true;

        foreach (var btn in b.Buttons)
            if (input.PadPressed(btn)) return true;

        return false;
    }

    public bool Released(in InputSnapshot input, GameAction action)
    {
        if (!_bindings.TryGetValue(action, out var b)) return false;

        foreach (var k in b.Keys)
            if (input.KeyReleased(k)) return true;

        // add if you want pad release queries later
        // foreach (var btn in b.Buttons) ...

        return false;
    }

    public static ActionMap CreateDefault()
    {
        // “Feels right” defaults for keyboard + Xbox-style pad
        return new ActionMap()
            .Bind(GameAction.Confirm, ActionBinding.Both(
                keys: new[] { Keys.Enter, Keys.Space },
                buttons: new[] { Buttons.A }))

            .Bind(GameAction.Cancel, ActionBinding.Both(
                keys: new[] { Keys.Escape, Keys.Back },
                buttons: new[] { Buttons.B }))

            .Bind(GameAction.Pause, ActionBinding.Both(
                keys: new[] { Keys.Tab },
                buttons: new[] { Buttons.Start }))

            .Bind(GameAction.MoveUp, ActionBinding.Both(
                keys: new[] { Keys.W, Keys.Up },
                buttons: new[] { Buttons.DPadUp }))

            .Bind(GameAction.MoveDown, ActionBinding.Both(
                keys: new[] { Keys.S, Keys.Down },
                buttons: new[] { Buttons.DPadDown }))

            .Bind(GameAction.MoveLeft, ActionBinding.Both(
                keys: new[] { Keys.A, Keys.Left },
                buttons: new[] { Buttons.DPadLeft }))

            .Bind(GameAction.MoveRight, ActionBinding.Both(
                keys: new[] { Keys.D, Keys.Right },
                buttons: new[] { Buttons.DPadRight }))

            .Bind(GameAction.MoveLeft, ActionBinding.Both(
                keys: new[] { Keys.A, Keys.Left },
                buttons: new[] { Buttons.DPadLeft }))
                
            .Bind(GameAction.MoveRight, ActionBinding.Both(
                keys: new[] { Keys.D, Keys.Right },
                buttons: new[] { Buttons.DPadRight }));
    }
}
