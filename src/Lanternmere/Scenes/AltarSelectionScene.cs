using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Lanternmere.Core;
using Lanternmere.Systems;

namespace Lanternmere.Scenes;

/// <summary>
/// Explicit tide-glass picker for the Amber Shore altar. The player can inspect
/// every carried shard, deliberately choose one, remove an already placed shard,
/// or cancel without changing puzzle state.
/// </summary>
public sealed class AltarSelectionScene : IScene
{
    private readonly InputManager _input;
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;
    private readonly GameContent _content;
    private readonly int _slot;
    private readonly IReadOnlyList<string> _shardItemIds;
    private readonly string? _placedItemId;
    private readonly Action<string> _placeItem;
    private readonly Action _removeItem;
    private readonly Dictionary<string, Texture2D> _icons = new(StringComparer.Ordinal);

    private SceneManager _manager = null!;
    private int _selected;

    public bool DrawsOverPreviousScene => true;

    public AltarSelectionScene(
        InputManager input,
        Texture2D pixel,
        SpriteFont font,
        GameContent content,
        int slot,
        IReadOnlyList<string> shardItemIds,
        string? placedItemId,
        Action<string> placeItem,
        Action removeItem)
    {
        _input = input;
        _pixel = pixel;
        _font = font;
        _content = content;
        _slot = slot;
        _shardItemIds = shardItemIds;
        _placedItemId = placedItemId;
        _placeItem = placeItem;
        _removeItem = removeItem;
    }

    private int ChoiceCount => _shardItemIds.Count + (_placedItemId is null ? 0 : 1);

    public void Enter(SceneManager manager)
    {
        _manager = manager;
        foreach (var itemId in _shardItemIds)
        {
            _icons[itemId] = TextureFactory.CreateIcon(
                manager.Game.GraphicsDevice,
                20,
                new Color(0x8f, 0xd4, 0xe3),
                TextureFactory.IconShapeForItem(itemId));
        }
    }

    public void Exit()
    {
        foreach (var icon in _icons.Values) icon.Dispose();
        _icons.Clear();
    }

    public void Update(GameTime gameTime)
    {
        _input.Update();

        if (_input.WasPressedThisFrame(GameAction.Cancel))
        {
            _manager.Pop();
            return;
        }

        if (ChoiceCount == 0) return;
        if (_input.WasPressedThisFrame(GameAction.MoveUp))
        {
            _selected = (_selected - 1 + ChoiceCount) % ChoiceCount;
        }
        else if (_input.WasPressedThisFrame(GameAction.MoveDown))
        {
            _selected = (_selected + 1) % ChoiceCount;
        }
        else if (_input.WasPressedThisFrame(GameAction.Confirm) || _input.WasPressedThisFrame(GameAction.Interact))
        {
            if (_selected < _shardItemIds.Count) _placeItem(_shardItemIds[_selected]);
            else _removeItem();
            _manager.Pop();
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var viewport = _manager.Game.GraphicsDevice.Viewport;
        var panelWidth = Math.Min(620, viewport.Width - 64);
        var panelHeight = Math.Min(390, viewport.Height - 64);
        var panel = new Rectangle((viewport.Width - panelWidth) / 2, (viewport.Height - panelHeight) / 2, panelWidth, panelHeight);

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black * 0.58f);
        spriteBatch.Draw(_pixel, panel, new Color(0x13, 0x16, 0x22) * 0.98f);
        DrawBorder(spriteBatch, panel, new Color(0xc8, 0x9c, 0x55));

        var x = panel.X + 30;
        var y = panel.Y + 24;
        spriteBatch.DrawString(_font, $"Tide-glass altar - slot {_slot + 1}", new Vector2(x, y), new Color(0xf2, 0xd9, 0xb3));
        y += 34;
        spriteBatch.DrawString(_font, "Choose the shard whose light belongs in this position.", new Vector2(x, y), Color.LightGray);
        y += 38;

        for (var i = 0; i < _shardItemIds.Count; i++)
        {
            var itemId = _shardItemIds[i];
            var selected = i == _selected;
            var row = new Rectangle(x - 10, (int)y - 7, panel.Width - 40, 36);
            if (selected) spriteBatch.Draw(_pixel, row, new Color(0x4b, 0x3a, 0x2b) * 0.9f);
            if (_icons.TryGetValue(itemId, out var icon)) spriteBatch.Draw(icon, new Vector2(x, y), Color.White);
            var name = _content.Items.TryGet(itemId, out var item) ? item.DisplayName : itemId;
            var marker = selected ? ">" : " ";
            spriteBatch.DrawString(_font, $"{marker}  {name}", new Vector2(x + 28, y), selected ? new Color(0xff, 0xd1, 0x72) : Color.White);
            y += 42;
        }

        if (_placedItemId is not null)
        {
            var index = _shardItemIds.Count;
            var selected = index == _selected;
            var row = new Rectangle(x - 10, (int)y - 7, panel.Width - 40, 36);
            if (selected) spriteBatch.Draw(_pixel, row, new Color(0x4b, 0x3a, 0x2b) * 0.9f);
            var placedName = _content.Items.TryGet(_placedItemId, out var item) ? item.DisplayName : _placedItemId;
            var marker = selected ? ">" : " ";
            spriteBatch.DrawString(_font, $"{marker}  Remove {placedName}", new Vector2(x + 28, y), selected ? new Color(0xff, 0xd1, 0x72) : Color.LightGray);
        }

        spriteBatch.DrawString(_font, "Up/Down: choose   Enter/E: confirm   Esc: cancel", new Vector2(x, panel.Bottom - 42), Color.Gray);
        spriteBatch.End();
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color)
    {
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, 2), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, 2, rect.Height), color);
        spriteBatch.Draw(_pixel, new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height), color);
    }
}
