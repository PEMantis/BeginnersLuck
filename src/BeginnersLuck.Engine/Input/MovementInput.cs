using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace BeginnersLuck.Engine.Input;

public static class MovementInput
{
    public static Vector2 GetMoveVector(in InputSnapshot input, float deadZone = 0.25f)
    {
        Vector2 v = Vector2.Zero;

        // keyboard
        if (input.Keyboard.IsKeyDown(Keys.W) || input.Keyboard.IsKeyDown(Keys.Up)) v.Y -= 1;
        if (input.Keyboard.IsKeyDown(Keys.S) || input.Keyboard.IsKeyDown(Keys.Down)) v.Y += 1;
        if (input.Keyboard.IsKeyDown(Keys.A) || input.Keyboard.IsKeyDown(Keys.Left)) v.X -= 1;
        if (input.Keyboard.IsKeyDown(Keys.D) || input.Keyboard.IsKeyDown(Keys.Right)) v.X += 1;

        // dpad
        if (input.Pad.IsButtonDown(Buttons.DPadUp)) v.Y -= 1;
        if (input.Pad.IsButtonDown(Buttons.DPadDown)) v.Y += 1;
        if (input.Pad.IsButtonDown(Buttons.DPadLeft)) v.X -= 1;
        if (input.Pad.IsButtonDown(Buttons.DPadRight)) v.X += 1;

        // left stick (note: Y is inverted in XNA style, up is negative)
        var stick = input.Pad.ThumbSticks.Left;
        var stickVec = new Vector2(stick.X, -stick.Y);

        if (stickVec.LengthSquared() >= deadZone * deadZone)
            v += stickVec;

        if (v.LengthSquared() > 1f)
            v.Normalize();

        return v;
    }
}
