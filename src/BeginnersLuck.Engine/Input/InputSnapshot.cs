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

    public bool KeyDown(Keys k) => Keyboard.IsKeyDown(k);
    public bool KeyPressed(Keys k) => Keyboard.IsKeyDown(k) && PrevKeyboard.IsKeyUp(k);
    public bool KeyReleased(Keys k) => Keyboard.IsKeyUp(k) && PrevKeyboard.IsKeyDown(k);

    public bool LeftDown => Mouse.LeftButton == ButtonState.Pressed;
    public bool LeftPressed => Mouse.LeftButton == ButtonState.Pressed && PrevMouse.LeftButton == ButtonState.Released;

    public bool PadDown(Buttons b) => Pad.IsButtonDown(b);
    public bool PadPressed(Buttons b) => Pad.IsButtonDown(b) && PrevPad.IsButtonUp(b);
}
