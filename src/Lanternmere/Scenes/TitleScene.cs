using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Lanternmere.Core;
using Lanternmere.Systems;

namespace Lanternmere.Scenes;

/// <summary>
/// Title screen: New / Continue / Load, settings, credits entry points.
/// This scaffold wires New Game (Enter/Confirm) and Quit (Escape/Cancel);
/// Continue/Load slot picker and Settings/Credits screens are listed as
/// not-yet-built in ROADMAP.md.
/// </summary>
public sealed class TitleScene : IScene
{
    private readonly InputManager _input;
    private readonly Texture2D _pixel;
    private SceneManager _manager = null!;

    public bool DrawsOverPreviousScene => true;

    public TitleScene(InputManager input, Texture2D pixel)
    {
        _input = input;
        _pixel = pixel;
    }

    public void Enter(SceneManager manager)
    {
        _manager = manager;
    }

    public void Exit() { }

    public void Update(GameTime gameTime)
    {
        _input.Update();

        if (_input.WasPressedThisFrame(GameAction.Confirm))
        {
            var world = new WorldState();
            _manager.Replace(new GameplayScene(_input, _pixel, world));
        }
        else if (_input.WasPressedThisFrame(GameAction.Cancel))
        {
            _manager.Game.Exit();
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var viewport = _manager.Game.GraphicsDevice.Viewport;
        spriteBatch.Begin();
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(0x1b, 0x16, 0x22));
        // A real title screen draws logo/menu text via SpriteFont here —
        // left as a visual-direction task; see ROADMAP.md.
        spriteBatch.End();
    }
}
