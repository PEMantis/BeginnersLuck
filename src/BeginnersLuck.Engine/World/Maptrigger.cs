namespace BeginnersLuck.Engine.World;

public enum TriggerType
{
    Town,
    Encounter,
    Teleport,
    Message
}

public readonly record struct MapTrigger(int X, int Y, TriggerType Type, string Id);
