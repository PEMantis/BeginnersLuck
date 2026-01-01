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

    // --- Keyboard helpers ---
    public bool KeyDown(Keys k) => Keyboard.IsKeyDown(k);
    public bool KeyPressed(Keys k) => Keyboard.IsKeyDown(k) && PrevKeyboard.IsKeyUp(k);
    public bool KeyReleased(Keys k) => Keyboard.IsKeyUp(k) && PrevKeyboard.IsKeyDown(k);

    // --- Mouse helpers (fully qualified enum to avoid collision) ---
    public bool LeftDown => Mouse.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;

    public bool LeftPressed =>
        Mouse.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed &&
        PrevMouse.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Released;

    // --- GamePad helpers ---
    public bool PadDown(Buttons b) => Pad.IsButtonDown(b);
    public bool PadPressed(Buttons b) => Pad.IsButtonDown(b) && PrevPad.IsButtonUp(b);

    // --- Unified helpers (for ActionMap / gameplay) ---
    public bool IsDown(Keys k) => Keyboard.IsKeyDown(k);
    public bool WasDown(Keys k) => PrevKeyboard.IsKeyDown(k);

    public bool IsDown(Buttons b) => Pad.IsButtonDown(b);
    public bool WasDown(Buttons b) => PrevPad.IsButtonDown(b);

    public bool Pressed(Keys k) => IsDown(k) && !WasDown(k);
    public bool Pressed(Buttons b) => IsDown(b) && !WasDown(b);

    public bool Released(Keys k) => !IsDown(k) && WasDown(k);
    public bool Released(Buttons b) => !IsDown(b) && WasDown(b);
}
