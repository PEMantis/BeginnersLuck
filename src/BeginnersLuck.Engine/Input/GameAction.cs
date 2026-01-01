namespace BeginnersLuck.Engine.Input;

public enum GameAction
{
    Confirm,
    Cancel,
    Pause,

    MoveUp,
    MoveDown,
    MoveLeft,
    MoveRight,

    // optional: menu nav vs gameplay can share these
    PageLeft,
    PageRight,
}
