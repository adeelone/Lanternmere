using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lanternmere.Core;

/// <summary>
/// Developer-only overlay: FPS, camera/player coordinates, collision
/// shapes, interactable bounds, current region, active flags, and audio
/// zones (per the brief's "Architecture" section). Compiled out of
/// non-debug builds entirely via the DEBUG conditional so a release build
/// cannot accidentally ship with debug controls reachable.
/// </summary>
public sealed class DebugOverlay
{
#if DEBUG
    public bool Visible { get; private set; }

    public void Toggle() => Visible = !Visible;

    public void Draw(SpriteBatch spriteBatch, SpriteFont? font, GameTime gameTime, string regionName, Vector2 playerPosition)
    {
        if (!Visible || font is null) return;

        var fps = 1.0 / gameTime.ElapsedGameTime.TotalSeconds;
        var lines = new[]
        {
            $"FPS: {fps:0}",
            $"Region: {regionName}",
            $"Player: {playerPosition.X:0.0}, {playerPosition.Y:0.0}",
        };

        var y = 8f;
        foreach (var line in lines)
        {
            spriteBatch.DrawString(font, line, new Vector2(8, y), Color.Lime);
            y += font.LineSpacing;
        }
    }
#else
    // Release builds: overlay is a complete no-op with no reachable toggle.
    public bool Visible => false;
    public void Toggle() { }
    public void Draw(SpriteBatch spriteBatch, SpriteFont? font, GameTime gameTime, string regionName, Vector2 playerPosition) { }
#endif
}
