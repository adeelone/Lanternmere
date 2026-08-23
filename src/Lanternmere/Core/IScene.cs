using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lanternmere.Core;

/// <summary>
/// One entry in the scene/state stack (title, gameplay, pause, dialogue,
/// transitions, credits — per the brief's "Architecture" section). Scenes
/// are pushed/popped rather than swapped outright so e.g. Pause can overlay
/// Gameplay without discarding gameplay state.
/// </summary>
public interface IScene
{
    /// <summary>Called once when the scene becomes the top of the stack for the first time.</summary>
    void Enter(SceneManager manager);

    /// <summary>Called when the scene is popped off the stack.</summary>
    void Exit();

    /// <summary>
    /// Fixed-timestep update. `gameTime.ElapsedGameTime` is constant when
    /// <see cref="Game.IsFixedTimeStep"/> is true, which Lanternmere relies
    /// on for deterministic puzzle/physics state (see brief: "Use a fixed-
    /// timestep update and deterministic state changes where practical").
    /// </summary>
    void Update(GameTime gameTime);

    void Draw(GameTime gameTime, SpriteBatch spriteBatch);

    /// <summary>
    /// If true, the scene below this one in the stack still receives Draw
    /// calls (e.g. Pause drawn over a frozen Gameplay). Update calls to the
    /// scene below are always suspended while a blocking scene is on top —
    /// only the topmost scene ever updates.
    /// </summary>
    bool DrawsOverPreviousScene { get; }
}
