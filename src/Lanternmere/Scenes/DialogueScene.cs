using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Lanternmere.Core;
using Lanternmere.Core.Content;
using Lanternmere.Systems;

namespace Lanternmere.Scenes;

/// <summary>
/// Renders a <see cref="DialogueRunner"/>: speaker + text with a typewriter
/// reveal (respecting the accessibility "text speed" and "instant text"
/// settings), branching choices, and a toggleable history log — per the
/// brief's "Branching dialogue, conditions, choices, and a dialogue
/// history." Overlays whatever scene pushed it (typically RegionScene) and
/// pops itself when the conversation ends.
/// </summary>
public sealed class DialogueScene : IScene
{
    private const float BaseCharsPerSecond = 42f;

    private readonly InputManager _input;
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;
    private readonly GameSettings _settings;
    private readonly AudioManager? _audio;
    private readonly DialogueRunner _runner;
    private readonly List<(string Speaker, string Text)> _history = new();
    private readonly Action? _onFinished;

    private int _selectedChoice;
    private float _revealedChars;
    private bool _showHistory;
    private SceneManager _manager = null!;
    private string _lastNodeId = "";

    public bool DrawsOverPreviousScene => true;

    public DialogueScene(InputManager input, Texture2D pixel, SpriteFont font, GameSettings settings, AudioManager? audio,
        DialogueTree tree, WorldState world, Action? onFinished = null)
    {
        _input = input;
        _pixel = pixel;
        _font = font;
        _settings = settings;
        _audio = audio;
        _runner = new DialogueRunner(tree, world);
        _onFinished = onFinished;
    }

    public void Enter(SceneManager manager)
    {
        _manager = manager;
        RecordCurrentNodeIfNew();
        if (_runner.IsFinished)
        {
            // A one-node/end-only tree (e.g. an ad-hoc examine-text popup that immediately ends).
        }
    }

    public void Exit() { }

    public void Update(GameTime gameTime)
    {
        _input.Update();

        if (_runner.IsFinished)
        {
            _manager.Pop();
            _onFinished?.Invoke();
            return;
        }

        RecordCurrentNodeIfNew();

        if (_input.WasPressedThisFrame(GameAction.OpenJournal))
        {
            _showHistory = !_showHistory;
            return;
        }
        if (_showHistory) return; // history overlay eats input until closed

        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var fullText = _runner.CurrentNode.Text;
        if (_settings.InstantText)
        {
            _revealedChars = fullText.Length;
        }
        else
        {
            _revealedChars = Math.Min(fullText.Length, _revealedChars + BaseCharsPerSecond * Math.Max(0.1f, _settings.TextSpeed) * dt);
        }

        var fullyRevealed = _revealedChars >= fullText.Length;
        var choices = _runner.AvailableChoices;

        if (choices.Count > 0)
        {
            if (!fullyRevealed)
            {
                if (_input.WasPressedThisFrame(GameAction.SkipText) || _input.WasPressedThisFrame(GameAction.Confirm))
                {
                    _revealedChars = fullText.Length;
                }
                return;
            }

            if (_input.WasPressedThisFrame(GameAction.MoveUp))
            {
                _selectedChoice = (_selectedChoice - 1 + choices.Count) % choices.Count;
                _audio?.PlaySfx("sfx_menu_move");
            }
            else if (_input.WasPressedThisFrame(GameAction.MoveDown))
            {
                _selectedChoice = (_selectedChoice + 1) % choices.Count;
                _audio?.PlaySfx("sfx_menu_move");
            }
            else if (_input.WasPressedThisFrame(GameAction.Confirm))
            {
                _audio?.PlaySfx("sfx_menu_select");
                _runner.Choose(_selectedChoice);
                _selectedChoice = 0;
                _revealedChars = 0;
            }
            return;
        }

        if (_input.WasPressedThisFrame(GameAction.AdvanceText) || _input.WasPressedThisFrame(GameAction.Confirm))
        {
            if (!fullyRevealed)
            {
                _revealedChars = fullText.Length;
            }
            else
            {
                _runner.Advance();
                _revealedChars = 0;
            }
        }
        else if (_input.WasPressedThisFrame(GameAction.SkipText) && !fullyRevealed)
        {
            _revealedChars = fullText.Length;
        }
    }

    private void RecordCurrentNodeIfNew()
    {
        var node = _runner.CurrentNode;
        if (node.Id == _lastNodeId) return;
        _lastNodeId = node.Id;
        if (!string.IsNullOrWhiteSpace(node.Text))
        {
            _history.Add((node.Speaker, node.Text));
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var viewport = _manager.Game.GraphicsDevice.Viewport;
        spriteBatch.Begin();

        if (_showHistory)
        {
            DrawHistory(spriteBatch, viewport);
            spriteBatch.End();
            return;
        }

        var boxHeight = 130;
        var boxRect = new Rectangle(20, viewport.Height - boxHeight - 20, viewport.Width - 40, boxHeight);
        spriteBatch.Draw(_pixel, boxRect, new Color(0x14, 0x10, 0x18, 235));
        spriteBatch.Draw(_pixel, new Rectangle(boxRect.X, boxRect.Y, boxRect.Width, 2), new Color(0xd9, 0x9a, 0x3e));

        var speaker = _runner.CurrentNode.Speaker;
        var textY = boxRect.Y + 12f;
        if (!string.IsNullOrWhiteSpace(speaker))
        {
            spriteBatch.DrawString(_font, speaker, new Vector2(boxRect.X + 16, textY), new Color(0xd9, 0x9a, 0x3e));
            textY += _font.LineSpacing + 4;
        }

        var fullText = _runner.CurrentNode.Text;
        var shown = fullText.Length == 0 ? "" : fullText.Substring(0, (int)Math.Min(fullText.Length, _revealedChars));
        TextRenderer.DrawWrapped(spriteBatch, _font, shown, new Vector2(boxRect.X + 16, textY), boxRect.Width - 32, Color.White);

        var choices = _runner.AvailableChoices;
        var fullyRevealed = _revealedChars >= fullText.Length;
        if (choices.Count > 0 && fullyRevealed)
        {
            var choiceY = boxRect.Y + boxHeight - choices.Count * (_font.LineSpacing + 2) - 8;
            for (var i = 0; i < choices.Count; i++)
            {
                var color = i == _selectedChoice ? new Color(0xd9, 0x9a, 0x3e) : Color.LightGray;
                var prefix = i == _selectedChoice ? "> " : "  ";
                spriteBatch.DrawString(_font, prefix + choices[i].Text, new Vector2(boxRect.X + 16, choiceY), color);
                choiceY += _font.LineSpacing + 2;
            }
        }
        else if (fullyRevealed)
        {
            var hint = "[Continue]";
            var size = _font.MeasureString(hint);
            spriteBatch.DrawString(_font, hint, new Vector2(boxRect.Right - size.X - 16, boxRect.Bottom - size.Y - 8), Color.Gray);
        }

        spriteBatch.End();
    }

    private void DrawHistory(SpriteBatch spriteBatch, Viewport viewport)
    {
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(0x10, 0x0c, 0x14, 245));
        spriteBatch.DrawString(_font, "Dialogue history (press Journal key to close)", new Vector2(20, 16), new Color(0xd9, 0x9a, 0x3e));

        var y = 16f + _font.LineSpacing + 12;
        foreach (var (speaker, text) in _history)
        {
            var line = string.IsNullOrWhiteSpace(speaker) ? text : $"{speaker}: {text}";
            y = TextRenderer.DrawWrapped(spriteBatch, _font, line, new Vector2(20, y), viewport.Width - 40, Color.White) + 6;
        }
    }

    /// <summary>Builds a one-off, single-node dialogue tree for a flat examine-text popup, so landmarks/signs without a full conversation reuse the same rendering/history/typewriter machinery as real NPC dialogue.</summary>
    public static DialogueTree BuildExamineTree(string id, string text) => new()
    {
        Id = id,
        Nodes = new List<DialogueNode>
        {
            new() { Id = "start", Speaker = "", Text = text, EndsConversation = true },
        },
    };
}
