using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace Lanternmere.Systems;

/// <summary>Logical actions the game responds to. Rebinding maps one of these to a physical key/button, never the other way around.</summary>
public enum GameAction
{
    MoveUp,
    MoveDown,
    MoveLeft,
    MoveRight,
    Interact,
    OpenJournal,
    OpenMap,
    OpenInventory,
    Pause,
    Confirm,
    Cancel,
    AdvanceText,
    SkipText,
}

/// <summary>
/// Keyboard + controller input with rebinding, per the brief's "Required
/// gameplay systems" and "Accessibility and settings" sections. Bindings
/// are loaded from/saved to <see cref="Core.GameSettings"/> so rebinds
/// persist across sessions. Exposes both "pressed this frame" and "held"
/// so callers can choose hold/toggle behavior per action where relevant.
/// </summary>
public sealed class InputManager
{
    private static readonly Dictionary<GameAction, Keys> DefaultKeyBindings = new()
    {
        [GameAction.MoveUp] = Keys.W,
        [GameAction.MoveDown] = Keys.S,
        [GameAction.MoveLeft] = Keys.A,
        [GameAction.MoveRight] = Keys.D,
        [GameAction.Interact] = Keys.E,
        [GameAction.OpenJournal] = Keys.J,
        [GameAction.OpenMap] = Keys.M,
        [GameAction.OpenInventory] = Keys.I,
        [GameAction.Pause] = Keys.Escape,
        [GameAction.Confirm] = Keys.Enter,
        [GameAction.Cancel] = Keys.Escape,
        [GameAction.AdvanceText] = Keys.Space,
        [GameAction.SkipText] = Keys.LeftControl,
    };

    private static readonly Dictionary<GameAction, Buttons> DefaultGamepadBindings = new()
    {
        [GameAction.MoveUp] = Buttons.LeftThumbstickUp,
        [GameAction.MoveDown] = Buttons.LeftThumbstickDown,
        [GameAction.MoveLeft] = Buttons.LeftThumbstickLeft,
        [GameAction.MoveRight] = Buttons.LeftThumbstickRight,
        [GameAction.Interact] = Buttons.A,
        [GameAction.OpenJournal] = Buttons.Y,
        [GameAction.OpenMap] = Buttons.Back,
        [GameAction.OpenInventory] = Buttons.X,
        [GameAction.Pause] = Buttons.Start,
        [GameAction.Confirm] = Buttons.A,
        [GameAction.Cancel] = Buttons.B,
        [GameAction.AdvanceText] = Buttons.A,
        [GameAction.SkipText] = Buttons.LeftTrigger,
    };

    private readonly Dictionary<GameAction, Keys> _keyBindings;
    private readonly Dictionary<GameAction, Buttons> _gamepadBindings;

    private KeyboardState _prevKeyboard;
    private KeyboardState _currentKeyboard;
    private GamePadState _prevGamePad;
    private GamePadState _currentGamePad;

    public InputManager(Core.GameSettings settings)
    {
        _keyBindings = new Dictionary<GameAction, Keys>(DefaultKeyBindings);
        _gamepadBindings = new Dictionary<GameAction, Buttons>(DefaultGamepadBindings);

        foreach (var (actionName, keyName) in settings.KeyBindings)
        {
            if (System.Enum.TryParse<GameAction>(actionName, out var action) &&
                System.Enum.TryParse<Keys>(keyName, out var key))
            {
                _keyBindings[action] = key;
            }
        }
    }

    public void Rebind(GameAction action, Keys key) => _keyBindings[action] = key;
    public void Rebind(GameAction action, Buttons button) => _gamepadBindings[action] = button;

    public void Update()
    {
        _prevKeyboard = _currentKeyboard;
        _currentKeyboard = Keyboard.GetState();
        _prevGamePad = _currentGamePad;
        _currentGamePad = GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One);
    }

    public bool IsHeld(GameAction action)
    {
        if (_keyBindings.TryGetValue(action, out var key) && _currentKeyboard.IsKeyDown(key)) return true;
        if (_gamepadBindings.TryGetValue(action, out var button) && _currentGamePad.IsButtonDown(button)) return true;
        return false;
    }

    public bool WasPressedThisFrame(GameAction action)
    {
        var keyPressed = _keyBindings.TryGetValue(action, out var key)
            && _currentKeyboard.IsKeyDown(key) && !_prevKeyboard.IsKeyDown(key);
        var buttonPressed = _gamepadBindings.TryGetValue(action, out var button)
            && _currentGamePad.IsButtonDown(button) && !_prevGamePad.IsButtonDown(button);
        return keyPressed || buttonPressed;
    }

    public void SaveBindingsInto(Core.GameSettings settings)
    {
        settings.KeyBindings.Clear();
        foreach (var (action, key) in _keyBindings)
        {
            settings.KeyBindings[action.ToString()] = key.ToString();
        }
    }
}
