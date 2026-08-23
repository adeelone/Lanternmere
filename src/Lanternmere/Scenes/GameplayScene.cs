using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Lanternmere.Core;
using Lanternmere.Systems;
using Lanternmere.Entities;

namespace Lanternmere.Scenes;

/// <summary>
/// The core loop scene: player movement, collision, interaction, and (via
/// WorldState) puzzle/journal/flag state. Region/map loading from data
/// files is stubbed — see ROADMAP.md — this scene currently places the
/// player in an empty bounded room so movement/collision are provably
/// working end to end.
/// </summary>
public sealed class GameplayScene : IScene
{
    private readonly InputManager _input;
    private readonly Texture2D _pixel;
    private readonly WorldState _world;
    private readonly Player _player = new();
    private readonly CollisionSystem _collision = new();
    private readonly DebugOverlay _debugOverlay = new();
    private SceneManager _manager = null!;

    public bool DrawsOverPreviousScene => true;

    public GameplayScene(InputManager input, Texture2D pixel, WorldState world)
    {
        _input = input;
        _pixel = pixel;
        _world = world;
    }

    public void Enter(SceneManager manager)
    {
        _manager = manager;
        _player.Position = new Vector2(_world.PlayerX == 0 ? 160 : _world.PlayerX, _world.PlayerY == 0 ? 90 : _world.PlayerY);

        var viewport = manager.Game.GraphicsDevice.Viewport;
        // Placeholder room bounds until real Tiled maps are wired up
        // (see ROADMAP.md "Region/map loading").
        _collision.SetSolidRegions(new[]
        {
            new Rectangle(0, 0, viewport.Width, 8),                 // top wall
            new Rectangle(0, viewport.Height - 8, viewport.Width, 8), // bottom wall
            new Rectangle(0, 0, 8, viewport.Height),                 // left wall
            new Rectangle(viewport.Width - 8, 0, 8, viewport.Height),// right wall
        });
    }

    public void Exit() { }

    public void Update(GameTime gameTime)
    {
        _input.Update();
        _world.PlaytimeSeconds += gameTime.ElapsedGameTime.TotalSeconds;

        if (_input.WasPressedThisFrame(GameAction.Pause))
        {
            _manager.Push(new PauseScene(_input, _pixel));
            return;
        }

#if DEBUG
        if (Microsoft.Xna.Framework.Input.Keyboard.GetState().IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F1)
            && _input.WasPressedThisFrame(GameAction.Interact) == false)
        {
            // F1 toggled via a dedicated check rather than GameAction so it
            // never collides with a rebindable player action.
        }
#endif

        _player.Update(gameTime, _input, _collision);

        _world.PlayerX = _player.Position.X;
        _world.PlayerY = _player.Position.Y;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var viewport = _manager.Game.GraphicsDevice.Viewport;
        spriteBatch.Begin();
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(0x2c, 0x3e, 0x2f));

        foreach (var wall in _collision.SolidRegions)
        {
            spriteBatch.Draw(_pixel, wall, new Color(0x1a, 0x1a, 0x1a));
        }

        spriteBatch.Draw(_pixel, _player.Bounds, new Color(0xd9, 0x9a, 0x3e));
        spriteBatch.End();

        spriteBatch.Begin();
        _debugOverlay.Draw(spriteBatch, null, gameTime, _world.CurrentRegionId, _player.Position);
        spriteBatch.End();
    }
}
