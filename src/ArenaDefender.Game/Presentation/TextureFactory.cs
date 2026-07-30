using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ArenaDefender.Presentation;

public sealed class TextureFactory : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly Dictionary<string, Texture2D> _cache = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    /// <exception cref="ArgumentNullException">The device was null.</exception>
    /// <exception cref="FileNotFoundException">A texture file was missing.</exception>
    public TextureFactory(GraphicsDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        _device = device;

        Pixel = LoadFromFile("pixel.png");
        Background = LoadFromFile("background/background.jpg");
    }

    /// <summary>One white pixel, stretched into whatever rectangle needs filling.</summary>
    public Texture2D Pixel { get; }

    public Texture2D Background { get; }

    /// <summary>Loads a sprite by file name, so the second ask doesn't touch disk again.</summary>
    public Texture2D Sprite(string fileName) => LoadFromFile(fileName);

    /// <summary>Safe to call twice.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (Texture2D texture in _cache.Values)
        {
            texture.Dispose();
        }

        _cache.Clear();
        _disposed = true;
    }

    private Texture2D LoadFromFile(string relativeFileName)
    {
        if (_cache.TryGetValue(relativeFileName, out Texture2D? cached))
        {
            return cached;
        }

        string fullPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Textures", relativeFileName);

        if (!File.Exists(fullPath))
        {
            // The background sits directly under Assets/ rather than in Assets/Textures/.
            string altPath = Path.Combine(AppContext.BaseDirectory, "Assets", relativeFileName);

            if (!File.Exists(altPath))
            {
                throw new FileNotFoundException(
                    $"Required texture asset not found at '{fullPath}'. Ensure Assets folder is copied to the build output.",
                    fullPath);
            }

            fullPath = altPath;
        }

        using FileStream stream = File.OpenRead(fullPath);
        Texture2D texture = Texture2D.FromStream(_device, stream);

        // Premultiplied alpha, or the SpriteBatch draws a white halo around every sprite.
        Color[] pixels = new Color[texture.Width * texture.Height];
        texture.GetData(pixels);
        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];
            float alphaFactor = c.A / 255f;
            pixels[i] = new Color(
                (byte)MathF.Round(c.R * alphaFactor),
                (byte)MathF.Round(c.G * alphaFactor),
                (byte)MathF.Round(c.B * alphaFactor),
                c.A);
        }
        texture.SetData(pixels);

        _cache[relativeFileName] = texture;
        return texture;
    }
}
