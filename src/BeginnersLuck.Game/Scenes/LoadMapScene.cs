using System;
using System.Drawing;
using BeginnersLuck.Engine.Rendering;
using BeginnersLuck.Engine.Scenes;
using BeginnersLuck.Engine.UI;
using BeginnersLuck.Engine.Update;
using BeginnersLuck.Engine.World;
using BeginnersLuck.Game.Services;
using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Local;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;
using TileId = global::BeginnersLuck.WorldGen.Data.TileId;
using TileFlags = global::BeginnersLuck.WorldGen.Data.TileFlags;


namespace BeginnersLuck.Game.Scenes;

public sealed class LocalMapScene : SceneBase
{
    private readonly GameServices _s;
    private readonly string _mapBinPath;
    private readonly LocalMapPurpose _purpose;

    private LocalMapData? _local;
    private Texture2D? _white;

    private KeyboardState _prevKs;
    private GamePadState _prevPad;

    public LocalMapScene(GameServices s, string mapBinPath, LocalMapPurpose purpose)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
        _mapBinPath = mapBinPath ?? throw new ArgumentNullException(nameof(mapBinPath));
        _purpose = purpose;
    }

    public override void Load(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _white = new Texture2D(graphicsDevice, 1, 1);
        _white.SetData(new[] { Color.White });

        _local = LocalMapBinLoader.Load(_mapBinPath);
    }

    public override void Unload()
    {
        _white?.Dispose();
        _white = null;
        _local = null;
    }

    public override void Update(UpdateContext uc)
    {
        var ks = Keyboard.GetState();
        var pad = GamePad.GetState(PlayerIndex.One);

        // Back to world map (Esc / B)
        if (Pressed(ks, Keys.Escape) || Pressed(pad, Buttons.B))
        {
            _s.Scenes.Pop();
            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        _prevKs = ks;
        _prevPad = pad;
    }

    protected override void DrawWorld(RenderContext rc)
    {
        if (_local == null || _white == null) return;

        var sb = rc.SpriteBatch;

        sb.Begin(
            samplerState: SamplerState.PointClamp,
            blendState: BlendState.AlphaBlend);

        // TEMP: just draw a tiny minimap-ish preview so you can confirm load is working.
        // Replace this with your real tile renderer later.
        int n = _local.Size;
        int scale = 2; // 128 -> 256px

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            int i = _local.Index(x, y);
            var t = _local.Terrain[i];

                Microsoft.Xna.Framework.Color c = t switch
                {
                    TileId.DeepWater => new Color(10, 18, 50),
                    TileId.ShallowWater => new Color(25, 55, 120),
                    TileId.Mountain => new Color(120, 120, 120),
                    TileId.Hill => new Color(90, 90, 90),
                    TileId.Swamp => new Color(35, 75, 35),
                    _ => Color.Magenta // <--- if you see magenta, you’re getting an unexpected tile id
                };


                // Roads/rivers overlay (bright)
                if ((_local.Flags[i] & TileFlags.Road) != 0) c = Color.Orange;
            if ((_local.Flags[i] & TileFlags.River) != 0) c = Color.CornflowerBlue;

            sb.Draw(_white, new Microsoft.Xna.Framework.Rectangle(x * scale, y * scale, scale, scale), c);
        }

        sb.End();
    }

    protected override void DrawUI(RenderContext rc)
    {
        if (_local == null) return;

        var sb = rc.SpriteBatch;

        sb.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);

        _s.UiFont.Draw(sb, $"LOCAL {_local.Size}x{_local.Size}  ({_local.WorldX},{_local.WorldY})  {_local.Biome}",
            new Vector2(8, 8), Color.White * 0.9f, scale: 1);

        _s.UiFont.Draw(sb, "ESC/B: Back",
            new Vector2(8, 20), Color.White * 0.7f, scale: 1);

        sb.End();
    }

    private bool Pressed(KeyboardState ks, Keys k) => ks.IsKeyDown(k) && !_prevKs.IsKeyDown(k);
    private bool Pressed(GamePadState pad, Buttons b) => pad.IsButtonDown(b) && !_prevPad.IsButtonDown(b);
}
