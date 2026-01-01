using BeginnersLuck.Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace BeginnersLuck.Engine.Input;

public sealed class InputHost
{
    private KeyboardState _prevK;
    private MouseState _prevM;
    private GamePadState _prevP;

    public InputSnapshot Snapshot { get; private set; }

    public void Update(PixelRenderer pixel, PlayerIndex player = PlayerIndex.One)
    {
        var k = Keyboard.GetState();
        var m = Mouse.GetState();
        var p = GamePad.GetState(player);

        Point? vMouse = null;
        if (pixel.TryScreenToVirtual(new Point(m.X, m.Y), out var vm))
            vMouse = vm;

        Snapshot = new InputSnapshot(k, _prevK, m, _prevM, p, _prevP, vMouse);

        _prevK = k;
        _prevM = m;
        _prevP = p;
    }
}
