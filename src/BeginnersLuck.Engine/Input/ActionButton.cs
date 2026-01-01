namespace BeginnersLuck.Engine.Input;

public readonly struct ActionButton
{
    public ActionButton(bool down, bool pressed, bool released, bool repeated)
    {
        Down = down;
        Pressed = pressed;
        Released = released;
        Repeated = repeated;
    }

    public bool Down { get; }
    public bool Pressed { get; }
    public bool Released { get; }

    // Pressed OR repeat-fire while held
    public bool Repeated { get; }
}