using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Lanternmere.Core;
using Lanternmere.Systems;

namespace Lanternmere.Scenes;

/// <summary>
/// Journal: every discovery unlocked so far (lore, landmark sketches, NPC
/// notes), replayable at any time — the brief's "replayable critical
/// clues" accessibility requirement, satisfied by discoveries persisting
/// and always being reachable here rather than only appearing once in a
/// dialogue box.
/// </summary>
public sealed class JournalScene : IScene
{
    private readonly InputManager _input;
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;
    private readonly Core.GameContent _content;
    private readonly WorldState _world;
    private readonly AudioManager? _audio;
    private SceneManager _manager = null!;
    private int _selected;

    public bool DrawsOverPreviousScene => true;

    public JournalScene(InputManager input, Texture2D pixel, SpriteFont font, Core.GameContent content, WorldState world, AudioManager? audio = null)
    {
        _input = input;
        _pixel = pixel;
        _font = font;
        _content = content;
        _world = world;
        _audio = audio;
    }

    // Cached once on Enter rather than recomputed via LINQ on every Update
    // and Draw call — the journal can't change contents while this
    // blocking overlay is open, so there's nothing to invalidate.
    private List<Core.Content.DiscoveryDefinition> _entries = new();

    public void Enter(SceneManager manager)
    {
        _manager = manager;
        _entries = _content.Discoveries.UnlockedFor(_world).ToList();
        _audio?.PlaySfx("sfx_page_turn");
    }

    public void Exit() { }

    public void Update(GameTime gameTime)
    {
        _input.Update();

        if (_input.WasPressedThisFrame(GameAction.Cancel) || _input.WasPressedThisFrame(GameAction.OpenJournal))
        {
            _manager.Pop();
            return;
        }
        if (_entries.Count == 0) return;

        if (_input.WasPressedThisFrame(GameAction.MoveUp)) _selected = (_selected - 1 + _entries.Count) % _entries.Count;
        else if (_input.WasPressedThisFrame(GameAction.MoveDown)) _selected = (_selected + 1) % _entries.Count;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var viewport = _manager.Game.GraphicsDevice.Viewport;
        var entries = _entries;

        spriteBatch.Begin();
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(0x10, 0x0c, 0x14, 245));
        spriteBatch.DrawString(_font, "Journal", new Vector2(40, 24), new Color(0xd9, 0x9a, 0x3e));

        if (entries.Count == 0)
        {
            spriteBatch.DrawString(_font, "Nothing recorded yet.", new Vector2(40, 70), Color.Gray);
            spriteBatch.End();
            return;
        }

        var listWidth = 260;
        var y = 70f;
        for (var i = 0; i < entries.Count; i++)
        {
            var color = i == _selected ? new Color(0xd9, 0x9a, 0x3e) : Color.LightGray;
            var prefix = i == _selected ? "> " : "  ";
            spriteBatch.DrawString(_font, prefix + entries[i].Title, new Vector2(40, y), color);
            y += 24;
        }

        var detail = entries[_selected];
        var detailX = 40 + listWidth;
        spriteBatch.DrawString(_font, $"[{detail.Category}] {detail.Title}", new Vector2(detailX, 70), new Color(0xd9, 0x9a, 0x3e));
        TextRenderer.DrawWrapped(spriteBatch, _font, detail.Text, new Vector2(detailX, 100), viewport.Width - detailX - 40, Color.White);

        spriteBatch.DrawString(_font, "Up/Down: browse   Esc/J: close", new Vector2(40, viewport.Height - 32), Color.Gray);
        spriteBatch.End();
    }
}
