using Microsoft.Xna.Framework;
using Lanternmere.Systems;

namespace Lanternmere.Entities;

/// <summary>
/// Eight-direction movement with consistent acceleration/deceleration (per
/// the brief's "Required gameplay systems"). Velocity-based rather than
/// direct position setting so movement feels consistent regardless of
/// how many directional keys are held, and so it composes cleanly with
/// the collision system's per-axis slide resolution.
/// </summary>
public sealed class Player
{
    public Vector2 Position;
    public Vector2 Velocity;

    public float MaxSpeed = 120f; // pixels/second
    public float Acceleration = 900f; // pixels/second^2
    public float Deceleration = 1200f; // pixels/second^2 when no input held

    public int Width = 16;
    public int Height = 20;

    public Rectangle Bounds => new(
        (int)(Position.X - Width / 2f),
        (int)(Position.Y - Height / 2f),
        Width,
        Height);

    public void Update(GameTime gameTime, InputManager input, CollisionSystem collision)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        var direction = Vector2.Zero;
        if (input.IsHeld(GameAction.MoveUp)) direction.Y -= 1;
        if (input.IsHeld(GameAction.MoveDown)) direction.Y += 1;
        if (input.IsHeld(GameAction.MoveLeft)) direction.X -= 1;
        if (input.IsHeld(GameAction.MoveRight)) direction.X += 1;

        if (direction != Vector2.Zero)
        {
            direction.Normalize(); // 8-direction movement should not be faster diagonally
            Velocity += direction * Acceleration * dt;
            if (Velocity.Length() > MaxSpeed)
            {
                Velocity = Vector2.Normalize(Velocity) * MaxSpeed;
            }
        }
        else if (Velocity != Vector2.Zero)
        {
            var decel = Deceleration * dt;
            if (Velocity.Length() <= decel)
            {
                Velocity = Vector2.Zero;
            }
            else
            {
                Velocity -= Vector2.Normalize(Velocity) * decel;
            }
        }

        var delta = Velocity * dt;
        var resolvedDelta = collision.ResolveMove(Bounds, delta);

        // Zero out velocity on any axis the collision system blocked, so the
        // player doesn't feel "stuck" accelerating into a wall.
        if (resolvedDelta.X == 0f) Velocity.X = 0f;
        if (resolvedDelta.Y == 0f) Velocity.Y = 0f;

        Position += resolvedDelta;
    }
}
