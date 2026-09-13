using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Lanternmere.Core;
using Lanternmere.Systems;

namespace Lanternmere.Scenes;

/// <summary>Standalone credits viewer reachable from the title menu (not tied to a completed save, unlike the post-ending CreditsScene).</summary>
public sealed class CreditsRollScene : IScene
{
    private readonly InputManager _input;
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;
    private SceneManager _manager = null!;

    public bool DrawsOverPreviousScene => true;

    public CreditsRollScene(InputManager input, Texture2D pixel, SpriteFont font)
    {
        _input = input;
        _pixel = pixel;
        _font = font;
    }

    public void Enter(SceneManager manager) => _manager = manager;
    public void Exit() { }

    public void Update(GameTime gameTime)
    {
        _input.Update();
        if (_input.WasPressedThisFrame(GameAction.Confirm) || _input.WasPressedThisFrame(GameAction.Cancel))
        {
            _manager.Pop();
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var viewport = _manager.Game.GraphicsDevice.Viewport;
        spriteBatch.Begin();
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black);

        var y = 60f;
        foreach (var line in CreditsContent.Lines)
        {
            var size = _font.MeasureString(line);
            spriteBatch.DrawString(_font, line, new Vector2((viewport.Width - size.X) / 2f, y), Color.White);
            y += _font.LineSpacing + 4;
        }

        var prompt = "Press Enter to return.";
        var promptSize = _font.MeasureString(prompt);
        spriteBatch.DrawString(_font, prompt, new Vector2((viewport.Width - promptSize.X) / 2f, viewport.Height - 40), Color.Gray);
        spriteBatch.End();
    }
}
