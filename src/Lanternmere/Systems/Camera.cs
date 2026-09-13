using System;
using Microsoft.Xna.Framework;

namespace Lanternmere.Systems;

/// <summary>
/// Follows the player, clamps to the current region's pixel bounds so the
/// camera never shows past the edge of a small region, and supports a
/// timed screen shake that respects the "reduced shake" accessibility
/// setting (a disabled shake is simply never applied, not applied-then-
/// hidden, per the brief's "subtle shake with disable option").
/// </summary>
public sealed class Camera
{
    private readonly Random _rng = new();

    public Vector2 Position { get; private set; }
    public Rectangle RegionBoundsPixels { get; private set; }
    public int ViewportWidth { get; set; }
    public int ViewportHeight { get; set; }

    private float _shakeMagnitude;
    private float _shakeTimeRemaining;
    private Vector2 _shakeOffset;

    public void SetRegionBounds(int widthPixels, int heightPixels) =>
        RegionBoundsPixels = new Rectangle(0, 0, widthPixels, heightPixels);

    public void SnapTo(Vector2 target)
    {
        Position = target;
        ClampToBounds();
    }

    public void Follow(Vector2 target, GameTime gameTime)
    {
        Position = target;
        ClampToBounds();
        UpdateShake(gameTime);
    }

    public void Shake(float magnitude, float durationSeconds, bool reducedShakeEnabled)
    {
        if (reducedShakeEnabled) return;
        _shakeMagnitude = magnitude;
        _shakeTimeRemaining = durationSeconds;
    }

    private void UpdateShake(GameTime gameTime)
    {
        if (_shakeTimeRemaining <= 0f)
        {
            _shakeOffset = Vector2.Zero;
            return;
        }

        _shakeTimeRemaining -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        var falloff = Math.Max(0f, _shakeTimeRemaining);
        _shakeOffset = new Vector2(
            (float)(_rng.NextDouble() * 2 - 1) * _shakeMagnitude * falloff,
            (float)(_rng.NextDouble() * 2 - 1) * _shakeMagnitude * falloff);
    }

    private void ClampToBounds()
    {
        if (RegionBoundsPixels.Width <= ViewportWidth)
        {
            Position = Position with { X = RegionBoundsPixels.Center.X };
        }
        else
        {
            var minX = RegionBoundsPixels.Left + ViewportWidth / 2f;
            var maxX = RegionBoundsPixels.Right - ViewportWidth / 2f;
            Position = Position with { X = Math.Clamp(Position.X, minX, maxX) };
        }

        if (RegionBoundsPixels.Height <= ViewportHeight)
        {
            Position = Position with { Y = RegionBoundsPixels.Center.Y };
        }
        else
        {
            var minY = RegionBoundsPixels.Top + ViewportHeight / 2f;
            var maxY = RegionBoundsPixels.Bottom - ViewportHeight / 2f;
            Position = Position with { Y = Math.Clamp(Position.Y, minY, maxY) };
        }
    }

    public Matrix GetViewMatrix() =>
        Matrix.CreateTranslation(new Vector3(
            -(float)Math.Round(Position.X - ViewportWidth / 2f - _shakeOffset.X),
            -(float)Math.Round(Position.Y - ViewportHeight / 2f - _shakeOffset.Y),
            0f));
}
