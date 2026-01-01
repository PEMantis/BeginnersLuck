using Microsoft.Xna.Framework;

namespace BeginnersLuck.Engine.Graphics;

public sealed class Camera2D
{
    public Vector2 Position;
    public float Zoom = 1f;

    public Matrix GetViewMatrix()
    {
        // Camera centers on internal resolution space
        var half = new Vector2(PixelRenderer.InternalWidth, PixelRenderer.InternalHeight) / 2f;

        return
            Matrix.CreateTranslation(new Vector3(-Position, 0f)) *
            Matrix.CreateTranslation(new Vector3(half, 0f)) *
            Matrix.CreateScale(Zoom, Zoom, 1f);
    }
}
