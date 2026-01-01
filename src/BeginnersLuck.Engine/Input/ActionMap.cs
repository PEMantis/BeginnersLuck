using Microsoft.Xna.Framework.Input;

namespace BeginnersLuck.Engine.Input;

public sealed class ActionMap
{
    // Repeat tuning for directional actions
    public float RepeatDelay { get; set; } = 0.25f;
    public float RepeatRate  { get; set; } = 0.10f;

    // Modern grouped actions
    public UiActions Ui { get; } = new();
    public SystemActions System { get; } = new();

    // Backing store for GameAction mapping (MenuModel expects this)
    private readonly Dictionary<GameAction, ActionButton> _buttons = new();

    private bool _consumeThisFrame;

    private RepeatState _upRep, _downRep, _leftRep, _rightRep;

    public void ConsumeAll() => _consumeThisFrame = true;

    // --- Compatibility API used by MenuModel ---
    public bool Pressed(in InputSnapshot input, GameAction a)
        => Get(a).Pressed;

    public bool Down(in InputSnapshot input, GameAction a)
        => Get(a).Down;

    public bool Released(in InputSnapshot input, GameAction a)
        => Get(a).Released;

    public ActionButton Get(GameAction a)
        => _buttons.TryGetValue(a, out var b) ? b : default;

    public void Update(InputSnapshot input, float dt)
    {
        if (_consumeThisFrame)
        {
            _consumeThisFrame = false;
            _buttons.Clear();

            Ui.Up = default;
            Ui.Down = default;
            Ui.Left = default;
            Ui.Right = default;
            Ui.Confirm = default;
            Ui.Cancel = default;
            Ui.Menu = default;

            System.Quit = default;

            _upRep = default;
            _downRep = default;
            _leftRep = default;
            _rightRep = default;

            return;
        }

        // ----- NAV (digital + dpad + stick handled by MenuModel, but we still provide buttons) -----
        bool upDown = input.IsDown(Keys.Up) || input.IsDown(Keys.W) || input.IsDown(Buttons.DPadUp);
        bool dnDown = input.IsDown(Keys.Down) || input.IsDown(Keys.S) || input.IsDown(Buttons.DPadDown);
        bool lfDown = input.IsDown(Keys.Left) || input.IsDown(Keys.A) || input.IsDown(Buttons.DPadLeft);
        bool rtDown = input.IsDown(Keys.Right) || input.IsDown(Keys.D) || input.IsDown(Buttons.DPadRight);

        // Repeat-enabled for UI navigation
        var upBtn = MakeRepeatButton(upDown, dt, ref _upRep);
        var dnBtn = MakeRepeatButton(dnDown, dt, ref _downRep);
        var lfBtn = MakeRepeatButton(lfDown, dt, ref _leftRep);
        var rtBtn = MakeRepeatButton(rtDown, dt, ref _rightRep);

        Ui.Up = upBtn;
        Ui.Down = dnBtn;
        Ui.Left = lfBtn;
        Ui.Right = rtBtn;

        _buttons[GameAction.MoveUp] = upBtn;
        _buttons[GameAction.MoveDown] = dnBtn;
        _buttons[GameAction.MoveLeft] = lfBtn;
        _buttons[GameAction.MoveRight] = rtBtn;

        // ----- ACTIONS -----
        bool confirmDown = input.IsDown(Keys.Enter) || input.IsDown(Keys.Space) || input.IsDown(Buttons.A);
        bool confirmWas  = input.WasDown(Keys.Enter) || input.WasDown(Keys.Space) || input.WasDown(Buttons.A);
        var confirmBtn   = MakeButton(confirmDown, confirmWas);

        bool cancelDown = input.IsDown(Keys.Escape) || input.IsDown(Buttons.B) || input.IsDown(Buttons.Back);
        bool cancelWas  = input.WasDown(Keys.Escape) || input.WasDown(Buttons.B) || input.WasDown(Buttons.Back);
        var cancelBtn   = MakeButton(cancelDown, cancelWas);

        bool menuDown = input.IsDown(Keys.Tab) || input.IsDown(Buttons.Start);
        bool menuWas  = input.WasDown(Keys.Tab) || input.WasDown(Buttons.Start);
        var menuBtn   = MakeButton(menuDown, menuWas);

        bool quitDown = input.IsDown(Keys.F10);
        bool quitWas  = input.WasDown(Keys.F10);
        var quitBtn   = MakeButton(quitDown, quitWas);

        Ui.Confirm = confirmBtn;
        Ui.Cancel = cancelBtn;
        Ui.Menu = menuBtn;

        System.Quit = quitBtn;

        _buttons[GameAction.Confirm] = confirmBtn;
        _buttons[GameAction.Cancel] = cancelBtn;
        _buttons[GameAction.Menu] = menuBtn;
        _buttons[GameAction.Quit] = quitBtn;
    }

    private static ActionButton MakeButton(bool down, bool wasDown)
    {
        bool pressed = down && !wasDown;
        bool released = !down && wasDown;
        return new ActionButton(down, pressed, released, repeated: pressed);
    }

    private ActionButton MakeRepeatButton(bool down, float dt, ref RepeatState rep)
    {
        bool pressed = down && !rep.WasDown;
        bool released = !down && rep.WasDown;

        bool repeated = false;

        if (down)
        {
            rep.TimeHeld += dt;

            if (pressed)
            {
                rep.NextFireAt = RepeatDelay;
                repeated = true;
            }
            else if (rep.TimeHeld >= rep.NextFireAt)
            {
                repeated = true;
                rep.NextFireAt += RepeatRate;
            }
        }
        else
        {
            rep = default;
        }

        rep.WasDown = down;

        // For UI we treat repeated as “fire”
        return new ActionButton(down, pressed, released, repeated);
    }
    public static ActionMap CreateDefault()
    {
        return new ActionMap
        {
            // Menu navigation feel
            RepeatDelay = 0.25f,
            RepeatRate = 0.09f
        };
    }

}
