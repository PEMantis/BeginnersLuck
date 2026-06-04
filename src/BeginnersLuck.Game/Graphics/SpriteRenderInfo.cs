using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Game.Graphics;

public readonly record struct SpriteRenderInfo(
    Texture2D Texture,
    Rectangle Source,
    Vector2 Origin,
    float Scale,
    bool Missing,
    string Key);
