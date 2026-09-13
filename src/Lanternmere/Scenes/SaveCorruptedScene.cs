using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Lanternmere.Core;
using Lanternmere.Systems;

namespace Lanternmere.Scenes;

/// <summary>Required error state: a save slot whose primary AND backup copy both failed to deserialize. Tells the player plainly instead of silently discarding progress or crashing.</summary>
public sealed class SaveCorruptedScene : IScene
{
    private readonly InputManager _input;
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;
    private SceneManager _manager = null!;

    public bool DrawsOverPreviousScene => true;

    public SaveCorruptedScene(InputManager input, Texture2D pixel, SpriteFont font)
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
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(0x10, 0x0c, 0x14, 245));
        spriteBatch.DrawString(_font, "Save Unrecoverable", new Vector2(40, 24), new Color(0xd9, 0x6a, 0x3e));
        spriteBatch.DrawString(_font, "This save slot and its backup copy are both damaged and cannot be read.",
            new Vector2(40, 70), Color.White);
        spriteBatch.DrawString(_font, "Choose a different slot, or start a new game.", new Vector2(40, 96), Color.White);
        spriteBatch.DrawString(_font, "Press Enter to continue.", new Vector2(40, viewport.Height - 32), Color.Gray);
        spriteBatch.End();
    }
}
