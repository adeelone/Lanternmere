using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lanternmere.Core;

/// <summary>
/// Owns the scene stack. Only the top scene updates; scenes marked
/// <see cref="IScene.DrawsOverPreviousScene"/> = false cause everything
/// below them to be skipped on Draw too (used for hard scene changes like
/// Title -> Gameplay). Transition scenes can wrap another scene to fade
/// between them without the stack itself needing to know about fades.
/// </summary>
public sealed class SceneManager
{
    private readonly Stack<IScene> _stack = new();
    public Game Game { get; }

    public SceneManager(Game game)
    {
        Game = game;
    }

    public IScene? Current => _stack.Count > 0 ? _stack.Peek() : null;

    public void Push(IScene scene)
    {
        scene.Enter(this);
        _stack.Push(scene);
    }

    public void Pop()
    {
        if (_stack.Count == 0) return;
        var top = _stack.Pop();
        top.Exit();
    }

    /// <summary>Pop the current scene and push a new one in a single step (a "hard cut").</summary>
    public void Replace(IScene scene)
    {
        Pop();
        Push(scene);
    }

    /// <summary>Clears the entire stack and pushes a single new scene — used for "quit to title" from deep inside gameplay/pause/dialogue.</summary>
    public void ReplaceAll(IScene scene)
    {
        while (_stack.Count > 0) Pop();
        Push(scene);
    }

    public void Update(GameTime gameTime)
    {
        Current?.Update(gameTime);
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        // Walk from the bottom of the stack up, but only start drawing from
        // the deepest scene that still wants to be visible underneath the
        // scenes above it.
        var scenesToDraw = new List<IScene>();
        foreach (var scene in _stack)
        {
            scenesToDraw.Add(scene);
            if (!scene.DrawsOverPreviousScene) break;
        }
        scenesToDraw.Reverse();
        foreach (var scene in scenesToDraw)
        {
            scene.Draw(gameTime, spriteBatch);
        }
    }
}
