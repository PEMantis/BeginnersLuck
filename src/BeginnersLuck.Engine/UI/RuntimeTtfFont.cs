using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StbTrueTypeSharp;

namespace BeginnersLuck.Engine.UI;

/// <summary>
/// Runtime-baked bitmap font from a TTF. No MGCB required.
/// Bakes an atlas for ASCII by default (32..126).
/// </summary>
public sealed class RuntimeTtfFont : IFont
{
    private readonly GraphicsDevice _gd;
    private readonly byte[] _ttf;
    private readonly int _pxHeight;
    private readonly int _firstChar;
    private readonly int _lastChar;

    private Texture2D _atlas = null!;
    private readonly Dictionary<int, Glyph> _glyphs = new();

    private int _ascentPx;
    private int _descentPx;
    private int _lineGapPx;

    // “Extra breathing room” in UI text
    public int ExtraSpacingX { get; set; } = 1;
    public int ExtraSpacingY { get; set; } = 2;

    private struct Glyph
    {
        public Rectangle Src;
        public int AdvancePx;

        // stb offsets from baseline to bitmap top-left (in pixels at the same scale used to build bitmap)
        public int XOffPx;
        public int YOffPx;

        public int W;
        public int H;
    }

    public RuntimeTtfFont(
        GraphicsDevice gd,
        byte[] ttfBytes,
        int pixelHeight,
        int firstChar = 32,
        int lastChar = 126)
    {
        _gd = gd ?? throw new ArgumentNullException(nameof(gd));
        _ttf = ttfBytes ?? throw new ArgumentNullException(nameof(ttfBytes));
        _pxHeight = pixelHeight;
        _firstChar = firstChar;
        _lastChar = lastChar;

        BakeAtlas();
    }

    public int LineHeight(int scale = 1)
        => ((_ascentPx - _descentPx + _lineGapPx) + ExtraSpacingY) * scale;

    public Point Measure(string text, int scale = 1)
    {
        if (string.IsNullOrEmpty(text))
            return Point.Zero;

        int x = 0;
        int y = 0;
        int maxX = 0;

        int lineH = LineHeight(scale);

        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];

            if (ch == '\n')
            {
                maxX = Math.Max(maxX, x);
                x = 0;
                y += lineH;
                continue;
            }

            if (!_glyphs.TryGetValue(ch, out var g))
            {
                x += (int)(0.5f * _pxHeight) * scale;
                continue;
            }

            x += (g.AdvancePx + ExtraSpacingX) * scale;
        }

        maxX = Math.Max(maxX, x);
        return new Point(maxX, y + lineH);
    }

    public void DrawString(SpriteBatch sb, string text, Vector2 pos, Color color, int scale = 1)
        => DrawInternal(sb, text, clip: null, pos, color, scale);

    public void DrawStringClipped(SpriteBatch sb, string text, Rectangle clip, Vector2 pos, Color color, int scale = 1)
        => DrawInternal(sb, text, clip, pos, color, scale);

    private void DrawInternal(SpriteBatch sb, string text, Rectangle? clip, Vector2 pos, Color color, int scale)
    {
        if (_atlas == null) return;
        if (string.IsNullOrEmpty(text)) return;

        int startX = (int)pos.X;
        int x = startX;
        int y = (int)pos.Y;

        int lineH = LineHeight(scale);

        // baseline for the line (top + ascent)
        int baseline = y + _ascentPx * scale;

        if (clip.HasValue)
        {
            var prev = _gd.ScissorRectangle;
            _gd.ScissorRectangle = clip.Value;

            DrawRun();

            _gd.ScissorRectangle = prev;
        }
        else
        {
            DrawRun();
        }

        void DrawRun()
        {
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];

                if (ch == '\n')
                {
                    x = startX;
                    y += lineH;
                    baseline = y + _ascentPx * scale;
                    continue;
                }

                if (!_glyphs.TryGetValue(ch, out var g))
                {
                    x += (int)(0.5f * _pxHeight) * scale;
                    continue;
                }

                // If this glyph has no bitmap (space, missing glyph, etc.), just advance.
                if (g.W <= 0 || g.H <= 0 || g.Src == Rectangle.Empty)
                {
                    x += (g.AdvancePx + ExtraSpacingX) * scale;
                    continue;
                }

                int dx = x + g.XOffPx * scale;
                int dy = baseline + g.YOffPx * scale;

                var dst = new Rectangle(dx, dy, g.W * scale, g.H * scale);
                sb.Draw(_atlas, dst, g.Src, color);

                x += (g.AdvancePx + ExtraSpacingX) * scale;
            }
        }
    }

    public void Dispose()
    {
        _atlas?.Dispose();
        _atlas = null!;
        _glyphs.Clear();
    }

    private unsafe void BakeAtlas()
    {
        // Initialize font
        var fontInfo = new StbTrueType.stbtt_fontinfo();
        fixed (byte* p = _ttf)
        {
            int offset = StbTrueType.stbtt_GetFontOffsetForIndex(p, 0);
            if (offset < 0)
                throw new InvalidOperationException("Failed to find font offset (stbtt_GetFontOffsetForIndex returned < 0).");

            if (StbTrueType.stbtt_InitFont(fontInfo, p, offset) == 0)
                throw new InvalidOperationException("Failed to init TTF font (stbtt_InitFont returned 0).");
        }

        float scale = StbTrueType.stbtt_ScaleForPixelHeight(fontInfo, _pxHeight);

        int ascent = 0, descent = 0, lineGap = 0;
        StbTrueType.stbtt_GetFontVMetrics(fontInfo, &ascent, &descent, &lineGap);

        _ascentPx  = (int)MathF.Round(ascent * scale);
        _descentPx = (int)MathF.Round(descent * scale);
        _lineGapPx = (int)MathF.Round(lineGap * scale);

        const int pad = 1;

        // Collect glyph rasters
        var glyphs = new List<(int ch, byte[] bmp, int w, int h, int xoff, int yoff, int advPx)>();

        for (int ch = _firstChar; ch <= _lastChar; ch++)
        {
            int w = 0, h = 0, xoff = 0, yoff = 0;

            byte* bmpPtr = StbTrueType.stbtt_GetCodepointBitmap(
                fontInfo,
                scale, scale,
                ch,
                &w, &h,
                &xoff, &yoff
            );

            byte[] bmp;
            if (bmpPtr == null || w <= 0 || h <= 0)
            {
                bmp = Array.Empty<byte>();
                w = 0; h = 0; xoff = 0; yoff = 0;
            }
            else
            {
                bmp = new byte[w * h];
                for (int i = 0; i < bmp.Length; i++)
                    bmp[i] = bmpPtr[i];
            }

            if (bmpPtr != null)
                StbTrueType.stbtt_FreeBitmap(bmpPtr, null);

            int adv = 0, lsb = 0;
            StbTrueType.stbtt_GetCodepointHMetrics(fontInfo, ch, &adv, &lsb);

            int advPx = (int)MathF.Round(adv * scale);

            glyphs.Add((ch, bmp, w, h, xoff, yoff, advPx));
        }

        // Pack into a simple row atlas
        int atlasW = 512;
        int x = pad, y = pad, rowH = 0;

        for (int i = 0; i < glyphs.Count; i++)
        {
            var g = glyphs[i];
            int gw = Math.Max(1, g.w);
            int gh = Math.Max(1, g.h);

            if (x + gw + pad >= atlasW)
            {
                x = pad;
                y += rowH + pad;
                rowH = 0;
            }

            rowH = Math.Max(rowH, gh);
            x += gw + pad;
        }

        int atlasH = y + rowH + pad;
        if (atlasH < 64) atlasH = 64;

        // Premultiplied RGBA atlas
        var pixels = new byte[atlasW * atlasH * 4];

        x = pad; y = pad; rowH = 0;
        _glyphs.Clear();

        for (int i = 0; i < glyphs.Count; i++)
        {
            var g = glyphs[i];

            // Glyphs with no bitmap: store empty src but keep advance
            if (g.w <= 0 || g.h <= 0 || g.bmp.Length == 0)
            {
                _glyphs[g.ch] = new Glyph
                {
                    Src = Rectangle.Empty,
                    AdvancePx = g.advPx,
                    XOffPx = 0,
                    YOffPx = 0,
                    W = 0,
                    H = 0
                };

                x += pad;
                continue;
            }

            if (x + g.w + pad >= atlasW)
            {
                x = pad;
                y += rowH + pad;
                rowH = 0;
            }

            // Blit grayscale -> premultiplied white (RGB=A, A=A)
            for (int yy = 0; yy < g.h; yy++)
            for (int xx = 0; xx < g.w; xx++)
            {
                byte a = g.bmp[yy * g.w + xx];

                int dstX = x + xx;
                int dstY = y + yy;
                int di = (dstY * atlasW + dstX) * 4;

                pixels[di + 0] = a;
                pixels[di + 1] = a;
                pixels[di + 2] = a;
                pixels[di + 3] = a;
            }

            var src = new Rectangle(x, y, g.w, g.h);

            _glyphs[g.ch] = new Glyph
            {
                Src = src,
                AdvancePx = g.advPx,
                XOffPx = g.xoff,
                YOffPx = g.yoff,
                W = g.w,
                H = g.h
            };

            rowH = Math.Max(rowH, g.h);
            x += g.w + pad;
        }

        _atlas?.Dispose();
        _atlas = new Texture2D(_gd, atlasW, atlasH, false, SurfaceFormat.Color);
        _atlas.SetData(pixels);
    }
}
