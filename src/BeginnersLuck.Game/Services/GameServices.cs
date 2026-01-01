using System;
using BeginnersLuck.Engine.Content;
using BeginnersLuck.Engine.Graphics;
using BeginnersLuck.Engine.Scenes;
using BeginnersLuck.Engine.Transitions;
using BeginnersLuck.Engine.UI;
using BeginnersLuck.Game.Encounters;
using BeginnersLuck.Game.World;

namespace BeginnersLuck.Game.Services;

public sealed class GameServices
{
    // Engine-level services
    public PixelRenderer Pixel { get; }
    public SceneManager Scenes { get; }
    public FadeTransition Fade { get; }
    public RawContent Raw { get; }
    public BitmapFont Font { get; }

    // Game-level services (safe here; this project owns them)
    public Random Rng { get; }
    public EncounterDirector EncounterDirector { get; }
    public ZoneMap Zones { get; set; } = null!; // built after map load (depends on map)

    public GameServices(
     PixelRenderer pixel,
     SceneManager scenes,
     FadeTransition fade,
     RawContent raw,
     BitmapFont font,
     Random rng,
     EncounterDirector encounters)
    {
        Pixel = pixel;
        Scenes = scenes;
        Fade = fade;
        Raw = raw;
        Font = font;

        Rng = rng;
        EncounterDirector = encounters;
    }

}
