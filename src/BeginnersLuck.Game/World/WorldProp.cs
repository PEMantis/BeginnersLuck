using Microsoft.Xna.Framework;

namespace BeginnersLuck.Game.World;

public readonly record struct WorldProp(string SpriteId, Point Tile, bool BlocksMove);
