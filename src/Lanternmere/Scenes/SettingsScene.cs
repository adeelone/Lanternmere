using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Lanternmere.Core;
using Lanternmere.Systems;

namespace Lanternmere.Scenes;

/// <summary>
/// Every accessibility/settings control the brief requires in one screen:
/// independent volume sliders, reduced motion/shake/flash, high-contrast
/// indicators, text speed + instant text, fullscreen/resolution, UI scale,
/// and keyboard/gamepad rebinding. Reachable from both Title and Pause.
/// </summary>
public sealed class SettingsScene : IScene
{
    private enum RowKind { Slider, Toggle, Action }

    private sealed class Row
    {
        public string Label = "";
        public RowKind Kind;
        public System.Func<GameSettings, float>? Get;
        public System.Action<GameSettings, float>? Adjust;
        public System.Func<GameSettings, bool>? GetBool;
        public System.Action<GameSettings>? Toggle;
        public System.Action? Activate;
    }

    private readonly InputManager _input;
    private readonly GameSettings _settings;
    private readonly AudioManager _audio;
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;
    private SceneManager _manager = null!;

    private readonly List<Row> _rows;
    private int _selected;
    private bool _rebinding;
    private GameAction _rebindingAction;
    private KeyboardState _rebindStartKeyboard;
    private GamePadState _rebindStartGamePad;
    private bool _inRebindMenu;
    private int _rebindSelected;
    private static readonly GameAction[] RebindableActions =
    {
        GameAction.MoveUp, GameAction.MoveDown, GameAction.MoveLeft, GameAction.MoveRight,
        GameAction.Interact, GameAction.OpenJournal, GameAction.OpenMap, GameAction.OpenInventory, GameAction.Pause,
    };

    /// <summary>Buttons offered for gamepad rebinding — face buttons, shoulders/triggers, d-pad, and start/back. Thumbstick directions are deliberately excluded since they're bound as continuous movement axes, not discrete presses.</summary>
    private static readonly Buttons[] RebindableGamepadButtons =
    {
        Buttons.A, Buttons.B, Buttons.X, Buttons.Y,
        Buttons.LeftShoulder, Buttons.RightShoulder, Buttons.LeftTrigger, Buttons.RightTrigger,
        Buttons.DPadUp, Buttons.DPadDown, Buttons.DPadLeft, Buttons.DPadRight,
        Buttons.Start, Buttons.Back,
    };

    public bool DrawsOverPreviousScene => true;

    public SettingsScene(InputManager input, GameSettings settings, AudioManager audio, Texture2D pixel, SpriteFont font)
    {
        _input = input;
        _settings = settings;
        _audio = audio;
        _pixel = pixel;
        _font = font;

        _rows = new List<Row>
        {
            new() { Label = "Master Volume", Kind = RowKind.Slider, Get = s => s.MasterVolume, Adjust = (s, d) => s.MasterVolume = Clamp01(s.MasterVolume + d) },
            new() { Label = "Music Volume", Kind = RowKind.Slider, Get = s => s.MusicVolume, Adjust = (s, d) => s.MusicVolume = Clamp01(s.MusicVolume + d) },
            new() { Label = "Ambience Volume", Kind = RowKind.Slider, Get = s => s.AmbienceVolume, Adjust = (s, d) => s.AmbienceVolume = Clamp01(s.AmbienceVolume + d) },
            new() { Label = "Effects Volume", Kind = RowKind.Slider, Get = s => s.EffectsVolume, Adjust = (s, d) => s.EffectsVolume = Clamp01(s.EffectsVolume + d) },
            new() { Label = "Text Speed", Kind = RowKind.Slider, Get = s => s.TextSpeed / 2f, Adjust = (s, d) => s.TextSpeed = System.Math.Clamp(s.TextSpeed + d * 2f, 0.25f, 2f) },
            new() { Label = "Instant Text", Kind = RowKind.Toggle, GetBool = s => s.InstantText, Toggle = s => s.InstantText = !s.InstantText },
            new() { Label = "Reduced Motion", Kind = RowKind.Toggle, GetBool = s => s.ReducedMotion, Toggle = s => s.ReducedMotion = !s.ReducedMotion },
            new() { Label = "Reduced Shake", Kind = RowKind.Toggle, GetBool = s => s.ReducedShake, Toggle = s => s.ReducedShake = !s.ReducedShake },
            new() { Label = "Reduced Flash", Kind = RowKind.Toggle, GetBool = s => s.ReducedFlash, Toggle = s => s.ReducedFlash = !s.ReducedFlash },
            new() { Label = "High-Contrast Indicators", Kind = RowKind.Toggle, GetBool = s => s.HighContrastInteractionIndicators, Toggle = s => s.HighContrastInteractionIndicators = !s.HighContrastInteractionIndicators },
            new() { Label = "Fullscreen", Kind = RowKind.Toggle, GetBool = s => s.Fullscreen, Toggle = s => s.Fullscreen = !s.Fullscreen },
            new() { Label = "UI Scale", Kind = RowKind.Slider, Get = s => (s.UiScale - 0.75f) / 0.75f, Adjust = (s, d) => s.UiScale = System.Math.Clamp(s.UiScale + d * 0.75f, 0.75f, 1.5f) },
            new() { Label = "Rebind Controls...", Kind = RowKind.Action, Activate = () => _inRebindMenu = true },
            new() { Label = "Back", Kind = RowKind.Action, Activate = () => _manager.Pop() },
        };
    }

    private static float Clamp01(float v) => System.Math.Clamp(v, 0f, 1f);

    public void Enter(SceneManager manager) => _manager = manager;

    public void Exit()
    {
        _input.SaveBindingsInto(_settings);
        _settings.Save();
    }

    public void Update(GameTime gameTime)
    {
        _input.Update();

        if (_rebinding)
        {
            // Only accept a key/button that was NOT already down when rebind
            // mode started — without this, the Enter/A press used to *enter*
            // rebind mode is often still physically held on the very next
            // frame (human key presses routinely span several 60fps frames),
            // which would otherwise immediately and incorrectly rebind the
            // action to whatever triggered the menu itself.
            var keyboard = Keyboard.GetState();
            foreach (var key in keyboard.GetPressedKeys())
            {
                if (_rebindStartKeyboard.IsKeyDown(key)) continue;
                _input.Rebind(_rebindingAction, key);
                _rebinding = false;
                _audio.PlaySfx("sfx_menu_select");
                return;
            }

            var gamepad = GamePad.GetState(PlayerIndex.One);
            foreach (var button in RebindableGamepadButtons)
            {
                if (gamepad.IsButtonDown(button) && !_rebindStartGamePad.IsButtonDown(button))
                {
                    _input.Rebind(_rebindingAction, button);
                    _rebinding = false;
                    _audio.PlaySfx("sfx_menu_select");
                    return;
                }
            }
            return;
        }

        if (_inRebindMenu)
        {
            UpdateRebindMenu();
            return;
        }

        if (_input.WasPressedThisFrame(GameAction.Cancel))
        {
            _manager.Pop();
            return;
        }

        if (_input.WasPressedThisFrame(GameAction.MoveUp))
        {
            _selected = (_selected - 1 + _rows.Count) % _rows.Count;
            _audio.PlaySfx("sfx_menu_move");
        }
        else if (_input.WasPressedThisFrame(GameAction.MoveDown))
        {
            _selected = (_selected + 1) % _rows.Count;
            _audio.PlaySfx("sfx_menu_move");
        }

        var row = _rows[_selected];
        if (row.Kind == RowKind.Slider)
        {
            if (_input.WasPressedThisFrame(GameAction.MoveLeft)) row.Adjust!(_settings, -0.1f);
            else if (_input.WasPressedThisFrame(GameAction.MoveRight)) row.Adjust!(_settings, 0.1f);
        }
        else if (_input.WasPressedThisFrame(GameAction.Confirm) || _input.WasPressedThisFrame(GameAction.MoveLeft) || _input.WasPressedThisFrame(GameAction.MoveRight))
        {
            if (row.Kind == RowKind.Toggle) row.Toggle!(_settings);
            else if (row.Kind == RowKind.Action && _input.WasPressedThisFrame(GameAction.Confirm)) row.Activate!();
        }
    }

    private void UpdateRebindMenu()
    {
        if (_input.WasPressedThisFrame(GameAction.Cancel))
        {
            _inRebindMenu = false;
            return;
        }
        if (_input.WasPressedThisFrame(GameAction.MoveUp))
        {
            _rebindSelected = (_rebindSelected - 1 + RebindableActions.Length) % RebindableActions.Length;
        }
        else if (_input.WasPressedThisFrame(GameAction.MoveDown))
        {
            _rebindSelected = (_rebindSelected + 1) % RebindableActions.Length;
        }
        else if (_input.WasPressedThisFrame(GameAction.Confirm))
        {
            _rebinding = true;
            _rebindingAction = RebindableActions[_rebindSelected];
            _rebindStartKeyboard = Keyboard.GetState();
            _rebindStartGamePad = GamePad.GetState(PlayerIndex.One);
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var viewport = _manager.Game.GraphicsDevice.Viewport;
        spriteBatch.Begin();
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(0x10, 0x0c, 0x14, 245));
        spriteBatch.DrawString(_font, "Settings", new Vector2(40, 24), new Color(0xd9, 0x9a, 0x3e));

        if (_inRebindMenu)
        {
            DrawRebindMenu(spriteBatch);
            spriteBatch.End();
            return;
        }

        var y = 70f;
        foreach (var row in _rows)
        {
            var index = _rows.IndexOf(row);
            var color = index == _selected ? new Color(0xd9, 0x9a, 0x3e) : Color.LightGray;
            var prefix = index == _selected ? "> " : "  ";
            var valueText = row.Kind switch
            {
                RowKind.Slider => BarText(row.Get!(_settings)),
                RowKind.Toggle => row.GetBool!(_settings) ? "On" : "Off",
                _ => "",
            };
            spriteBatch.DrawString(_font, $"{prefix}{row.Label,-26} {valueText}", new Vector2(40, y), color);
            y += 26;
        }

        spriteBatch.DrawString(_font, "Arrows: navigate/adjust   Enter: select   Esc: back", new Vector2(40, viewport.Height - 32), Color.Gray);
        spriteBatch.End();
    }

    private void DrawRebindMenu(SpriteBatch spriteBatch)
    {
        var y = 70f;
        for (var i = 0; i < RebindableActions.Length; i++)
        {
            var color = i == _rebindSelected ? new Color(0xd9, 0x9a, 0x3e) : Color.LightGray;
            var prefix = i == _rebindSelected ? "> " : "  ";
            var label = RebindableActions[i].ToString();
            var action = RebindableActions[i];
            var current = $"Key: {_input.GetKeyBinding(action),-10} Gamepad: {_input.GetGamepadBinding(action)}";
            var display = _rebinding && i == _rebindSelected ? "Press a key or gamepad button..." : current;
            spriteBatch.DrawString(_font, $"{prefix}{label,-16} {display}", new Vector2(40, y), color);
            y += 26;
        }
        spriteBatch.DrawString(_font, "Enter: rebind   Esc: back", new Vector2(40, y + 20), Color.Gray);
    }

    private static string BarText(float value01)
    {
        var filled = System.Math.Clamp((int)System.MathF.Round(value01 * 10), 0, 10);
        return "[" + new string('#', filled) + new string('-', 10 - filled) + "]";
    }
}
