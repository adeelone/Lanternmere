using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Lanternmere.Core;
using Lanternmere.Core.Content;
using Lanternmere.Systems;

namespace Lanternmere.Scenes;

/// <summary>
/// Credits, then the required post-game "completion state": the player
/// can keep exploring the completed save (post-game continue) or return
/// to the title screen. Reaching here marks WorldState.ReachedMainEnding,
/// already set by RegionScene before this scene was pushed.
/// </summary>
public sealed class CreditsScene : IScene
{
    private static readonly string[] Options = { "Continue exploring", "Return to Title" };

    private readonly InputManager _input;
    private readonly GameSettings _settings;
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;
    private readonly AudioManager _audio;
    private readonly WorldState _world;
    private SceneManager _manager = null!;
    private int _selected;

    public bool DrawsOverPreviousScene => true;

    public CreditsScene(InputManager input, GameSettings settings, Texture2D pixel, SpriteFont font, AudioManager audio, WorldState world)
    {
        _input = input;
        _settings = settings;
        _pixel = pixel;
        _font = font;
        _audio = audio;
        _world = world;
    }

    public void Enter(SceneManager manager) => _manager = manager;
    public void Exit() { }

    public void Update(GameTime gameTime)
    {
        _input.Update();
        if (_input.WasPressedThisFrame(GameAction.MoveUp) || _input.WasPressedThisFrame(GameAction.MoveDown))
        {
            _selected = 1 - _selected;
            _audio.PlaySfx("sfx_menu_move");
        }
        else if (_input.WasPressedThisFrame(GameAction.Confirm))
        {
            _audio.PlaySfx("sfx_menu_select");
            if (_selected == 0)
            {
                _manager.ReplaceAll(BuildPostGameRegionScene());
            }
            else
            {
                _manager.ReplaceAll(new TitleScene(_input, _settings, _pixel, _font, _audio));
            }
        }
    }

    private IScene BuildPostGameRegionScene()
    {
        // The caller (Game1) owns GameContent; RegionScene needs it too, so
        // route back through a fresh title->new-game-equivalent construction
        // isn't right here. CreditsScene is only ever reached via RegionScene,
        // so we ask Game1 for a scene builder instead of duplicating content
        // loading — see Game1.BuildPostGameScene.
        return Game1.Instance!.BuildRegionScene(_world.CurrentRegionId, "default", _world);
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var viewport = _manager.Game.GraphicsDevice.Viewport;
        spriteBatch.Begin();
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black);

        var y = 40f;
        foreach (var line in Core.CreditsContent.Lines)
        {
            var size = _font.MeasureString(line);
            spriteBatch.DrawString(_font, line, new Vector2((viewport.Width - size.X) / 2f, y), Color.White);
            y += _font.LineSpacing + 4;
        }

        y += 20;
        for (var i = 0; i < Options.Length; i++)
        {
            var color = i == _selected ? new Color(0xd9, 0x9a, 0x3e) : Color.LightGray;
            var text = (i == _selected ? "> " : "  ") + Options[i];
            var size = _font.MeasureString(text);
            spriteBatch.DrawString(_font, text, new Vector2((viewport.Width - size.X) / 2f, y), color);
            y += _font.LineSpacing + 6;
        }

        spriteBatch.End();
    }
}
