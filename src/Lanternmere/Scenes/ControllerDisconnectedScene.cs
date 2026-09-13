using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Lanternmere.Core;
using Lanternmere.Systems;

namespace Lanternmere.Scenes;

/// <summary>
/// Required error state: shown when the game was being played with a
/// gamepad and it disconnects mid-session. Pauses gameplay in place
/// (drawn over the frozen scene beneath) rather than letting movement
/// silently stop responding with no explanation.
/// </summary>
public sealed class ControllerDisconnectedScene : IScene
{
    private readonly InputManager _input;
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;
    private SceneManager _manager = null!;

    public bool DrawsOverPreviousScene => true;

    public ControllerDisconnectedScene(InputManager input, Texture2D pixel, SpriteFont font)
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
        if (GamePad.GetState(PlayerIndex.One).IsConnected || Keyboard.GetState().GetPressedKeyCount() > 0)
        {
            _manager.Pop();
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var viewport = _manager.Game.GraphicsDevice.Viewport;
        spriteBatch.Begin();
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(0, 0, 0, 200));
        var text = "Controller disconnected.\nReconnect it, or press any keyboard key to continue.";
        spriteBatch.DrawString(_font, text, new Vector2(viewport.Width / 2f - 220, viewport.Height / 2f - 20), Color.White);
        spriteBatch.End();
    }
}
