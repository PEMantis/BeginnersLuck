using Microsoft.Xna.Framework;

namespace BeginnersLuck.Game.World;

public readonly record struct WorldPoi(PoiKind Kind, Point Tile, string Name);
