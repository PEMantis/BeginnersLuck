using Microsoft.Xna.Framework;

namespace BeginnersLuck.Engine.Graphics;

public sealed class Camera2D
{
    public Vector2 Position;
    public float Zoom { get; set; } = 1f;

    public Matrix GetViewMatrix()
    {
        var center = new Vector2(PixelRenderer.InternalWidth * 0.5f, PixelRenderer.InternalHeight * 0.5f);

        return
            Matrix.CreateTranslation(new Vector3(-Position, 0f)) *
            Matrix.CreateScale(Zoom, Zoom, 1f) *
            Matrix.CreateTranslation(new Vector3(center, 0f));
    }

}
