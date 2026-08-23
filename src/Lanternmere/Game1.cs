using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Lanternmere.Core;
using Lanternmere.Systems;
using Lanternmere.Scenes;

namespace Lanternmere;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;

    private GameSettings _settings = null!;
    private InputManager _input = null!;
    private SceneManager _sceneManager = null!;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        // Fixed timestep drives deterministic puzzle/physics state (see
        // brief: "Use a fixed-timestep update and deterministic state
        // changes where practical"). This is the MonoGame default, made
        // explicit here so it isn't accidentally disabled later.
        IsFixedTimeStep = true;
        TargetElapsedTime = System.TimeSpan.FromSeconds(1.0 / 60.0);
    }

    protected override void Initialize()
    {
        _settings = GameSettings.LoadOrDefault();

        _graphics.PreferredBackBufferWidth = _settings.ResolutionWidth;
        _graphics.PreferredBackBufferHeight = _settings.ResolutionHeight;
        _graphics.IsFullScreen = _settings.Fullscreen;
        _graphics.ApplyChanges();

        _input = new InputManager(_settings);
        _sceneManager = new SceneManager(this);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _sceneManager.Push(new TitleScene(_input, _pixel));
    }

    protected override void Update(GameTime gameTime)
    {
        _sceneManager.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _sceneManager.Draw(gameTime, _spriteBatch);
        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _input.SaveBindingsInto(_settings);
        _settings.Save();
        base.UnloadContent();
    }
}
