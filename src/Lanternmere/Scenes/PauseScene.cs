using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Lanternmere.Core;
using Lanternmere.Systems;

namespace Lanternmere.Scenes;

/// <summary>
/// Pause overlay: Resume, Save, Settings, Quit to Title (with a confirm
/// step since it discards anything since the last autosave). Draws over a
/// still-visible, frozen RegionScene beneath it.
/// </summary>
public sealed class PauseScene : IScene
{
    private static readonly string[] Options = { "Resume", "Save Game", "Settings", "Quit to Title" };

    private readonly InputManager _input;
    private readonly GameSettings _settings;
    private readonly AudioManager _audio;
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;
    private readonly WorldState _world;
    private SceneManager _manager = null!;

    private int _selected;
    private bool _confirmingQuit;
    private string? _statusMessage;
    private float _statusSeconds;

    public bool DrawsOverPreviousScene => true;

    public PauseScene(InputManager input, GameSettings settings, AudioManager audio, Texture2D pixel, SpriteFont font, WorldState world)
    {
        _input = input;
        _settings = settings;
        _audio = audio;
        _pixel = pixel;
        _font = font;
        _world = world;
    }

    public void Enter(SceneManager manager) => _manager = manager;
    public void Exit() { }

    public void Update(GameTime gameTime)
    {
        _input.Update();
        if (_statusSeconds > 0f) _statusSeconds -= (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_input.WasPressedThisFrame(GameAction.Cancel))
        {
            if (_confirmingQuit) _confirmingQuit = false;
            else _manager.Pop();
            return;
        }

        if (_input.WasPressedThisFrame(GameAction.MoveUp))
        {
            _selected = (_selected - 1 + Options.Length) % Options.Length;
            _confirmingQuit = false;
            _audio.PlaySfx("sfx_menu_move");
        }
        else if (_input.WasPressedThisFrame(GameAction.MoveDown))
        {
            _selected = (_selected + 1) % Options.Length;
            _confirmingQuit = false;
            _audio.PlaySfx("sfx_menu_move");
        }
        else if (_input.WasPressedThisFrame(GameAction.Confirm) || _input.WasPressedThisFrame(GameAction.Pause))
        {
            if (_input.WasPressedThisFrame(GameAction.Pause)) { _manager.Pop(); return; }
            _audio.PlaySfx("sfx_menu_select");
            HandleSelect();
        }
    }

    private void HandleSelect()
    {
        switch (Options[_selected])
        {
            case "Resume":
                _manager.Pop();
                break;
            case "Save Game":
                SaveSystem.Save(1, _world);
                _statusMessage = "Saved.";
                _statusSeconds = 1.5f;
                break;
            case "Settings":
                _manager.Push(new SettingsScene(_input, _settings, _audio, _pixel, _font));
                break;
            case "Quit to Title":
                if (_confirmingQuit)
                {
                    _manager.ReplaceAll(new TitleScene(_input, _settings, _pixel, _font, _audio));
                }
                else
                {
                    _confirmingQuit = true;
                }
                break;
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var viewport = _manager.Game.GraphicsDevice.Viewport;
        spriteBatch.Begin();
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(0, 0, 0, 150));

        var panelWidth = 280;
        var panelHeight = 40 + Options.Length * 30 + 20;
        var panel = new Rectangle((viewport.Width - panelWidth) / 2, (viewport.Height - panelHeight) / 2, panelWidth, panelHeight);
        spriteBatch.Draw(_pixel, panel, new Color(0x14, 0x10, 0x18, 235));
        spriteBatch.Draw(_pixel, new Rectangle(panel.X, panel.Y, panel.Width, 2), new Color(0xd9, 0x9a, 0x3e));

        spriteBatch.DrawString(_font, "Paused", new Vector2(panel.X + 16, panel.Y + 12), Color.White);

        var y = panel.Y + 44;
        for (var i = 0; i < Options.Length; i++)
        {
            var color = i == _selected ? new Color(0xd9, 0x9a, 0x3e) : Color.LightGray;
            var prefix = i == _selected ? "> " : "  ";
            var label = Options[i];
            if (label == "Quit to Title" && _confirmingQuit) label = "Quit to Title (press again to confirm)";
            spriteBatch.DrawString(_font, prefix + label, new Vector2(panel.X + 16, y), color);
            y += 30;
        }

        if (_statusMessage is not null && _statusSeconds > 0f)
        {
            spriteBatch.DrawString(_font, _statusMessage, new Vector2(panel.X + 16, panel.Bottom - 24), Color.LightGreen);
        }

        spriteBatch.End();
    }
}
