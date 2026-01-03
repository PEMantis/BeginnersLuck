namespace BeginnersLuck.WorldGen.Local;

public interface ILocalGenStep
{
    string Name { get; }
    void Run(LocalGenContext ctx);
}
