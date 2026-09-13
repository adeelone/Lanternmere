using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Lanternmere.Core;
using Lanternmere.Systems;

namespace Lanternmere.Scenes;

/// <summary>Title screen: New Game, Continue (most recent save), Load, Settings, Credits, Quit.</summary>
public sealed class TitleScene : IScene
{
    private readonly InputManager _input;
    private readonly GameSettings _settings;
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;
    private readonly AudioManager _audio;
    private SceneManager _manager = null!;
    private int _selected;
    private List<string> _options = new();

    public bool DrawsOverPreviousScene => true;

    public TitleScene(InputManager input, GameSettings settings, Texture2D pixel, SpriteFont font, AudioManager audio)
    {
        _input = input;
        _settings = settings;
        _pixel = pixel;
        _font = font;
        _audio = audio;
    }

    public void Enter(SceneManager manager)
    {
        _manager = manager;
        _audio.PlayMusic("music_theme");
        _audio.PlayAmbience("ambience_village_hub");
        RebuildOptions();
    }

    public void Exit() { }

    private void RebuildOptions()
    {
        _options = new List<string> { "New Game" };
        if (AnySaveExists()) _options.Add("Continue");
        _options.Add("Load");
        _options.Add("Settings");
        _options.Add("Credits");
        _options.Add("Quit");
        _selected = Math.Min(_selected, _options.Count - 1);
    }

    private static bool AnySaveExists()
    {
        for (var slot = 0; slot < SaveSystem.SlotCount; slot++)
        {
            if (SaveSystem.SlotExists(slot)) return true;
        }
        return false;
    }

    public void Update(GameTime gameTime)
    {
        _input.Update();

        if (_input.WasPressedThisFrame(GameAction.MoveUp))
        {
            _selected = (_selected - 1 + _options.Count) % _options.Count;
            _audio.PlaySfx("sfx_menu_move");
        }
        else if (_input.WasPressedThisFrame(GameAction.MoveDown))
        {
            _selected = (_selected + 1) % _options.Count;
            _audio.PlaySfx("sfx_menu_move");
        }
        else if (_input.WasPressedThisFrame(GameAction.Confirm))
        {
            _audio.PlaySfx("sfx_menu_select");
            HandleSelect(_options[_selected]);
        }
        else if (_input.WasPressedThisFrame(GameAction.Cancel))
        {
            _manager.Game.Exit();
        }
    }

    private void HandleSelect(string option)
    {
        var content = Game1.Instance!.Content2;
        switch (option)
        {
            case "New Game":
                var world = new WorldState();
                world.SetFlag("game_started");
                _manager.ReplaceAll(new RegionScene(_input, _settings, content, world, _audio, _font, _pixel, "village_hub", "default"));
                break;
            case "Continue":
                LoadMostRecent();
                break;
            case "Load":
                _manager.Push(new SaveSlotScene(_input, _settings, _audio, _pixel, _font, content, SaveSlotMode.Load));
                break;
            case "Settings":
                _manager.Push(new SettingsScene(_input, _settings, _audio, _pixel, _font));
                break;
            case "Credits":
                _manager.Push(new CreditsRollScene(_input, _pixel, _font));
                break;
            case "Quit":
                _manager.Game.Exit();
                break;
        }
    }

    private void LoadMostRecent()
    {
        var content = Game1.Instance!.Content2;
        var best = -1;
        DateTime bestTime = DateTime.MinValue;
        for (var slot = 0; slot < SaveSystem.SlotCount; slot++)
        {
            var (result, savedAt, _) = SaveSystem.LoadWithMetadata(slot);
            if (result is LoadResult.Success or LoadResult.RecoveredFromBackup && savedAt > bestTime)
            {
                best = slot;
                bestTime = savedAt ?? DateTime.MinValue;
            }
        }
        if (best < 0) return;

        var (loadResult, _, world) = SaveSystem.LoadWithMetadata(best);
        if (loadResult is LoadResult.Success or LoadResult.RecoveredFromBackup)
        {
            _manager.ReplaceAll(new RegionScene(_input, _settings, content, world!, _audio, _font, _pixel, world!.CurrentRegionId, "default"));
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var viewport = _manager.Game.GraphicsDevice.Viewport;
        spriteBatch.Begin();
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(0x1b, 0x16, 0x22));

        var title = "LANTERNMERE";
        var titleFont = Game1.Instance!.LargeFont;
        var titleSize = titleFont.MeasureString(title);
        spriteBatch.DrawString(titleFont, title, new Vector2((viewport.Width - titleSize.X) / 2f, viewport.Height * 0.25f), new Color(0xd9, 0x9a, 0x3e));

        var subtitle = "a quiet exploration";
        var subtitleSize = _font.MeasureString(subtitle);
        spriteBatch.DrawString(_font, subtitle, new Vector2((viewport.Width - subtitleSize.X) / 2f, viewport.Height * 0.25f + titleSize.Y + 4), Color.LightGray);

        var y = viewport.Height * 0.5f;
        foreach (var option in _options)
        {
            var index = _options.IndexOf(option);
            var color = index == _selected ? new Color(0xd9, 0x9a, 0x3e) : Color.LightGray;
            var text = (index == _selected ? "> " : "  ") + option;
            var size = _font.MeasureString(text);
            spriteBatch.DrawString(_font, text, new Vector2((viewport.Width - size.X) / 2f, y), color);
            y += _font.LineSpacing + 8;
        }

        spriteBatch.End();
    }
}
