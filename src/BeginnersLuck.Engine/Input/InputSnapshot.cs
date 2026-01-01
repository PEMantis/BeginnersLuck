using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace BeginnersLuck.Engine.Input;

public readonly struct InputSnapshot
{
    public InputSnapshot(
        KeyboardState keyboard, KeyboardState prevKeyboard,
        MouseState mouse, MouseState prevMouse,
        GamePadState pad, GamePadState prevPad,
        Point? virtualMouse)
    {
        Keyboard = keyboard;
        PrevKeyboard = prevKeyboard;
        Mouse = mouse;
        PrevMouse = prevMouse;
        Pad = pad;
        PrevPad = prevPad;
        VirtualMouse = virtualMouse;
    }

    public KeyboardState Keyboard { get; }
    public KeyboardState PrevKeyboard { get; }

    public MouseState Mouse { get; }
    public MouseState PrevMouse { get; }

    public GamePadState Pad { get; }
    public GamePadState PrevPad { get; }

    // null if mouse is in letterbox bars
    public Point? VirtualMouse { get; }

    // --------------------
    // Keyboard helpers
    // --------------------
    public bool IsDown(Keys k)
        => Keyboard.IsKeyDown(k);

    public bool WasDown(Keys k)
        => PrevKeyboard.IsKeyDown(k);

    public bool Pressed(Keys k)
        => Keyboard.IsKeyDown(k) && PrevKeyboard.IsKeyUp(k);

    public bool Released(Keys k)
        => Keyboard.IsKeyUp(k) && PrevKeyboard.IsKeyDown(k);

    // --------------------
    // GamePad helpers
    // --------------------
    public bool IsDown(Buttons b)
        => Pad.IsButtonDown(b);

    public bool WasDown(Buttons b)
        => PrevPad.IsButtonDown(b);

    public bool Pressed(Buttons b)
        => Pad.IsButtonDown(b) && PrevPad.IsButtonUp(b);

    public bool Released(Buttons b)
        => Pad.IsButtonUp(b) && PrevPad.IsButtonDown(b);

    // --------------------
    // Mouse helpers (optional, consistent)
    // --------------------
    public bool MouseLeftDown
        => Mouse.LeftButton == ButtonState.Pressed;

    public bool MouseLeftPressed
        => Mouse.LeftButton == ButtonState.Pressed &&
           PrevMouse.LeftButton == ButtonState.Released;

    public bool MouseLeftReleased
        => Mouse.LeftButton == ButtonState.Released &&
           PrevMouse.LeftButton == ButtonState.Pressed;
}
