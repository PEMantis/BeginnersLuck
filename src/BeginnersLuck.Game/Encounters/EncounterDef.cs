namespace BeginnersLuck.Game.Encounters;

public readonly record struct EnemyDef(string Id, string Name, int Hp);
public readonly record struct EncounterDef(string Id, string Name, EnemyDef[] Enemies);
