using System;
using System.IO;
using ArenaDefender.Core.Entities;
using ArenaDefender.Core.Entities.Enemies;
using ArenaDefender.Core.Mathematics;
using ArenaDefender.Core.Simulation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimVector2 = System.Numerics.Vector2;

namespace ArenaDefender.Presentation;

public sealed class GameRenderer
{
    /// <summary>Seconds an enemy stays lit up white after being hit.</summary>
    private const float HitFlashSeconds = 0.14f;

    /// <summary>Rays used to fill a sentry's vision cone.</summary>
    private const int ConeRayCount = 20;

    /// <summary>The sprites point up, while a facing angle of zero points right.</summary>
    private const float SpriteFacingOffset = MathF.PI / 2f;

    private readonly Texture2D _pixel;
    private readonly Texture2D _circle;
    private readonly Texture2D _ring;
    private readonly Texture2D _background;
    private readonly Texture2D _playerSprite;
    private readonly Texture2D _chaserSprite;
    private readonly Texture2D _sentrySprite;
    private readonly Texture2D _bruteSprite;

    /// <exception cref="ArgumentNullException">The factory was null.</exception>
    /// <exception cref="FileNotFoundException">A texture file was missing.</exception>
    public GameRenderer(TextureFactory textures)
    {
        ArgumentNullException.ThrowIfNull(textures);

        _pixel = textures.Pixel;
        _background = textures.Background;

        // Stored large and scaled down at draw time, so a brute and a spark can share one texture
        // without the smaller of the two looking chunky.
        _circle = textures.Sprite("circle.png");
        _ring = textures.Sprite("ring.png");

        _playerSprite = textures.Sprite("player.png");
        _chaserSprite = textures.Sprite("enemy_chaser.png");
        _sentrySprite = textures.Sprite("enemy_sentry.png");
        _bruteSprite = textures.Sprite("enemy_brute.png");
    }

    /// <summary>Draw order: cones, pickups and projectiles sit under the ships.</summary>
    /// <exception cref="ArgumentNullException">Either argument was null.</exception>
    public void Draw(SpriteBatch spriteBatch, GameWorld world, Vector2 offset, float totalSeconds)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);
        ArgumentNullException.ThrowIfNull(world);

        DrawArena(spriteBatch, world.Bounds, offset);

        foreach (Enemy enemy in world.Enemies)
        {
            if (enemy is SentryEnemy sentry)
            {
                DrawVisionCone(spriteBatch, sentry, offset);
            }
        }

        foreach (PowerUp pickup in world.Pickups)
        {
            DrawPickup(spriteBatch, pickup, offset);
        }

        foreach (Projectile projectile in world.Projectiles)
        {
            DrawProjectile(spriteBatch, projectile, offset);
        }

        foreach (Enemy enemy in world.Enemies)
        {
            DrawEnemy(spriteBatch, enemy, offset);
        }

        DrawPlayer(spriteBatch, world.Player, offset, totalSeconds);
    }

    /// <summary>Used behind the home screen.</summary>
    /// <exception cref="ArgumentNullException">The sprite batch was null.</exception>
    public void DrawBackdrop(SpriteBatch spriteBatch, ArenaBounds bounds, Vector2 offset)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);
        DrawArena(spriteBatch, bounds, offset);
    }

    private void DrawArena(SpriteBatch spriteBatch, ArenaBounds bounds, Vector2 offset)
    {
        int width = (int)bounds.Width;
        int height = (int)bounds.Height;
        int originX = (int)offset.X;
        int originY = (int)offset.Y;

        spriteBatch.Draw(_background, new Rectangle(originX, originY, width, height), Color.White);

        Primitives.OutlineRectangle(
            spriteBatch, _pixel, new Rectangle(originX, originY, width, height), 3, Palette.Border);

        // A second, dimmer inset line gives the wall a little depth without needing a shader.
        Primitives.OutlineRectangle(
            spriteBatch,
            _pixel,
            new Rectangle(originX + 5, originY + 5, width - 10, height - 10),
            1,
            Palette.Border * 0.35f);
    }

    /// <summary>Filled fan with a solid rim.</summary>
    private void DrawVisionCone(SpriteBatch spriteBatch, SentryEnemy sentry, Vector2 offset)
    {
        float halfAngle = SentryEnemy.ConeHalfAngle;
        float range = SentryEnemy.ConeRange;
        float centreAngle = GameMath.ToAngle(sentry.Facing);

        Vector2 apex = sentry.Position.ToXna(offset);
        Color colour = sentry.HasTargetInSight ? Palette.ConeAlert : Palette.ConeIdle;

        Vector2 Tip(float angle) => apex + (GameMath.FromAngle(angle).ToXna() * range);

        float step = halfAngle * 2f / (ConeRayCount - 1);
        // A little wider than the exact gap between rays, so the fan reads as a solid wedge rather
        // than as a set of stripes.
        float rayThickness = (range * step) + 3.5f;
        float fillAlpha = sentry.HasTargetInSight ? 0.16f : 0.075f;

        for (int i = 0; i < ConeRayCount; i++)
        {
            Primitives.DrawLine(
                spriteBatch,
                _pixel,
                apex,
                Tip(centreAngle - halfAngle + (i * step)),
                rayThickness,
                colour * fillAlpha);
        }

        // The rim keeps the cone's exact edges readable over a busy background.
        const int arcSegments = 10;
        float edgeAlpha = sentry.HasTargetInSight ? 0.85f : 0.4f;
        Vector2 leftTip = Tip(centreAngle - halfAngle);

        Primitives.DrawLine(spriteBatch, _pixel, apex, leftTip, 2f, colour * edgeAlpha);
        Primitives.DrawLine(
            spriteBatch, _pixel, apex, Tip(centreAngle + halfAngle), 2f, colour * edgeAlpha);

        Vector2 previous = leftTip;

        for (int i = 1; i <= arcSegments; i++)
        {
            Vector2 point = Tip(centreAngle - halfAngle + (halfAngle * 2f * i / arcSegments));
            Primitives.DrawLine(spriteBatch, _pixel, previous, point, 2f, colour * edgeAlpha);
            previous = point;
        }
    }

    private void DrawEnemy(SpriteBatch spriteBatch, Enemy enemy, Vector2 offset)
    {
        Vector2 centre = enemy.Position.ToXna(offset);
        float diameter = enemy.Radius * 2f;
        float rotation = GameMath.ToAngle(enemy.Facing);
        Color colour = ApplyHitFlash(BaseColourFor(enemy), enemy.SecondsSinceHit);

        // The art fills 75% of its frame, so a scale is 4/3 of the size the ship should look.
        (Texture2D Sprite, float Scale) art = enemy switch
        {
            BruteEnemy => (_bruteSprite, 1.53f),
            SentryEnemy => (_sentrySprite, 1.96f),
            _ => (_chaserSprite, 2.15f)
        };

        // A faint tinted disc behind the sprite keeps each type's colour readable in a crowd,
        // and carries the hit flash that the sprite's own colours would otherwise swallow.
        Primitives.DrawCentered(spriteBatch, _circle, centre, diameter * 1.15f, colour * 0.22f);
        Primitives.DrawCentered(
            spriteBatch, art.Sprite, centre, diameter * art.Scale, colour, rotation + SpriteFacingOffset);

        DrawEnemyHealth(spriteBatch, enemy, centre);
    }

    // Health bar above an enemy, but only once it has actually been hurt.
    private void DrawEnemyHealth(SpriteBatch spriteBatch, Enemy enemy, Vector2 centre)
    {
        if (enemy.HealthFraction >= 0.999f)
        {
            return;
        }

        int width = (int)MathF.Max(18f, enemy.Radius * 2.4f);
        int height = 3;
        int left = (int)(centre.X - (width * 0.5f));
        int top = (int)(centre.Y - enemy.Radius - 11f);

        Primitives.FillRectangle(
            spriteBatch, _pixel, new Rectangle(left, top, width, height), Color.Black * 0.6f);

        int filled = (int)(width * enemy.HealthFraction);

        Primitives.FillRectangle(
            spriteBatch,
            _pixel,
            new Rectangle(left, top, filled, height),
            Palette.Blend(Palette.HealthEmpty, Palette.HealthFull, enemy.HealthFraction));
    }

    private void DrawPlayer(SpriteBatch spriteBatch, Player player, Vector2 offset, float totalSeconds)
    {
        if (!player.IsActive)
        {
            return;
        }

        Vector2 centre = player.Position.ToXna(offset);
        float diameter = player.Radius * 2f;
        float rotation = GameMath.ToAngle(player.Facing);

        // Immunity is shown as a flicker rather than a static tint, because a tint is easy to miss
        // in a busy frame while a flicker is not.
        float flash = player.IsInvulnerable
            ? GameMath.Lerp(0.3f, 1f, (MathF.Sin(totalSeconds * 26f) + 1f) * 0.5f)
            : 1f;

        Primitives.DrawCentered(spriteBatch, _circle, centre, diameter * 1.8f, Palette.Player * 0.12f);

        Primitives.DrawCentered(
            spriteBatch,
            _playerSprite,
            centre,
            diameter * 2.25f,
            Color.White * flash,
            rotation + SpriteFacingOffset);

        if (player.ShieldCharges > 0)
        {
            DrawShield(spriteBatch, centre, diameter, player.ShieldCharges);
        }
    }

    // One ring per shield charge left, each fainter than the last.
    private void DrawShield(SpriteBatch spriteBatch, Vector2 centre, float diameter, int charges)
    {
        for (int i = 0; i < charges; i++)
        {
            float ringDiameter = diameter * (2f + (i * 0.32f));
            Primitives.DrawCentered(
                spriteBatch, _ring, centre, ringDiameter, Palette.Shield * (0.8f / (i + 1f)));
        }
    }

    private void DrawProjectile(SpriteBatch spriteBatch, Projectile projectile, Vector2 offset)
    {
        Vector2 centre = projectile.Position.ToXna(offset);
        float diameter = projectile.Radius * 2f;

        bool fromPlayer = projectile.Owner == ProjectileOwner.Player;
        Color colour = fromPlayer ? Palette.PlayerShot : Palette.EnemyShot;

        // Fully bright for most of its flight, then interpolated away over the last stretch so an
        // expiring shot warns the player instead of blinking out.
        float alpha = GameMath.Remap(projectile.LifeFraction, 0.6f, 1f, 1f, 0f);

        Primitives.DrawCentered(spriteBatch, _circle, centre, diameter * 2.6f, colour * (alpha * 0.2f));
        Primitives.DrawCentered(spriteBatch, _circle, centre, diameter, colour * alpha);

        // A short tail in the direction of travel reads as speed at a glance.
        SimVector2 heading = GameMath.SafeNormalize(projectile.Velocity);

        if (heading != SimVector2.Zero)
        {
            Vector2 tail = centre - (heading.ToXna() * diameter * 2.2f);
            Primitives.DrawLine(spriteBatch, _pixel, centre, tail, diameter * 0.6f, colour * (alpha * 0.4f));
        }
    }

    private void DrawPickup(SpriteBatch spriteBatch, PowerUp pickup, Vector2 offset)
    {
        Vector2 centre = pickup.Position.ToXna(offset);
        Color colour = Palette.ForPowerUp(pickup.Kind);

        float alpha = pickup.Opacity;
        float diameter = pickup.Radius * 2f;

        Primitives.DrawCentered(spriteBatch, _circle, centre, diameter * 1.9f, colour * (alpha * 0.16f));
        Primitives.DrawCentered(spriteBatch, _ring, centre, diameter * 1.15f, colour * alpha);
        Primitives.DrawCentered(spriteBatch, _circle, centre, diameter * 0.55f, colour * alpha);
    }

    private static Color ApplyHitFlash(Color baseColour, float secondsSinceHit)
    {
        if (secondsSinceHit >= HitFlashSeconds)
        {
            return baseColour;
        }

        float flash = 1f - GameMath.Clamp01(secondsSinceHit / HitFlashSeconds);
        return Palette.Blend(baseColour, Color.White, flash);
    }

    private static Color BaseColourFor(Enemy enemy) => enemy switch
    {
        BruteEnemy => Palette.Brute,
        SentryEnemy => Palette.Sentry,
        _ => Palette.Chaser
    };
}
