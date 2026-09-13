using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Lanternmere.Core;
using Lanternmere.Systems;

namespace Lanternmere.Scenes;

/// <summary>
/// Required error state: shown instead of crashing or silently launching a
/// broken world when region/dialogue/item content fails to load or fails
/// validation (missing spawn point, bad transition target, etc.). Lists
/// exactly what's wrong so a developer running a broken content change
/// gets a diagnosis, not a stack trace.
/// </summary>
public sealed class MissingContentScene : IScene
{
    private readonly InputManager _input;
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;
    private readonly string[] _issues;
    private SceneManager _manager = null!;

    public bool DrawsOverPreviousScene => false;

    public MissingContentScene(InputManager input, Texture2D pixel, SpriteFont font, string[] issues)
    {
        _input = input;
        _pixel = pixel;
        _font = font;
        _issues = issues;
    }

    public void Enter(SceneManager manager) => _manager = manager;
    public void Exit() { }

    public void Update(GameTime gameTime)
    {
        _input.Update();
        if (_input.WasPressedThisFrame(GameAction.Cancel))
        {
            _manager.Game.Exit();
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var viewport = _manager.Game.GraphicsDevice.Viewport;
        spriteBatch.Begin();
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(0x1b, 0x10, 0x10));
        spriteBatch.DrawString(_font, "Content failed to load", new Vector2(40, 24), new Color(0xe0, 0x6a, 0x3e));

        var y = 70f;
        foreach (var issue in _issues)
        {
            spriteBatch.DrawString(_font, "- " + issue, new Vector2(40, y), Color.White);
            y += 22;
        }

        spriteBatch.DrawString(_font, "Press Esc to quit.", new Vector2(40, viewport.Height - 32), Color.Gray);
        spriteBatch.End();
    }
}
