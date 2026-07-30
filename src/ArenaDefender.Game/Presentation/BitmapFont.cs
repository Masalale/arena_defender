using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ArenaDefender.Presentation;

/// <summary>A 5x7 pixel monospace bitmap font loaded from an external PNG sprite sheet atlas.</summary>
public sealed class BitmapFont : IDisposable
{
    private const int GlyphWidth = 5;
    private const int GlyphHeight = 7;
    private const int CellWidth = 6;
    private const int CellHeight = 8;
    private const int AtlasColumns = 16;
    private const int GlyphAdvance = CellWidth;

    private readonly Texture2D _texture;
    private bool _disposed;

    /// <exception cref="ArgumentNullException">The graphics device was null.</exception>
    public BitmapFont(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);

        string assetPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Font", "font_atlas.png");

        try
        {
            using var stream = File.OpenRead(assetPath);
            _texture = Texture2D.FromStream(graphicsDevice, stream);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Arena Defender could not load its font from '{assetPath}'. The Assets folder has " +
                "to sit next to the executable, so copy it across if you moved or published the " +
                "build on its own.",
                ex);
        }
    }

    /// <summary>Pixel size of <paramref name="text"/> at a scale, multi-line spacing included.</summary>
    public Vector2 Measure(string text, float scale)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Vector2.Zero;
        }

        float widest = 0f;
        float lineWidth = 0f;
        int lineCount = 1;

        foreach (char c in text)
        {
            if (c == '\n')
            {
                widest = MathF.Max(widest, lineWidth);
                lineWidth = 0f;
                lineCount++;
                continue;
            }

            lineWidth += GlyphAdvance * scale;
        }

        widest = MathF.Max(widest, lineWidth);
        float widthWithoutTrailingSpacing = widest > 0f ? widest - scale : 0f;

        return new Vector2(widthWithoutTrailingSpacing, lineCount * GlyphHeight * scale);
    }

    /// <summary>Draws <paramref name="text"/> from <paramref name="position"/>, one glyph at a time, dropping a row on '\n'.</summary>
    public void DrawString(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale = 1f)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        float cursorX = position.X;
        float cursorY = position.Y;

        foreach (char c in text)
        {
            if (c == '\n')
            {
                cursorX = position.X;
                cursorY += GlyphHeight * scale;
                continue;
            }

            if (c != ' ')
            {
                Rectangle sourceRect = GetSourceRect(c);
                var destination = new Rectangle(
                    (int)cursorX,
                    (int)cursorY,
                    (int)(GlyphWidth * scale),
                    (int)(GlyphHeight * scale));

                spriteBatch.Draw(_texture, destination, sourceRect, color);
            }

            cursorX += GlyphAdvance * scale;
        }
    }

    public void DrawStringCentered(SpriteBatch spriteBatch, string text, Vector2 center, Color color, float scale = 1f)
    {
        Vector2 size = Measure(text, scale);
        Vector2 topLeft = center - (size * 0.5f);
        DrawString(spriteBatch, text, topLeft, color, scale);
    }

    public void DrawStringRightAligned(
        SpriteBatch spriteBatch, string text, float right, float top, Color color, float scale = 1f) =>
        DrawString(spriteBatch, text, new Vector2(right - Measure(text, scale).X, top), color, scale);

    /// <summary>Safe to call twice.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _texture.Dispose();
        _disposed = true;
    }

    private static Rectangle GetSourceRect(char c)
    {
        char lookup = char.IsLower(c) ? char.ToUpperInvariant(c) : c;
        int ascii = lookup;

        int index;
        if (ascii >= 32 && ascii < 127)
        {
            index = ascii - 32;
        }
        else
        {
            index = 95; // Fallback glyph cell (solid square)
        }

        int col = index % AtlasColumns;
        int row = index / AtlasColumns;

        return new Rectangle(col * CellWidth, row * CellHeight, GlyphWidth, GlyphHeight);
    }
}
