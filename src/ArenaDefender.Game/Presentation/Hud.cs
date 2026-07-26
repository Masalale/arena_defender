using System;
using System.Collections.Generic;
using System.Globalization;
using ArenaDefender.Core.Mathematics;
using ArenaDefender.Core.Simulation;
using ArenaDefender.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ArenaDefender.Presentation;

public sealed class Hud
{
    private const float HealthBarSharpness = 9f;
    private const float ScoreSharpness = 11f;

    private const int MarginX = 22;
    private const int MarginY = 18;
    private const int BarWidth = 300;
    private const int BarHeight = 20;

    private readonly Texture2D _pixel;
    private readonly BitmapFont _font;

    private const float AnnouncementSeconds = 2.6f;

    private float _displayedHealth = 1f;
    private float _displayedScore;
    private string _announcement = string.Empty;
    private float _announcementRemaining;

    /// <exception cref="ArgumentNullException">Either argument was null.</exception>
    public Hud(TextureFactory textures, BitmapFont font)
    {
        ArgumentNullException.ThrowIfNull(textures);
        ArgumentNullException.ThrowIfNull(font);

        _pixel = textures.Pixel;
        _font = font;
    }

    /// <summary>Snaps the animated readouts to reality and clears any banner. Called at the start of a run.</summary>
    /// <exception cref="ArgumentNullException">The world was null.</exception>
    public void Reset(GameWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);

        _displayedHealth = world.Player.HealthFraction;
        _displayedScore = world.Score.Score;
        _announcement = string.Empty;
        _announcementRemaining = 0f;
    }

    public void Announce(string text)
    {
        _announcement = text ?? string.Empty;
        _announcementRemaining = AnnouncementSeconds;
    }

    /// <summary>Eases the readouts towards the values the simulation actually holds.</summary>
    /// <exception cref="ArgumentNullException">The world was null.</exception>
    public void Update(float deltaSeconds, GameWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (deltaSeconds <= 0f)
        {
            return;
        }

        // Interpolated rather than assigned, so a hit reads as a visible drain instead of a jump.
        _displayedHealth = GameMath.Damp(
            _displayedHealth, world.Player.HealthFraction, HealthBarSharpness, deltaSeconds);

        _displayedScore = GameMath.Damp(
            _displayedScore, world.Score.Score, ScoreSharpness, deltaSeconds);

        _announcementRemaining = MathF.Max(0f, _announcementRemaining - deltaSeconds);
    }

    /// <exception cref="ArgumentNullException">Either argument was null.</exception>
    public void Draw(SpriteBatch spriteBatch, GameWorld world, Rectangle viewport, float totalSeconds)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);
        ArgumentNullException.ThrowIfNull(world);

        DrawHealthBar(spriteBatch, world);
        DrawLives(spriteBatch, world);
        DrawScoreAndWave(spriteBatch, world, viewport);
        DrawCombo(spriteBatch, world, viewport, totalSeconds);
        DrawActiveEffects(spriteBatch, world, viewport);
        DrawAnnouncement(spriteBatch, viewport);
    }

    private void DrawAnnouncement(SpriteBatch spriteBatch, Rectangle viewport)
    {
        if (_announcementRemaining <= 0f || _announcement.Length == 0)
        {
            return;
        }

        float life = GameMath.Clamp01(_announcementRemaining / AnnouncementSeconds);

        // Hold at full opacity for most of the banner's life, then fade over the final third.
        float alpha = GameMath.Clamp01(GameMath.Remap(life, 0f, 0.35f, 0f, 1f));
        float rise = GameMath.Lerp(26f, 0f, life);

        var centre = new Vector2(viewport.Center.X, (viewport.Center.Y * 0.55f) - rise);

        _font.DrawStringCentered(spriteBatch, _announcement, centre, Palette.Accent * alpha, 3f);
    }

    private void DrawHealthBar(SpriteBatch spriteBatch, GameWorld world)
    {
        var frame = new Rectangle(MarginX, MarginY, BarWidth, BarHeight);
        float actual = world.Player.HealthFraction;

        Primitives.FillRectangle(spriteBatch, _pixel, frame, new Color(16, 20, 34));

        int drawnWidth = (int)(BarWidth * GameMath.Clamp01(_displayedHealth));
        Primitives.FillRectangle(
            spriteBatch,
            _pixel,
            new Rectangle(frame.X, frame.Y, drawnWidth, frame.Height),
            Palette.Blend(Palette.HealthEmpty, Palette.HealthFull, _displayedHealth));

        // The stretch the bar is still travelling over is highlighted, so a hit shows a red tail
        // draining away and a repair shows a pale band filling in.
        int trueWidth = (int)(BarWidth * actual);
        int from = Math.Min(drawnWidth, trueWidth);
        int to = Math.Max(drawnWidth, trueWidth);

        if (to - from > 1)
        {
            Color delta = actual < _displayedHealth ? Palette.HealthEmpty : Color.White;
            Primitives.FillRectangle(
                spriteBatch, _pixel, new Rectangle(frame.X + from, frame.Y, to - from, frame.Height), delta * 0.85f);
        }

        Primitives.OutlineRectangle(spriteBatch, _pixel, frame, 2, Palette.Border);

        string label = ((int)MathF.Ceiling(world.Player.Health)).ToString(CultureInfo.InvariantCulture);
        _font.DrawString(spriteBatch, "HP", new Vector2(frame.Right + 12, frame.Y + 3), Palette.TextDim, 2f);
        _font.DrawString(spriteBatch, label, new Vector2(frame.Right + 50, frame.Y + 3), Palette.Text, 2f);
    }

    private void DrawLives(SpriteBatch spriteBatch, GameWorld world)
    {
        int top = MarginY + BarHeight + 10;
        _font.DrawString(spriteBatch, "LIVES", new Vector2(MarginX, top + 1), Palette.TextDim, 2f);

        int left = MarginX + 72;

        for (int i = 0; i < world.Player.Lives; i++)
        {
            Primitives.FillRectangle(
                spriteBatch, _pixel, new Rectangle(left + (i * 20), top, 13, 13), Palette.Player);
        }
    }

    private void DrawScoreAndWave(SpriteBatch spriteBatch, GameWorld world, Rectangle viewport)
    {
        string score = ((int)MathF.Round(_displayedScore)).ToString("N0", CultureInfo.InvariantCulture);
        _font.DrawStringRightAligned(
            spriteBatch, score, viewport.Right - MarginX, MarginY, Palette.Text, 3f);

        string wave = "WAVE " + world.WaveNumber.ToString(CultureInfo.InvariantCulture);
        _font.DrawStringRightAligned(
            spriteBatch, wave, viewport.Right - MarginX, MarginY + 28, Palette.TextDim, 2f);
    }

    private void DrawCombo(SpriteBatch spriteBatch, GameWorld world, Rectangle viewport, float totalSeconds)
    {
        ScoreBoard score = world.Score;

        if (score.ComboCount <= 0)
        {
            return;
        }

        // The multiplier is worth more the longer it survives, so the text grows with it.
        float emphasis = GameMath.Remap(score.ComboMultiplier, 1f, 5f, 2.4f, 3.6f);

        string text = "X" + score.ComboMultiplier.ToString("0.0", CultureInfo.InvariantCulture);
        var centre = new Vector2(viewport.Center.X, MarginY + 20);

        _font.DrawStringCentered(spriteBatch, text, centre, Palette.Accent, emphasis);

        // A draining bar underneath shows exactly how long is left to extend the chain.
        float remaining = GameMath.Clamp01(score.ComboSecondsRemaining / 2.5f);
        var track = new Rectangle(viewport.Center.X - 60, (int)centre.Y + 22, 120, 4);

        Primitives.FillRectangle(spriteBatch, _pixel, track, Color.Black * 0.5f);
        Primitives.FillRectangle(
            spriteBatch,
            _pixel,
            new Rectangle(track.X, track.Y, (int)(track.Width * remaining), track.Height),
            Palette.Accent * 0.9f);
    }

    private void DrawActiveEffects(SpriteBatch spriteBatch, GameWorld world, Rectangle viewport)
    {
        IReadOnlyList<ActiveEffect> effects = world.PowerUps.ActiveEffects;

        if (effects.Count == 0)
        {
            return;
        }

        const int rowHeight = 22;
        const int trackWidth = 150;

        // Wide enough for the longest name at this scale: 13 glyphs at 6px advance, doubled, plus a
        // gap before the timer bar.
        const int labelWidth = 168;
        int top = viewport.Bottom - MarginY - (effects.Count * rowHeight);

        for (int i = 0; i < effects.Count; i++)
        {
            ActiveEffect effect = effects[i];
            Color colour = Palette.ForPowerUp(effect.Kind);
            int rowTop = top + (i * rowHeight);

            _font.DrawString(
                spriteBatch, Palette.DisplayName(effect.Kind), new Vector2(MarginX, rowTop), colour, 2f);

            var track = new Rectangle(MarginX + labelWidth, rowTop + 2, trackWidth, 10);
            Primitives.FillRectangle(spriteBatch, _pixel, track, new Color(16, 20, 34));

            Primitives.FillRectangle(
                spriteBatch,
                _pixel,
                new Rectangle(track.X, track.Y, (int)(trackWidth * GameMath.Clamp01(effect.Fraction)), track.Height),
                colour);

            string seconds = MathF.Ceiling(effect.RemainingSeconds).ToString(CultureInfo.InvariantCulture) + "S";
            _font.DrawString(spriteBatch, seconds, new Vector2(track.Right + 10, rowTop), Palette.TextDim, 2f);
        }
    }
}
