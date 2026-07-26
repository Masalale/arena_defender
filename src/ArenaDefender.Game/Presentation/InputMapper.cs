using ArenaDefender.Core.Input;
using ArenaDefender.Core.Mathematics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SimVector2 = System.Numerics.Vector2;

namespace ArenaDefender.Presentation;

/// <summary>
/// Translates keyboard and mouse into device independent intent. The only class in the game that
/// knows a key exists.
/// </summary>
public sealed class InputMapper
{
    /// <summary>How long mouse aiming stays in control after the mouse last moved.</summary>
    private const float MouseAimGraceSeconds = 1.25f;

    private KeyboardState _keyboard;
    private KeyboardState _previousKeyboard;
    private MouseState _mouse;

    private Point _lastMousePosition;
    private float _secondsSinceMouseMoved = float.MaxValue;

    public bool ConfirmPressed { get; private set; }

    public bool BackPressed { get; private set; }

    public bool MenuUpPressed { get; private set; }

    public bool MenuDownPressed { get; private set; }

    /// <summary>Where the cursor sits, in screen pixels.</summary>
    public Point MousePosition => _mouse.Position;

    private bool IsMouseAiming => _secondsSinceMouseMoved <= MouseAimGraceSeconds;

    public void Update(float deltaSeconds)
    {
        _previousKeyboard = _keyboard;

        _keyboard = Keyboard.GetState();
        _mouse = Mouse.GetState();

        TrackMouseMovement(deltaSeconds);

        // The mouse button is the fire control, so it deliberately does not confirm menu choices.
        ConfirmPressed = WasKeyJustPressed(Keys.Enter);
        BackPressed = WasKeyJustPressed(Keys.Escape);
        MenuUpPressed = WasKeyJustPressed(Keys.Up) || WasKeyJustPressed(Keys.W);
        MenuDownPressed = WasKeyJustPressed(Keys.Down) || WasKeyJustPressed(Keys.S);
    }

    /// <param name="playerScreenPosition">
    /// Where the player is drawn, so the aim knows which way the cursor lies.
    /// </param>
    public PlayerIntent BuildIntent(SimVector2 playerScreenPosition)
    {
        SimVector2 move = ReadMovement();
        SimVector2 aim = ReadAim(playerScreenPosition, move);
        bool fire = _mouse.LeftButton == ButtonState.Pressed;

        return new PlayerIntent(move, aim, fire);
    }

    private SimVector2 ReadMovement()
    {
        float x = 0f;
        float y = 0f;

        if (_keyboard.IsKeyDown(Keys.A) || _keyboard.IsKeyDown(Keys.Left))
        {
            x -= 1f;
        }

        if (_keyboard.IsKeyDown(Keys.D) || _keyboard.IsKeyDown(Keys.Right))
        {
            x += 1f;
        }

        if (_keyboard.IsKeyDown(Keys.W) || _keyboard.IsKeyDown(Keys.Up))
        {
            y -= 1f;
        }

        if (_keyboard.IsKeyDown(Keys.S) || _keyboard.IsKeyDown(Keys.Down))
        {
            y += 1f;
        }

        // PlayerIntent normalises for us, so a diagonal is not faster than a straight line.
        return new SimVector2(x, y);
    }

    private SimVector2 ReadAim(SimVector2 playerScreenPosition, SimVector2 moveDirection)
    {
        if (!IsMouseAiming)
        {
            return moveDirection;
        }

        SimVector2 toCursor = _mouse.Position.ToNumerics() - playerScreenPosition;
        SimVector2 aim = GameMath.SafeNormalize(toCursor);

        return aim == SimVector2.Zero ? moveDirection : aim;
    }

    private void TrackMouseMovement(float deltaSeconds)
    {
        if (_mouse.Position != _lastMousePosition)
        {
            _lastMousePosition = _mouse.Position;
            _secondsSinceMouseMoved = 0f;
            return;
        }

        if (_secondsSinceMouseMoved < float.MaxValue)
        {
            _secondsSinceMouseMoved += deltaSeconds;
        }
    }

    private bool WasKeyJustPressed(Keys key) =>
        _keyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
}
