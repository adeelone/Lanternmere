using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Lanternmere.Core;
using Lanternmere.Systems;

namespace Lanternmere.Scenes;

/// <summary>Inventory: the small fixed set of meaningful items the player is carrying.</summary>
public sealed class InventoryScene : IScene
{
    private readonly InputManager _input;
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;
    private readonly Core.GameContent _content;
    private readonly WorldState _world;
    private readonly AudioManager? _audio;
    private SceneManager _manager = null!;
    private int _selected;
    private readonly Dictionary<string, Texture2D> _iconsByItemId = new();

    public bool DrawsOverPreviousScene => true;

    public InventoryScene(InputManager input, Texture2D pixel, SpriteFont font, Core.GameContent content, WorldState world, AudioManager? audio = null)
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
        var device = manager.Game.GraphicsDevice;
        foreach (var itemId in _world.InventoryItemIds)
        {
            _iconsByItemId[itemId] = TextureFactory.CreateIcon(device, 14, new Color(0x9a, 0xd9, 0xe0), TextureFactory.IconShapeForItem(itemId));
        }
        _audio?.PlaySfx("sfx_page_turn");
    }

    public void Exit() { }

    public void Update(GameTime gameTime)
    {
        _input.Update();
        var items = _world.InventoryItemIds;

        if (_input.WasPressedThisFrame(GameAction.Cancel) || _input.WasPressedThisFrame(GameAction.OpenInventory))
        {
            _manager.Pop();
            return;
        }
        if (items.Count == 0) return;

        if (_input.WasPressedThisFrame(GameAction.MoveUp)) _selected = (_selected - 1 + items.Count) % items.Count;
        else if (_input.WasPressedThisFrame(GameAction.MoveDown)) _selected = (_selected + 1) % items.Count;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var viewport = _manager.Game.GraphicsDevice.Viewport;
        var items = _world.InventoryItemIds;

        spriteBatch.Begin();
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(0x10, 0x0c, 0x14, 245));
        spriteBatch.DrawString(_font, "Inventory", new Vector2(40, 24), new Color(0xd9, 0x9a, 0x3e));

        if (items.Count == 0)
        {
            spriteBatch.DrawString(_font, "You aren't carrying anything yet.", new Vector2(40, 70), Color.Gray);
            spriteBatch.End();
            return;
        }

        var y = 70f;
        for (var i = 0; i < items.Count; i++)
        {
            var color = i == _selected ? new Color(0xd9, 0x9a, 0x3e) : Color.LightGray;
            var prefix = i == _selected ? "> " : "  ";
            var displayName = _content.Items.TryGet(items[i], out var def) ? def.DisplayName : items[i];
            if (_iconsByItemId.TryGetValue(items[i], out var icon))
            {
                spriteBatch.Draw(icon, new Vector2(40, y + 2), Color.White);
            }
            spriteBatch.DrawString(_font, prefix + displayName, new Vector2(64, y), color);
            y += 24;
        }

        if (_content.Items.TryGet(items[_selected], out var selectedItem))
        {
            spriteBatch.DrawString(_font, selectedItem.Description, new Vector2(320, 70), Color.White);
        }

        spriteBatch.DrawString(_font, "Up/Down: browse   Esc/I: close", new Vector2(40, viewport.Height - 32), Color.Gray);
        spriteBatch.End();
    }
}
