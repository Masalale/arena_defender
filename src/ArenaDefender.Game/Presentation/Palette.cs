using ArenaDefender.Core.Entities;
using ArenaDefender.Core.Mathematics;
using Microsoft.Xna.Framework;

namespace ArenaDefender.Presentation;

/// <summary>The game's palette and the colour blending the renderer uses.</summary>
public static class Palette
{
    public static readonly Color Background = new(9, 11, 20);

    public static readonly Color Border = new(70, 96, 160);

    /// <summary>The player's hull.</summary>
    public static readonly Color Player = new(96, 224, 255);

    public static readonly Color Shield = new(140, 255, 190);

    /// <summary>Fast and fragile, dives at the player.</summary>
    public static readonly Color Chaser = new(255, 96, 120);

    /// <summary>Slow and armoured, soaks hits.</summary>
    public static readonly Color Brute = new(214, 128, 255);

    /// <summary>Stands off and shoots.</summary>
    public static readonly Color Sentry = new(255, 186, 82);

    public static readonly Color ConeIdle = new(120, 180, 255);

    /// <summary>The sentry's cone once it has spotted the player.</summary>
    public static readonly Color ConeAlert = new(255, 70, 70);

    public static readonly Color PlayerShot = new(180, 250, 255);

    public static readonly Color EnemyShot = new(255, 150, 70);

    public static readonly Color Text = new(226, 234, 250);

    public static readonly Color TextDim = new(126, 142, 178);

    /// <summary>Title and combo readout accent.</summary>
    public static readonly Color Accent = new(255, 214, 102);

    public static readonly Color HealthFull = new(96, 230, 140);

    public static readonly Color HealthEmpty = new(232, 68, 68);

    /// <summary>The colour a power up wears, on the ground and in the HUD.</summary>
    public static Color ForPowerUp(PowerUpKind kind) => kind switch
    {
        PowerUpKind.Repair => new Color(96, 230, 140),
        PowerUpKind.RapidFire => new Color(255, 226, 96),
        PowerUpKind.DoubleDamage => new Color(255, 122, 168),
        PowerUpKind.SpeedBoost => new Color(120, 200, 255),
        _ => new Color(150, 255, 210)
    };

    /// <summary>Label next to a power up timer. Names match the README.</summary>
    public static string DisplayName(PowerUpKind kind) => kind switch
    {
        PowerUpKind.Repair => "REPAIR",
        PowerUpKind.RapidFire => "RAPID FIRE",
        PowerUpKind.DoubleDamage => "DOUBLE DAMAGE",
        PowerUpKind.SpeedBoost => "SPEED BOOST",
        _ => "SHIELD"
    };

    /// <summary>Blends two colours channel by channel.</summary>
    /// <param name="from">Colour returned when <paramref name="t"/> is 0.</param>
    /// <param name="to">Colour returned when <paramref name="t"/> is 1.</param>
    /// <param name="t">Blend factor, clamped to 0..1.</param>
    public static Color Blend(Color from, Color to, float t) => new(
        (int)GameMath.Lerp(from.R, to.R, t),
        (int)GameMath.Lerp(from.G, to.G, t),
        (int)GameMath.Lerp(from.B, to.B, t));
}
