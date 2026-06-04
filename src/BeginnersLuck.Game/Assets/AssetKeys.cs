namespace BeginnersLuck.Game.Assets;

public static class AssetKeys
{
    public static class Entities
    {
        public const string BerryBush = "entity.resource.berry_bush";
        public const string WoodPile = "entity.resource.wood_pile";
        public const string StonePile = "entity.resource.stone_pile";
        public const string OldChest = "entity.container.old_chest";
        public const string Slime = "entity.enemy.slime";
        public const string Gate = "entity.structure.gate";
        public const string Tree = "entity.prop.tree";
        public const string Rock = "entity.prop.rock";
    }

    public static class Items
    {
        public const string Berries = "item.berries";
        public const string Wood = "item.wood";
        public const string Stone = "item.stone";
        public const string OldCoin = "item.old_coin";
        public const string SlimeGel = "item.slime_gel";
        public const string HealthHerb = "item.health_herb";
    }

    public static class Actor
    {
        public static class Player
        {
            public const string IdleDown = "actor.player.idle.down";
            public const string WalkDown = "actor.player.walk.down";
            public const string IdleUp = "actor.player.idle.up";
            public const string IdleSide = "actor.player.idle.side";
            public const string WalkUp = "actor.player.walk.up";
            public const string WalkSide = "actor.player.walk.side";
        }
    }

    public static class Actors
    {
        public const string PlayerIdleDown = Actor.Player.IdleDown;
        public const string PlayerIdleUp = Actor.Player.IdleUp;
        public const string PlayerIdleSide = Actor.Player.IdleSide;
        public const string PlayerWalkDown = Actor.Player.WalkDown;
        public const string PlayerWalkUp = Actor.Player.WalkUp;
        public const string PlayerWalkSide = Actor.Player.WalkSide;
    }

    public static class Tiles
    {
        public const string Grass = "tile.grass";
        public const string Water = "tile.water";
        public const string Dirt = "tile.dirt";
        public const string StoneGround = "tile.stone_ground";
        public const string Wall = "tile.wall";
    }
}
