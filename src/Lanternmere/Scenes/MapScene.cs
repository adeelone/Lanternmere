using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Lanternmere.Core;
using Lanternmere.Systems;

namespace Lanternmere.Scenes;

/// <summary>
/// The fog-of-discovery map: a schematic layout of the valley's regions
/// around the central lantern, per the brief ("Fog-of-discovery map, not a
/// GPS-style objective arrow"). A region only shows its name and revealed
/// landmarks once the player has actually visited it; unvisited regions
/// are drawn as a dim, unlabeled node so the player knows more exists
/// without being told exactly what or where.
/// </summary>
public sealed class MapScene : IScene
{
    private sealed record NodeLayout(string RegionId, string ShortLabel, Vector2 RelativePosition);

    private static readonly NodeLayout[] Layout =
    {
        new("village_hub", "Village Hub", new Vector2(0.5f, 0.5f)),
        new("rain_garden", "Rain Garden", new Vector2(0.25f, 0.3f)),
        new("wind_cliffs", "Wind Cliffs", new Vector2(0.75f, 0.3f)),
        new("amber_shore", "Amber Shore", new Vector2(0.5f, 0.82f)),
        new("archive", "Archive", new Vector2(0.5f, 0.18f)),
    };

    private readonly InputManager _input;
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;
    private readonly Core.GameContent _content;
    private readonly WorldState _world;
    private readonly AudioManager? _audio;
    private SceneManager _manager = null!;

    public bool DrawsOverPreviousScene => true;

    public MapScene(InputManager input, Texture2D pixel, SpriteFont font, Core.GameContent content, WorldState world, AudioManager? audio = null)
    {
        _input = input;
        _pixel = pixel;
        _font = font;
        _content = content;
        _world = world;
        _audio = audio;
    }

    public void Enter(SceneManager manager)
    {
        _manager = manager;
        _audio?.PlaySfx("sfx_page_turn");
    }

    public void Exit() { }

    public void Update(GameTime gameTime)
    {
        _input.Update();
        if (_input.WasPressedThisFrame(GameAction.Cancel) || _input.WasPressedThisFrame(GameAction.OpenMap))
        {
            _manager.Pop();
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var viewport = _manager.Game.GraphicsDevice.Viewport;
        spriteBatch.Begin();
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(0x10, 0x0c, 0x14, 245));
        spriteBatch.DrawString(_font, "Map of Lanternmere", new Vector2(40, 24), new Color(0xd9, 0x9a, 0x3e));

        var mapArea = new Rectangle(80, 80, viewport.Width - 160, viewport.Height - 160);

        // Connective lines from the hub to every other known node, drawn faint.
        var hub = Layout[0];
        var hubPos = ToScreen(hub, mapArea);
        foreach (var node in Layout)
        {
            if (node.RegionId == hub.RegionId) continue;
            var pos = ToScreen(node, mapArea);
            DrawLine(spriteBatch, hubPos, pos, new Color(0x50, 0x48, 0x40));
        }

        foreach (var node in Layout)
        {
            var visited = _world.VisitedRegionIds.Contains(node.RegionId);
            var pos = ToScreen(node, mapArea);
            var color = visited ? new Color(0xd9, 0x9a, 0x3e) : new Color(0x40, 0x40, 0x40);
            var radius = 10;
            spriteBatch.Draw(_pixel, new Rectangle((int)pos.X - radius, (int)pos.Y - radius, radius * 2, radius * 2), color);

            var label = visited ? node.ShortLabel : "???";
            var size = _font.MeasureString(label);
            spriteBatch.DrawString(_font, label, pos + new Vector2(-size.X / 2f, radius + 4), visited ? Color.White : Color.Gray);
        }

        spriteBatch.DrawString(_font, "Esc/M: close", new Vector2(40, viewport.Height - 32), Color.Gray);
        spriteBatch.End();
    }

    private static Vector2 ToScreen(NodeLayout node, Rectangle area) =>
        new(area.X + node.RelativePosition.X * area.Width, area.Y + node.RelativePosition.Y * area.Height);

    private void DrawLine(SpriteBatch spriteBatch, Vector2 a, Vector2 b, Color color)
    {
        var delta = b - a;
        var length = delta.Length();
        var angle = System.MathF.Atan2(delta.Y, delta.X);
        spriteBatch.Draw(_pixel, a, null, color, angle, Vector2.Zero, new Vector2(length, 1.5f), SpriteEffects.None, 0f);
    }
}
