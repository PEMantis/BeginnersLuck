using System;
using BeginnersLuck.Engine.Content;
using BeginnersLuck.Engine.Graphics;
using BeginnersLuck.Engine.Scenes;
using BeginnersLuck.Engine.Transitions;
using BeginnersLuck.Engine.UI;
using BeginnersLuck.Game.Encounters;
using BeginnersLuck.Game.Items;
using BeginnersLuck.Game.State;
using BeginnersLuck.Game.UI;
using BeginnersLuck.Game.World;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Game.Services;

public sealed class GameServices
{
    // Engine-level services
    public PixelRenderer Pixel { get; }
    public SceneManager Scenes { get; }
    public FadeTransition Fade { get; }
    public RawContent Raw { get; }

    // Fonts
    public IFont UiFont { get; }
    public IFont TitleFont { get; }

    // Optional compatibility shim: existing code using _services.Font keeps working (points to UiFont)
    public IFont Font => UiFont;

    public IFont ButtonFont { get; }

    // ✅ Shared 1x1 pixel
    public Texture2D PixelWhite { get; }

    // ✅ Toasts
    public ToastQueue Toasts { get; }

    // ✅ Item use logic (set after construction to avoid circular dependency)
    public ItemUseSystem ItemUse { get; set; } = null!;

    // Game-level services
    public Random Rng { get; }
    public EncounterDirector EncounterDirector { get; }
    public ZoneMap Zones { get; set; } = null!;

    public PlayerState Player { get; }
    public ItemDb Items { get; }

    public WorldState World { get; }

    public GameServices(
        PixelRenderer pixel,
        SceneManager scenes,
        FadeTransition fade,
        RawContent raw,
        IFont uiFont,
        IFont titleFont,
        IFont buttonFont,
        Texture2D pixelWhite,
        ToastQueue toasts,
        Random rng,
        EncounterDirector encounters,
        PlayerState player,
        ItemDb items,
        WorldState world)
    {
        Pixel = pixel;
        Scenes = scenes;
        Fade = fade;
        Raw = raw;

        UiFont = uiFont;
        TitleFont = titleFont;
        ButtonFont = buttonFont;
        
        PixelWhite = pixelWhite;
        Toasts = toasts;

        Rng = rng;
        EncounterDirector = encounters;
        Player = player;
        Items = items;
        World = world;
    }
}
