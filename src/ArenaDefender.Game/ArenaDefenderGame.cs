using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ArenaDefender;

/// <summary>MonoGame host. Window, graphics device and frame loop live here; game rules do not.</summary>
public class ArenaDefenderGame : Game
{
    private readonly GraphicsDeviceManager _graphics;

    private SpriteBatch _spriteBatch = null!;

    public ArenaDefenderGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = string.Empty;
        IsMouseVisible = true;
        Window.Title = "Arena Defender";
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        base.Draw(gameTime);
    }
}
