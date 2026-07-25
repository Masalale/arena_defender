using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ArenaDefender.Presentation;

public static class Primitives
{
    /// <exception cref="ArgumentNullException">Either argument was null.</exception>
    public static void FillRectangle(SpriteBatch spriteBatch, Texture2D pixel, Rectangle area, Color colour)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);
        ArgumentNullException.ThrowIfNull(pixel);

        spriteBatch.Draw(pixel, area, colour);
    }

    /// <exception cref="ArgumentNullException">The sprite batch or pixel texture was null.</exception>
    public static void OutlineRectangle(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        Rectangle area,
        int thickness,
        Color colour)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);
        ArgumentNullException.ThrowIfNull(pixel);

        if (thickness <= 0)
        {
            return;
        }

        spriteBatch.Draw(pixel, new Rectangle(area.Left, area.Top, area.Width, thickness), colour);
        spriteBatch.Draw(pixel, new Rectangle(area.Left, area.Bottom - thickness, area.Width, thickness), colour);
        spriteBatch.Draw(pixel, new Rectangle(area.Left, area.Top, thickness, area.Height), colour);
        spriteBatch.Draw(pixel, new Rectangle(area.Right - thickness, area.Top, thickness, area.Height), colour);
    }

    /// <exception cref="ArgumentNullException">The sprite batch or pixel texture was null.</exception>
    public static void DrawLine(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        Vector2 from,
        Vector2 to,
        float thickness,
        Color colour)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);
        ArgumentNullException.ThrowIfNull(pixel);

        Vector2 delta = to - from;
        float length = delta.Length();

        if (length <= float.Epsilon || thickness <= 0f)
        {
            return;
        }

        spriteBatch.Draw(
            pixel,
            from,
            null,
            colour,
            MathF.Atan2(delta.Y, delta.X),
            new Vector2(0f, 0.5f),
            new Vector2(length, thickness),
            SpriteEffects.None,
            0f);
    }

    /// <exception cref="ArgumentNullException">The sprite batch or texture was null.</exception>
    public static void DrawCentered(
        SpriteBatch spriteBatch,
        Texture2D texture,
        Vector2 centre,
        float diameter,
        Color colour,
        float rotation = 0f)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);
        ArgumentNullException.ThrowIfNull(texture);

        if (texture.Width <= 0 || diameter <= 0f)
        {
            return;
        }

        spriteBatch.Draw(
            texture,
            centre,
            null,
            colour,
            rotation,
            new Vector2(texture.Width * 0.5f, texture.Height * 0.5f),
            diameter / texture.Width,
            SpriteEffects.None,
            0f);
    }
}
