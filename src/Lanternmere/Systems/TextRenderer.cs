using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lanternmere.Systems;

/// <summary>
/// Shared word-wrapped text drawing, used by every scene that renders a
/// paragraph of prose (dialogue, journal entries, dialogue history).
/// Previously duplicated near-verbatim in DialogueScene and JournalScene —
/// consolidated here so a wrapping fix only needs to happen once.
/// </summary>
public static class TextRenderer
{
    /// <summary>Draws word-wrapped text starting at `position`, wrapping at `maxWidth`. Returns the Y coordinate just below the last line drawn, so callers can stack multiple wrapped blocks.</summary>
    public static float DrawWrapped(SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 position, float maxWidth, Color color)
    {
        var words = text.Split(' ');
        var line = "";
        var y = position.Y;
        foreach (var word in words)
        {
            var candidate = line.Length == 0 ? word : line + " " + word;
            if (font.MeasureString(candidate).X > maxWidth && line.Length > 0)
            {
                spriteBatch.DrawString(font, line, new Vector2(position.X, y), color);
                y += font.LineSpacing + 2;
                line = word;
            }
            else
            {
                line = candidate;
            }
        }
        if (line.Length > 0)
        {
            spriteBatch.DrawString(font, line, new Vector2(position.X, y), color);
            y += font.LineSpacing + 2;
        }
        return y;
    }
}
