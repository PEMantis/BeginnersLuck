using System;
using BeginnersLuck.Engine.Content;
using BeginnersLuck.Engine.Graphics;
using BeginnersLuck.Engine.Scenes;
using BeginnersLuck.Engine.Transitions;
using BeginnersLuck.Engine.UI;
using BeginnersLuck.Game.Encounters;
using BeginnersLuck.Game.World;
using BeginnersLuck.Game.Items;
using BeginnersLuck.Game.State;
using Microsoft.Xna.Framework.Graphics; // ✅ ADD

namespace BeginnersLuck.Game.Services;

public sealed class GameServices
{
    // Engine-level services
    public PixelRenderer Pixel { get; }
    public SceneManager Scenes { get; }
    public FadeTransition Fade { get; }
    public RawContent Raw { get; }
    public BitmapFont Font { get; }

    // ✅ Shared 1x1 pixel texture for UI primitives
    public Texture2D PixelWhite { get; }

    // Game-level services (safe here; this project owns them)
    public Random Rng { get; }
    public EncounterDirector EncounterDirector { get; }
    public ZoneMap Zones { get; set; } = null!;

    public PlayerState Player { get; }
    public ItemDb Items { get; }

    public GameServices(
        PixelRenderer pixel,
        SceneManager scenes,
        FadeTransition fade,
        RawContent raw,
        BitmapFont font,
        Texture2D pixelWhite,          // ✅ ADD
        Random rng,
        EncounterDirector encounters,
        PlayerState player,
        ItemDb items)
    {
        Pixel = pixel;
        Scenes = scenes;
        Fade = fade;
        Raw = raw;
        Font = font;

        PixelWhite = pixelWhite;       // ✅ ADD

        Rng = rng;
        EncounterDirector = encounters;
        Player = player;
        Items = items;
    }
}
