namespace BeginnersLuck.Game;

public static class Program
{
    public static Game1? GlobalGame;

    static void Main()
    {
        using var game = new Game1();
        GlobalGame = game;
        game.Run();
    }
}
