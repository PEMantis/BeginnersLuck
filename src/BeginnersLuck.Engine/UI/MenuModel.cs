using BeginnersLuck.Engine.Input;
using Microsoft.Xna.Framework;

namespace BeginnersLuck.Engine.UI;

public sealed class MenuModel
{
    private readonly List<MenuItem> _items = new();
    public IReadOnlyList<MenuItem> Items => _items;

    public int FocusIndex { get; private set; } = 0;

    // Navigation tuning
    private const float StickDeadZone = 0.35f;
    private const float InitialRepeatDelay = 0.25f;
    private const float RepeatInterval = 0.09f;

    // Repeat state
    private float _repeatTimer = 0f;
    private Point _repeatDir = Point.Zero;   // (-1,0),(1,0),(0,-1),(0,1)
    private bool _repeatArmed = false;

    // How focus moves
    public bool VerticalFocusEnabled { get; set; } = true;
    public bool HorizontalFocusEnabled { get; set; } = false; // turn on for tab rows etc.

    public void Add(MenuItem item) => _items.Add(item);

    public void SetFocus(int index)
    {
        if (_items.Count == 0) { FocusIndex = 0; return; }
        FocusIndex = Math.Clamp(index, 0, _items.Count - 1);
        if (!_items[FocusIndex].Enabled) FocusNext(1);
    }

    public void FocusNext(int dir)
    {
        if (_items.Count == 0) return;

        var start = FocusIndex;
        for (int i = 0; i < _items.Count; i++)
        {
            FocusIndex = (FocusIndex + dir + _items.Count) % _items.Count;
            if (_items[FocusIndex].Enabled) return;
        }
        FocusIndex = start;
    }

    public void UpdateFocusFromMouse(Point? virtualMouse)
    {
        if (virtualMouse is not Point vm) return;

        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].Enabled && _items[i].Bounds.Contains(vm))
            {
                FocusIndex = i;
                return;
            }
        }
    }

    public bool ActivateFocused()
    {
        if (_items.Count == 0) return false;
        var item = _items[FocusIndex];
        if (!item.Enabled) return false;
        item.OnActivate();
        return true;
    }

    public void HandleNavigation(in InputSnapshot input, ActionMap actions, float dtSeconds)
    {
        // 1) Immediate pressed handling (feels snappy)
        var pressedDir = GetPressedDir(input, actions);
        if (pressedDir != Point.Zero)
        {
            StepOrAdjust(pressedDir);
            ArmRepeat(pressedDir);
            return;
        }

        // 2) Determine if user is holding a direction (digital or stick)
        var holdDir = GetHeldDir(input, actions);
        if (holdDir == Point.Zero)
        {
            DisarmRepeat();
            return;
        }

        // 3) Direction changed while holding: step immediately + restart delay
        if (!_repeatArmed || holdDir != _repeatDir)
        {
            StepOrAdjust(holdDir);
            ArmRepeat(holdDir);
            return;
        }

        // 4) Repeat while held
        _repeatTimer -= dtSeconds;
        if (_repeatTimer <= 0f)
        {
            StepOrAdjust(_repeatDir);
            _repeatTimer += RepeatInterval;
        }
    }

    private void StepOrAdjust(Point dir)
    {
        if (_items.Count == 0) return;

        var item = _items[FocusIndex];

        // Left/Right first try to adjust the focused item (slider/tabs/value)
        if (dir.X < 0 && item.OnLeft != null)
        {
            item.OnLeft();
            return;
        }
        if (dir.X > 0 && item.OnRight != null)
        {
            item.OnRight();
            return;
        }

        // Otherwise, move focus if enabled for that axis
        if (dir.Y != 0 && VerticalFocusEnabled)
            FocusNext(Math.Sign(dir.Y)); // up = -1, down = +1

        if (dir.X != 0 && HorizontalFocusEnabled)
            FocusNext(Math.Sign(dir.X)); // left = -1, right = +1
    }

    private static Point GetPressedDir(in InputSnapshot input, ActionMap actions)
    {
        // Priority order prevents “two directions pressed” weirdness
        if (actions.Pressed(input, GameAction.MoveUp)) return new Point(0, -1);
        if (actions.Pressed(input, GameAction.MoveDown)) return new Point(0, +1);
        if (actions.Pressed(input, GameAction.MoveLeft)) return new Point(-1, 0);
        if (actions.Pressed(input, GameAction.MoveRight)) return new Point(+1, 0);
        return Point.Zero;
    }

    private static Point GetHeldDir(in InputSnapshot input, ActionMap actions)
    {
        // Digital hold (keyboard + D-pad)
        if (actions.Down(input, GameAction.MoveUp)) return new Point(0, -1);
        if (actions.Down(input, GameAction.MoveDown)) return new Point(0, +1);
        if (actions.Down(input, GameAction.MoveLeft)) return new Point(-1, 0);
        if (actions.Down(input, GameAction.MoveRight)) return new Point(+1, 0);

        // Stick hold (dominant axis)
        var sx = input.Pad.ThumbSticks.Left.X;
        var sy = input.Pad.ThumbSticks.Left.Y;

        var ax = MathF.Abs(sx);
        var ay = MathF.Abs(sy);

        if (ax < StickDeadZone && ay < StickDeadZone) return Point.Zero;

        if (ax >= ay)
            return sx < 0 ? new Point(-1, 0) : new Point(+1, 0);

        // Y: up is + in ThumbSticks; we want up = -1
        return sy > 0 ? new Point(0, -1) : new Point(0, +1);
    }

    private void ArmRepeat(Point dir)
    {
        _repeatArmed = true;
        _repeatDir = dir;
        _repeatTimer = InitialRepeatDelay;
    }

    private void DisarmRepeat()
    {
        _repeatArmed = false;
        _repeatDir = Point.Zero;
        _repeatTimer = 0f;
    }
}
