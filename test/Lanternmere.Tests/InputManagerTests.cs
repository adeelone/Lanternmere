using Lanternmere.Core;
using Lanternmere.Systems;
using Microsoft.Xna.Framework.Input;
using Xunit;

namespace Lanternmere.Tests;

/// <summary>
/// InputManager previously had no test coverage at all despite depending
/// only on Lanternmere.Core types. These specifically cover the gamepad
/// rebind persistence path added alongside the code-quality pass (see
/// CODE_QUALITY_AUDIT.md) — GamepadBindings used to be loaded/saved
/// nowhere, so a gamepad rebind silently didn't survive a restart.
/// </summary>
public class InputManagerTests
{
    [Fact]
    public void Rebind_Key_IsReflectedByGetKeyBinding()
    {
        var input = new InputManager(new GameSettings());

        input.Rebind(GameAction.Interact, Keys.F);

        Assert.Equal(Keys.F, input.GetKeyBinding(GameAction.Interact));
    }

    [Fact]
    public void Rebind_GamepadButton_IsReflectedByGetGamepadBinding()
    {
        var input = new InputManager(new GameSettings());

        input.Rebind(GameAction.Interact, Buttons.X);

        Assert.Equal(Buttons.X, input.GetGamepadBinding(GameAction.Interact));
    }

    [Fact]
    public void SaveBindingsInto_ThenReload_PreservesKeyRebind()
    {
        var settings = new GameSettings();
        var input = new InputManager(settings);
        input.Rebind(GameAction.Pause, Keys.P);

        input.SaveBindingsInto(settings);
        var reloaded = new InputManager(settings);

        Assert.Equal(Keys.P, reloaded.GetKeyBinding(GameAction.Pause));
    }

    [Fact]
    public void SaveBindingsInto_ThenReload_PreservesGamepadRebind()
    {
        var settings = new GameSettings();
        var input = new InputManager(settings);
        input.Rebind(GameAction.Pause, Buttons.Back);

        input.SaveBindingsInto(settings);
        var reloaded = new InputManager(settings);

        Assert.Equal(Buttons.Back, reloaded.GetGamepadBinding(GameAction.Pause));
    }

    [Fact]
    public void DefaultBindings_AreNotEmpty()
    {
        var input = new InputManager(new GameSettings());

        Assert.Equal(Keys.W, input.GetKeyBinding(GameAction.MoveUp));
        Assert.Equal(Buttons.A, input.GetGamepadBinding(GameAction.Interact));
    }
}
