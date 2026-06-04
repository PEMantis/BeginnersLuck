using System;
using System.Collections.Generic;
using System.Text;
using BeginnersLuck.Engine.Graphics;
using BeginnersLuck.Engine.Rendering;
using BeginnersLuck.Engine.Scenes;
using BeginnersLuck.Engine.UI;
using BeginnersLuck.Engine.Update;
using BeginnersLuck.Game.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BeginnersLuck.Game.Scenes;

/// <summary>
/// Developer-only sprite sheet inspector scene.
/// Shows a spritesheet with a grid overlay and displays source rectangle info on hover.
/// </summary>
public sealed class SpriteSheetInspectorScene : SceneBase
{
    // Available sheets to inspect
    private static readonly string[] AvailableSheets = new[]
    {
        "Art/Packs/CainosTopDownBasic/TX Plant.png",
        "Art/Packs/CainosTopDownBasic/TX Props.png",
        "Art/Packs/CainosTopDownBasic/TX Struct.png",
        "Art/Packs/CainosTopDownBasic/TX Tileset Grass.png",
        "Art/Packs/CainosTopDownBasic/TX Tileset Stone Ground.png",
        "Art/Packs/CainosTopDownBasic/TX Tileset Wall.png",
    };

    private readonly GameServices _s;
    private ContentManager? _contentManager;
    private int _currentSheetIndex = 0;
    private Texture2D? _currentSheet;
    private string _currentSheetPath = "";

    private int _gridSize = 32; // 16, 32, or 64
    private int _offsetX = 0;
    private int _offsetY = 0;

    private Point _lastMousePos = Point.Zero;
    private Point _hoveredGridCell = new(-1, -1);

    private Texture2D? _white;
    private KeyboardState _prevKs;

    public SpriteSheetInspectorScene(GameServices s)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
    }

    public override void Load(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _white = new Texture2D(graphicsDevice, 1, 1);
        _white.SetData(new[] { Color.White });
        Console.WriteLine("[SpriteSheetInspector] Scene loaded!");

        _contentManager = content;
        LoadCurrentSheet(content);
        _prevKs = Keyboard.GetState();
    }

    public override void Unload()
    {
        _currentSheet?.Dispose();
        _currentSheet = null;
        _white?.Dispose();
        _white = null;
    }

    public override void Update(UpdateContext uc)
    {
        var ks = Keyboard.GetState();
        var ms = Mouse.GetState();

        // Escape to return
        if (ks.IsKeyDown(Keys.Escape) && !_prevKs.IsKeyDown(Keys.Escape))
        {
            _s.Scenes.Pop();
            _prevKs = ks;
            return;
        }

        // Sheet switching (PageUp/PageDown)
        if (ks.IsKeyDown(Keys.PageUp) && !_prevKs.IsKeyDown(Keys.PageUp))
        {
            _currentSheetIndex = (_currentSheetIndex - 1 + AvailableSheets.Length) % AvailableSheets.Length;
            if (_contentManager != null)
                LoadCurrentSheet(_contentManager);
        }

        if (ks.IsKeyDown(Keys.PageDown) && !_prevKs.IsKeyDown(Keys.PageDown))
        {
            _currentSheetIndex = (_currentSheetIndex + 1) % AvailableSheets.Length;
            if (_contentManager != null)
                LoadCurrentSheet(_contentManager);
        }

        // Grid size switching (R, T, Y keys for simplicity, avoid number conflicts)
        if (ks.IsKeyDown(Keys.R) && !_prevKs.IsKeyDown(Keys.R))
            _gridSize = 16;
        if (ks.IsKeyDown(Keys.T) && !_prevKs.IsKeyDown(Keys.T))
            _gridSize = 32;
        if (ks.IsKeyDown(Keys.Y) && !_prevKs.IsKeyDown(Keys.Y))
            _gridSize = 64;

        // Pan with arrow keys
        if (ks.IsKeyDown(Keys.Left)) _offsetX -= 5;
        if (ks.IsKeyDown(Keys.Right)) _offsetX += 5;
        if (ks.IsKeyDown(Keys.Up)) _offsetY -= 5;
        if (ks.IsKeyDown(Keys.Down)) _offsetY += 5;

        // Zoom with +/- keys
        if (ks.IsKeyDown(Keys.Add) && !_prevKs.IsKeyDown(Keys.Add))
            _gridSize = Math.Min(256, _gridSize + 8);
        if (ks.IsKeyDown(Keys.Subtract) && !_prevKs.IsKeyDown(Keys.Subtract))
            _gridSize = Math.Max(8, _gridSize - 8);

        // Update hovered cell based on mouse position
        _lastMousePos = new Point(ms.X, ms.Y);
        UpdateHoveredCell();

        // Click to output manifest JSON
        if (ms.LeftButton == ButtonState.Pressed)
        {
            OutputManifestJson();
        }

        // Enter/Space to output as well
        if ((ks.IsKeyDown(Keys.Enter) && !_prevKs.IsKeyDown(Keys.Enter)) ||
            (ks.IsKeyDown(Keys.Space) && !_prevKs.IsKeyDown(Keys.Space)))
        {
            OutputManifestJson();
        }

        _prevKs = ks;
    }

    protected override void DrawUI(RenderContext rc)
    {
        if (_white == null || _currentSheet == null)
            return;

        var sb = rc.SpriteBatch;
        int w = PixelRenderer.InternalWidth;
        int h = PixelRenderer.InternalHeight;

        sb.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);

        // Semi-transparent background
        sb.Draw(_white, new Rectangle(0, 0, w, h), Color.Black * 0.3f);

        // Draw sheet with pan offset
        DrawSheetWithGrid(sb, w, h);

        // Draw info panel
        DrawInfoPanel(sb, w, h);

        sb.End();
    }

    private void DrawSheetWithGrid(SpriteBatch sb, int screenW, int screenH)
    {
        if (_currentSheet == null) return;

        // Compute visible sheet area
        int sheetDisplayX = 50 + _offsetX;
        int sheetDisplayY = 50 + _offsetY;
        int sheetDisplayW = _currentSheet.Width;
        int sheetDisplayH = _currentSheet.Height;

        // Draw the sheet texture
        sb.Draw(_currentSheet,
            new Rectangle(sheetDisplayX, sheetDisplayY, sheetDisplayW, sheetDisplayH),
            Color.White);

        // Draw grid lines
        DrawGridLines(sb, sheetDisplayX, sheetDisplayY, sheetDisplayW, sheetDisplayH);

        // Draw hovered cell highlight
        if (_hoveredGridCell.X >= 0 && _hoveredGridCell.Y >= 0)
        {
            DrawHoveredCellHighlight(sb, sheetDisplayX, sheetDisplayY);
        }
    }

    private void DrawGridLines(SpriteBatch sb, int x, int y, int w, int h)
    {
        if (_white == null) return;

        // Vertical lines
        for (int i = 0; i <= w; i += _gridSize)
        {
            int lineX = x + i;
            sb.Draw(_white, new Rectangle(lineX, y, 1, h), Color.Cyan * 0.6f);
        }

        // Horizontal lines
        for (int j = 0; j <= h; j += _gridSize)
        {
            int lineY = y + j;
            sb.Draw(_white, new Rectangle(x, lineY, w, 1), Color.Cyan * 0.6f);
        }
    }

    private void DrawHoveredCellHighlight(SpriteBatch sb, int sheetX, int sheetY)
    {
        if (_white == null) return;

        int cellLeft = sheetX + _hoveredGridCell.X * _gridSize;
        int cellTop = sheetY + _hoveredGridCell.Y * _gridSize;

        var rect = new Rectangle(cellLeft, cellTop, _gridSize, _gridSize);
        sb.Draw(_white, rect, Color.Yellow * 0.2f);

        // Border
        DrawRectOutline(sb, rect, Color.Yellow);
    }

    private void DrawRectOutline(SpriteBatch sb, Rectangle rect, Color color)
    {
        if (_white == null) return;

        sb.Draw(_white, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
        sb.Draw(_white, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color);
        sb.Draw(_white, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
        sb.Draw(_white, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color);
    }

    private void DrawInfoPanel(SpriteBatch sb, int screenW, int screenH)
    {
        if (_white == null) return;

        int panelX = 10;
        int panelY = screenH - 200;
        int panelW = screenW - 20;
        int panelH = 190;

        // Panel background
        sb.Draw(_white, new Rectangle(panelX, panelY, panelW, panelH), new Color(20, 20, 40) * 0.95f);
        DrawRectOutline(sb, new Rectangle(panelX, panelY, panelW, panelH), Color.Gold);

        // Title
        var titleText = $"Sprite Sheet Inspector [{_currentSheetIndex + 1}/{AvailableSheets.Length}]";
        DrawText(sb, _s.UiFont, titleText, panelX + 10, panelY + 5, Color.White, scale: 2);

        // Sheet info
        var sheetName = System.IO.Path.GetFileName(_currentSheetPath);
        DrawText(sb, _s.UiFont, $"Sheet: {sheetName}", panelX + 10, panelY + 25, Color.Cyan, scale: 1);

        if (_currentSheet != null)
        {
            DrawText(sb, _s.UiFont, $"Size: {_currentSheet.Width}x{_currentSheet.Height}px",
                panelX + 10, panelY + 38, Color.LimeGreen, scale: 1);
        }

        // Grid info
        DrawText(sb, _s.UiFont, $"Grid: {_gridSize}x{_gridSize}px (R=16, T=32, Y=64)",
            panelX + 10, panelY + 51, Color.Yellow, scale: 1);

        // Mouse info
        DrawText(sb, _s.UiFont, $"Mouse: {_lastMousePos.X}, {_lastMousePos.Y}",
            panelX + 10, panelY + 64, Color.White, scale: 1);

        // Hovered cell info
        if (_hoveredGridCell.X >= 0 && _hoveredGridCell.Y >= 0)
        {
            int srcX = _hoveredGridCell.X * _gridSize;
            int srcY = _hoveredGridCell.Y * _gridSize;
            DrawText(sb, _s.UiFont, $"Cell: ({_hoveredGridCell.X}, {_hoveredGridCell.Y})",
                panelX + 10, panelY + 77, Color.Gold, scale: 1);
            DrawText(sb, _s.UiFont, $"SourceRect: x={srcX} y={srcY} w={_gridSize} h={_gridSize}",
                panelX + 10, panelY + 90, Color.Gold, scale: 1);
        }

        // Help text
        DrawText(sb, _s.UiFont, "PageUp/Dn=Sheet | R/T/Y=Grid | +/-=Zoom | Arrow=Pan | Click/Enter=Output | Esc=Exit",
            panelX + 10, panelY + 110, Color.White * 0.7f, scale: 1);

        DrawText(sb, _s.UiFont, "Click a sprite cell to output manifest JSON to console.",
            panelX + 10, panelY + 143, Color.LimeGreen * 0.8f, scale: 1);

        DrawText(sb, _s.UiFont, "Copy the output and add to asset-manifest.json.",
            panelX + 10, panelY + 156, Color.LimeGreen * 0.8f, scale: 1);

        DrawText(sb, _s.UiFont, "Update key to match the entity type.",
            panelX + 10, panelY + 169, Color.LimeGreen * 0.8f, scale: 1);
    }

    private void DrawText(SpriteBatch sb, IFont font, string text, int x, int y, Color color, int scale = 1)
    {
        font.Draw(sb, text, new Vector2(x, y), color, scale);
    }

    private void LoadCurrentSheet(ContentManager content)
    {
        _currentSheet?.Dispose();
        _currentSheet = null;
        _offsetX = 0;
        _offsetY = 0;

        if (_currentSheetIndex >= 0 && _currentSheetIndex < AvailableSheets.Length)
        {
            _currentSheetPath = AvailableSheets[_currentSheetIndex];
            try
            {
                _currentSheet = content.Load<Texture2D>(_currentSheetPath);
                Console.WriteLine($"[SpriteInspector] Loaded sheet: {_currentSheetPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SpriteInspector] Failed to load sheet: {_currentSheetPath} | {ex.Message}");
            }
        }
    }

    private void UpdateHoveredCell()
    {
        int sheetDisplayX = 50 + _offsetX;
        int sheetDisplayY = 50 + _offsetY;

        if (_lastMousePos.X < sheetDisplayX || _lastMousePos.Y < sheetDisplayY)
        {
            _hoveredGridCell = new Point(-1, -1);
            return;
        }

        int relX = _lastMousePos.X - sheetDisplayX;
        int relY = _lastMousePos.Y - sheetDisplayY;

        if (_currentSheet == null || relX >= _currentSheet.Width || relY >= _currentSheet.Height)
        {
            _hoveredGridCell = new Point(-1, -1);
            return;
        }

        int cellX = relX / _gridSize;
        int cellY = relY / _gridSize;

        _hoveredGridCell = new Point(cellX, cellY);
    }

    private void OutputManifestJson()
    {
        if (_hoveredGridCell.X < 0 || _hoveredGridCell.Y < 0)
        {
            Console.WriteLine("[SpriteInspector] No cell hovered.");
            return;
        }

        int srcX = _hoveredGridCell.X * _gridSize;
        int srcY = _hoveredGridCell.Y * _gridSize;

        var sheetName = System.IO.Path.GetFileNameWithoutExtension(_currentSheetPath);
        var sheetWithoutTX = sheetName.StartsWith("TX ") ? sheetName.Substring(3) : sheetName;

        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"key\": \"TODO.{sheetWithoutTX}.{_hoveredGridCell.X}_{_hoveredGridCell.Y}\",");
        sb.AppendLine($"  \"path\": \"Art/Packs/CainosTopDownBasic/{System.IO.Path.GetFileName(_currentSheetPath)}\",");
        sb.AppendLine($"  \"x\": {srcX},");
        sb.AppendLine($"  \"y\": {srcY},");
        sb.AppendLine($"  \"width\": {_gridSize},");
        sb.AppendLine($"  \"height\": {_gridSize},");
        sb.AppendLine($"  \"scale\": 1");
        sb.AppendLine("},");

        var json = sb.ToString();
        Console.WriteLine("\n[SpriteInspector] Manifest entry:");
        Console.WriteLine(json);
        Console.WriteLine("[SpriteInspector] Copy this to asset-manifest.json and update the key.");
    }
}
