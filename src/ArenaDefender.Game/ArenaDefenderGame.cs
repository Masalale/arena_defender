using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ArenaDefender.Core.Simulation;
using ArenaDefender.Presentation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SimVector2 = System.Numerics.Vector2;

namespace ArenaDefender;

/// <summary>MonoGame host. Window, graphics device and frame loop live here; game rules do not.</summary>
public class ArenaDefenderGame : Game
{
    /// <summary>How long the results screen swallows input, so a stray press can't skip it.</summary>
    private const float ResultsInputDelay = 0.7f;

    /// <summary>Pause menu entries. The order drives both drawing and selection.</summary>
    private static readonly string[] PauseOptions = { "RESUME", "RESTART", "HOME" };

    private const int LeaderboardSize = 3;

    private const int MaxResizeAttempts = 3;

    private readonly GraphicsDeviceManager _graphics;
    private readonly GameWorld _world;
    private readonly InputMapper _input = new();
    private readonly CameraShake _shake = new();

    private SpriteBatch _spriteBatch = null!;
    private TextureFactory _textures = null!;
    private BitmapFont _font = null!;
    private GameRenderer _renderer = null!;
    private Hud _hud = null!;

    // Null when the machine has no usable audio device. The game is fully playable without it.
    private SoundBank? _sound;

    private float _totalSeconds;
    private float _resultsTimer;
    private GameState _lastState;
    private bool _contentReady;
    private bool _paused;
    private int _pauseSelection;
    private bool _restoringWindowSize;
    private int _resizeAttempts;

    // Final score of every completed run this session, highest first. Deliberately not written to
    // disk: a file store would need its own failure handling for a feature nothing else depends on.
    private readonly List<int> _sessionScores = new();

    public ArenaDefenderGame()
    {
        _world = new GameWorld();

        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = (int)_world.Bounds.Width,
            PreferredBackBufferHeight = (int)_world.Bounds.Height
        };

        Content.RootDirectory = string.Empty;
        IsMouseVisible = true;
        Window.Title = "Arena Defender";

        // AllowUserResizing is only a hint a window manager may ignore, so the size is forced back too.
        Window.AllowUserResizing = false;
        _graphics.IsFullScreen = false;
        Window.ClientSizeChanged += OnClientSizeChanged;

        _lastState = _world.State;

        _world.PlayerDamaged += OnPlayerDamaged;
        _world.WaveReached += OnWaveReached;
        _world.PlayerFired += OnPlayerFired;
        _world.EnemyDestroyed += OnEnemyDestroyed;
        _world.PowerUpCollected += OnPowerUpCollected;
    }

    protected override void LoadContent()
    {
        if (GraphicsDevice is null)
        {
            throw new InvalidOperationException(
                "Arena Defender could not start because no graphics device was created.");
        }

        try
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _textures = new TextureFactory(GraphicsDevice);
            _font = new BitmapFont(GraphicsDevice);
            _renderer = new GameRenderer(_textures);
            _hud = new Hud(_textures, _font);

            _contentReady = true;
        }
        catch (InvalidOperationException)
        {
            // Already carries a message naming what failed and how to fix it.
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Arena Defender could not build its drawing resources, which usually means the " +
                "graphics device was lost or refused a texture.",
                ex);
        }

        // Audio is optional. A machine with no output device, which includes some headless and
        // remote desktop setups, should still get a playable game rather than a startup failure.
        try
        {
            _sound = new SoundBank();
            if (_world.State == GameState.Menu)
            {
                _sound.PlayLobbyMusic();
            }
        }
        catch (Exception)
        {
            _sound = null;
        }

        _hud.Reset(_world);
    }

    protected override void Update(GameTime gameTime)
    {
        ArgumentNullException.ThrowIfNull(gameTime);

        float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _totalSeconds += delta;

        _input.Update(delta);
        _shake.Update(delta);
        _sound?.Update(delta);

        switch (_world.State)
        {
            case GameState.Playing when !_paused:
                UpdatePlaying(delta);
                break;

            case GameState.Playing:
                ReleaseMouse();
                UpdatePaused();
                break;

            case GameState.GameOver:
                ReleaseMouse();
                UpdateResults(delta);
                break;

            default:
                ReleaseMouse();
                UpdateMenu();
                break;
        }

        NoticeStateChange();
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // A minimised or zero width window leaves nothing to draw into, and asking SpriteBatch to
        // work against it throws rather than drawing nothing.
        if (!_contentReady || !HasDrawableSurface())
        {
            return;
        }

        GraphicsDevice.Clear(Palette.Background);

        Rectangle viewport = GraphicsDevice.Viewport.Bounds;

        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp);

        if (_world.State == GameState.Menu)
        {
            // Only the empty arena, so nothing competes with the title text for the middle of the
            // screen. The results screen keeps its scene, because that scene is the outcome.
            _renderer.DrawBackdrop(_spriteBatch, _world.Bounds, _shake.Offset);
            DrawMenuScreen(viewport);
        }
        else
        {
            _renderer.Draw(_spriteBatch, _world, _shake.Offset, _totalSeconds);

            if (_world.State == GameState.Playing)
            {
                _hud.Draw(_spriteBatch, _world, viewport, _totalSeconds);

                if (_paused)
                {
                    DrawPauseScreen(viewport);
                }
            }
            else
            {
                DrawResultsScreen(viewport);
            }
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReleaseMouse();
            Window.ClientSizeChanged -= OnClientSizeChanged;

            _world.PlayerDamaged -= OnPlayerDamaged;
            _world.WaveReached -= OnWaveReached;
            _world.PlayerFired -= OnPlayerFired;
            _world.EnemyDestroyed -= OnEnemyDestroyed;
            _world.PowerUpCollected -= OnPowerUpCollected;

            _sound?.Dispose();
            _font?.Dispose();
            _textures?.Dispose();
            _spriteBatch?.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>The home screen, and the only place the game can actually be closed.</summary>
    private void UpdateMenu()
    {
        if (_input.BackPressed)
        {
            Exit();
            return;
        }

        if (_input.ConfirmPressed)
        {
            StartRun();
        }
    }

    private void UpdatePlaying(float delta)
    {
        if (_input.BackPressed)
        {
            _paused = true;
            _pauseSelection = 0;
            return;
        }

        ConfineMouseToWindow();

        SimVector2 playerScreenPosition = _world.Player.Position + _shake.Offset.ToNumerics();

        _world.Update(delta, _input.BuildIntent(playerScreenPosition));
        _hud.Update(delta, _world);
    }

    /// <summary>Snaps the window back to the arena size if anything resizes it.</summary>
    private void OnClientSizeChanged(object? sender, EventArgs e)
    {
        if (_restoringWindowSize || _resizeAttempts >= MaxResizeAttempts)
        {
            return;
        }

        int width = (int)_world.Bounds.Width;
        int height = (int)_world.Bounds.Height;

        if (Window.ClientBounds.Width == width && Window.ClientBounds.Height == height)
        {
            _resizeAttempts = 0;
            return;
        }

        // A window manager that refuses the size would otherwise be fought forever, so the attempts
        // are capped. ApplyChanges also raises this event again, which the flag absorbs.
        _resizeAttempts++;
        _restoringWindowSize = true;

        try
        {
            _graphics.PreferredBackBufferWidth = width;
            _graphics.PreferredBackBufferHeight = height;
            _graphics.ApplyChanges();
        }
        finally
        {
            _restoringWindowSize = false;
        }
    }

    private void ConfineMouseToWindow()
    {
        // Alt-tabbing away must hand the pointer back, or the game looks frozen on another window.
        if (!IsActive)
        {
            ReleaseMouse();
            return;
        }

        Viewport viewport = GraphicsDevice.Viewport;
        MouseGrab.Confine(Window.Handle, viewport.Width, viewport.Height);

        Point position = _input.MousePosition;

        int x = Math.Clamp(position.X, 1, Math.Max(1, viewport.Width - 2));
        int y = Math.Clamp(position.Y, 1, Math.Max(1, viewport.Height - 2));

        if (x != position.X || y != position.Y)
        {
            Mouse.SetPosition(x, y);
        }
    }

    /// <summary>Hands the pointer back, so leaving play doesn't trap it in the window.</summary>
    private void ReleaseMouse() => MouseGrab.Release(Window.Handle);

    private void UpdatePaused()
    {
        if (_input.BackPressed)
        {
            _paused = false;
            return;
        }

        if (_input.MenuUpPressed)
        {
            _pauseSelection = (_pauseSelection + PauseOptions.Length - 1) % PauseOptions.Length;
        }

        if (_input.MenuDownPressed)
        {
            _pauseSelection = (_pauseSelection + 1) % PauseOptions.Length;
        }

        if (!_input.ConfirmPressed)
        {
            return;
        }

        switch (_pauseSelection)
        {
            case 0:
                _paused = false;
                break;

            case 1:
                StartRun();
                break;

            default:
                ReturnToTitle();
                break;
        }
    }

    /// <summary>The results screen. Back goes home instead of closing the game.</summary>
    private void UpdateResults(float delta)
    {
        _resultsTimer += delta;

        if (_input.BackPressed)
        {
            ReturnToTitle();
            return;
        }

        if (_resultsTimer >= ResultsInputDelay && _input.ConfirmPressed)
        {
            StartRun();
        }
    }

    /// <summary>Spots a screen change and reacts once.</summary>
    private void NoticeStateChange()
    {
        if (_world.State == _lastState)
        {
            return;
        }

        _lastState = _world.State;

        if (_world.State == GameState.Menu)
        {
            _sound?.PlayLobbyMusic();
        }
        else if (_world.State == GameState.Playing)
        {
            _sound?.StopLobbyMusic();
        }
        else if (_world.State == GameState.GameOver)
        {
            _resultsTimer = 0f;

            _sessionScores.Add(_world.Score.Score);
            _sessionScores.Sort((first, second) => second.CompareTo(first));
        }
    }

    private void StartRun()
    {
        _world.StartNewRun();
        _shake.Reset();
        _hud.Reset(_world);
        _resultsTimer = 0f;
        _paused = false;
        _lastState = _world.State;
    }

    private void ReturnToTitle()
    {
        _world.ReturnToMenu();
        _shake.Reset();
        _paused = false;
        _lastState = _world.State;
    }

    private void DrawMenuScreen(Rectangle viewport)
    {
        Vector2 centre = Scrim(viewport, 0.62f);

        Centred("ARENA DEFENDER", centre.X, centre.Y - 170f, Palette.Accent, 7f);
        Centred("SURVIVE THE WAVES.  KEEP THE COMBO ALIVE.", centre.X, centre.Y - 116f, Palette.TextDim, 2f);

        string[] controls =
        {
            "MOVE   WASD OR ARROW KEYS",
            "AIM    MOUSE",
            "FIRE   LEFT MOUSE",
            "PAUSE  ESCAPE",
            "QUIT   ESCAPE"
        };

        // Left aligned on a shared edge so the fixed width font lines the two columns up, and spaced
        // by hand because the font packs its own lines too tightly to read.
        float left = centre.X - 300f;

        Left("CONTROLS", left, centre.Y - 92f, Palette.Accent, 2.6f);

        for (int i = 0; i < controls.Length; i++)
        {
            Left(controls[i], left, centre.Y - 56f + (i * 30f), Palette.Text, 2.4f);
        }

        DrawEnemyLegend(centre.X, centre.Y + 116f);
        Centred("PRESS ENTER TO PLAY", centre.X, centre.Y + 186f, Palette.Text, 3f);
        DrawScores(centre.X, centre.Y + 226f, limit: 1);
    }

    private void DrawPauseScreen(Rectangle viewport)
    {
        Vector2 centre = Scrim(viewport, 0.7f);

        Centred("PAUSED", centre.X, centre.Y - 70f, Palette.Accent, 6f);

        for (int i = 0; i < PauseOptions.Length; i++)
        {
            bool selected = i == _pauseSelection;

            // The marker matters as much as the colour: the arena still shows through the scrim, so
            // colour alone is easy to lose against a bright frame.
            string label = selected ? "> " + PauseOptions[i] + " <" : PauseOptions[i];

            Centred(
                label,
                centre.X,
                centre.Y - 10f + (i * 34f),
                selected ? Palette.Accent : Palette.TextDim,
                selected ? 3f : 2.4f);
        }

        Centred("W/S OR UP/DOWN TO MOVE, ENTER TO SELECT, ESCAPE TO RESUME", centre.X, centre.Y + 116f, Palette.TextDim, 1.8f);
    }

    private void DrawEnemyLegend(float centreX, float y)
    {
        (string Label, Color Colour)[] entries =
        {
            ("CHASER - FAST", Palette.Chaser),
            ("BRUTE - ARMOURED", Palette.Brute),
            ("SENTRY - WATCHES", Palette.Sentry)
        };

        const float spacing = 250f;
        float left = centreX - (spacing * (entries.Length - 1) * 0.5f);

        for (int i = 0; i < entries.Length; i++)
        {
            Centred(entries[i].Label, left + (i * spacing), y, entries[i].Colour, 2f);
        }
    }

    private void DrawResultsScreen(Rectangle viewport)
    {
        Vector2 centre = Scrim(viewport, 0.78f);

        Centred("GAME OVER", centre.X, centre.Y - 200f, Palette.HealthEmpty, 6f);
        Centred("YOUR SCORE", centre.X, centre.Y - 142f, Palette.TextDim, 2f);
        Centred(Number(_world.Score.Score), centre.X, centre.Y - 114f, Palette.Accent, 5f);

        DrawScores(centre.X, centre.Y - 56f, LeaderboardSize);

        (string Label, string Value)[] rows =
        {
            ("ENEMIES DESTROYED", _world.Score.TotalKills.ToString(CultureInfo.InvariantCulture)),
            ("WAVES SURVIVED", _world.WaveNumber.ToString(CultureInfo.InvariantCulture))
        };

        for (int i = 0; i < rows.Length; i++)
        {
            float y = centre.Y + 60f + (i * 30f);
            Left(rows[i].Label, centre.X - 250f, y, Palette.TextDim, 2.4f);
            Right(rows[i].Value, centre.X + 250f, y, Palette.Text, 2.4f);
        }

        Centred("PRESS ENTER TO PLAY AGAIN", centre.X, centre.Y + 150f, Palette.Text, 3f);
        Centred("PRESS ESCAPE TO GO HOME", centre.X, centre.Y + 186f, Palette.TextDim, 1.8f);
    }

    private void DrawScores(float centreX, float y, int limit)
    {
        if (_sessionScores.Count == 0)
        {
            return;
        }

        if (limit == 1)
        {
            Centred("SESSION BEST  " + Number(_sessionScores[0]), centreX, y, Palette.TextDim, 2f);
            return;
        }

        Centred("SESSION BEST", centreX, y, Palette.TextDim, 2f);

        List<int> top = _sessionScores.Take(limit).ToList();

        for (int i = 0; i < top.Count; i++)
        {
            float row = y + 26f + (i * 26f);
            Left((i + 1).ToString(CultureInfo.InvariantCulture) + ".", centreX - 120f, row, Palette.TextDim, 2.2f);
            Right(Number(top[i]), centreX + 120f, row, Palette.Text, 2.2f);
        }
    }

    private Vector2 Scrim(Rectangle viewport, float strength)
    {
        Primitives.FillRectangle(_spriteBatch, _textures.Pixel, viewport, Palette.Background * strength);
        return new Vector2(viewport.Center.X, viewport.Center.Y);
    }

    private void Centred(string text, float x, float y, Color colour, float scale) =>
        _font.DrawStringCentered(_spriteBatch, text, new Vector2(x, y), colour, scale);

    private void Left(string text, float x, float y, Color colour, float scale) =>
        _font.DrawString(_spriteBatch, text, new Vector2(x, y), colour, scale);

    private void Right(string text, float right, float y, Color colour, float scale) =>
        _font.DrawStringRightAligned(_spriteBatch, text, right, y, colour, scale);

    private static string Number(int value) => value.ToString("N0", CultureInfo.InvariantCulture);

    private bool HasDrawableSurface()
    {
        Viewport viewport = GraphicsDevice.Viewport;
        return viewport.Width > 0 && viewport.Height > 0;
    }

    /// <summary>Jolts the camera when the player is hit. Damage should be felt, not just shown in a bar.</summary>
    private void OnPlayerDamaged(SimVector2 position)
    {
        _shake.Add(0.55f);
        _sound?.Play(GameSound.PlayerHit);
    }

    private void OnPlayerFired() => _sound?.PlayShot(_world.Player.ShotCount);

    private void OnEnemyDestroyed() => _sound?.Play(GameSound.EnemyDown);

    private void OnPowerUpCollected() => _sound?.Play(GameSound.PickUp);

    private void OnWaveReached(WaveReward reward)
    {
        if (reward.Wave <= 1)
        {
            return;
        }

        _sound?.Play(GameSound.Wave);

        if (reward.GrantedExtraShot)
        {
            _hud.Announce("WEAPON UPGRADE");
            _shake.Add(0.25f);
            return;
        }

        if (reward.GrantedExtraLife)
        {
            _hud.Announce("EXTRA LIFE");
            return;
        }

        if (reward.Points > 0)
        {
            _hud.Announce("BONUS  +" + Number(reward.Points));
            return;
        }

        _hud.Announce("WAVE " + reward.Wave.ToString(CultureInfo.InvariantCulture));
    }
}
