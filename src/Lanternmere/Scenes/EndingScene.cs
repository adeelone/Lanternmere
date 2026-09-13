using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Lanternmere.Core;
using Lanternmere.Systems;

namespace Lanternmere.Scenes;

/// <summary>
/// The main ending, with a small optional variation shown when the
/// player also found the archive's full record before relighting the
/// lantern (per docs/NARRATIVE_BIBLE.md's revelation order). Reached only
/// from RegionScene's lantern relight, which has already verified all
/// three name-fragments were recovered and set WorldState.ReachedMainEnding.
/// </summary>
public sealed class EndingScene : IScene
{
    private readonly InputManager _input;
    private readonly GameSettings _settings;
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;
    private readonly AudioManager _audio;
    private readonly WorldState _world;
    private SceneManager _manager = null!;

    public bool DrawsOverPreviousScene => true;

    public EndingScene(InputManager input, GameSettings settings, Texture2D pixel, SpriteFont font, AudioManager audio, WorldState world)
    {
        _input = input;
        _settings = settings;
        _pixel = pixel;
        _font = font;
        _audio = audio;
        _world = world;
    }

    public void Enter(SceneManager manager)
    {
        _manager = manager;
        _audio.PlayMusic("music_ending");
        _audio.PlayAmbience(null);
    }

    public void Exit() { }

    public void Update(GameTime gameTime)
    {
        _input.Update();
        if (_input.WasPressedThisFrame(GameAction.Confirm))
        {
            _manager.Replace(new CreditsScene(_input, _settings, _pixel, _font, _audio, _world));
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var viewport = _manager.Game.GraphicsDevice.Viewport;
        spriteBatch.Begin();
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(0x1b, 0x16, 0x0e));

        var text = _world.ReachedOptionalEndingVariation ? OptionalVariationText : MainEndingText;
        var y = 60f;
        foreach (var line in text)
        {
            var size = _font.MeasureString(line);
            spriteBatch.DrawString(_font, line, new Vector2((viewport.Width - size.X) / 2f, y), new Color(0xf2, 0xd9, 0xb3));
            y += _font.LineSpacing + 8;
        }

        var prompt = "Press Enter to continue.";
        var promptSize = _font.MeasureString(prompt);
        spriteBatch.DrawString(_font, prompt, new Vector2((viewport.Width - promptSize.X) / 2f, viewport.Height - 48), Color.Gray);

        spriteBatch.End();
    }

    private static readonly string[] MainEndingText =
    {
        "The lantern catches, slow and steady, and the light finds the names again:",
        "Amaranth Hollow. The Cliffwatch. Tern's Landing.",
        "",
        "Nothing that was lost comes back all at once. But the map is whole,",
        "and someone is here to read it. That was always enough.",
    };

    private static readonly string[] OptionalVariationText =
    {
        "The lantern catches, slow and steady, and the light finds the names again:",
        "Amaranth Hollow. The Cliffwatch. Tern's Landing.",
        "",
        "And from the archive, one name more: the keeper's own, written last,",
        "explaining at last why they left without saying goodbye.",
        "",
        "Nothing that was lost comes back all at once. But the whole record",
        "is read now, by someone who chose to stay long enough to read it.",
    };
}
