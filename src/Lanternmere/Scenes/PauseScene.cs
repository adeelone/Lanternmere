using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Lanternmere.Core;
using Lanternmere.Systems;

namespace Lanternmere.Scenes;

/// <summary>
/// Pause overlay. Draws over the frozen gameplay scene beneath it
/// (DrawsOverPreviousScene = false stops draw propagation further down, so
/// only Gameplay + Pause are ever drawn together — never Title bleeding
/// through). Gameplay's Update is naturally suspended while Pause is on
/// top, since SceneManager only updates the topmost scene.
/// </summary>
public sealed class PauseScene : IScene
{
    private readonly InputManager _input;
    private readonly Texture2D _pixel;
    private SceneManager _manager = null!;

    public bool DrawsOverPreviousScene => false;

    public PauseScene(InputManager input, Texture2D pixel)
    {
        _input = input;
        _pixel = pixel;
    }

    public void Enter(SceneManager manager) => _manager = manager;
    public void Exit() { }

    public void Update(GameTime gameTime)
    {
        _input.Update();
        if (_input.WasPressedThisFrame(GameAction.Pause) || _input.WasPressedThisFrame(GameAction.Cancel))
        {
            _manager.Pop();
        }
        // TODO: settings navigation, "quit to title" confirmation dialog.
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var viewport = _manager.Game.GraphicsDevice.Viewport;
        spriteBatch.Begin();
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(0, 0, 0, 140));
        spriteBatch.End();
    }
}
