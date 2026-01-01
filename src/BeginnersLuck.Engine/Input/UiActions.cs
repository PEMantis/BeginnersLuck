namespace BeginnersLuck.Engine.Input;

public sealed class UiActions
{
    public ActionButton Up { get; internal set; }
    public ActionButton Down { get; internal set; }
    public ActionButton Left { get; internal set; }
    public ActionButton Right { get; internal set; }

    public ActionButton Confirm { get; internal set; }
    public ActionButton Cancel { get; internal set; }
    public ActionButton Menu { get; internal set; }
}