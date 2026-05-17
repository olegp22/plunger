namespace Plunger.Models;

using System;
using Plunger.Models.Common;

public class PlungerProjectile
{
    private double _exactX;
    private double _exactY;

    private double _velocityX;
    private double _velocityY;

    // Public read-only position (snapped to int for rendering)
    public Point Location => new Point((int)_exactX, (int)_exactY);
    public bool IsActive { get; private set; }

    // ── Speed ────────────────────────────────────────────────────────────────
    // Original was 20f; requirement is 3x–4x, so we use 70f (3.5×).
    // Adjust this single constant to tune feel without touching other logic.
    public const float Speed = 70f;

    private const int Size = 15;

    // ── Launch ───────────────────────────────────────────────────────────────
    /// <summary>Launches the plunger from <paramref name="startLocation"/> at
    /// <paramref name="angleInDegrees"/> (0° = right, negative = up in screen space).</summary>
    public void Launch(Point startLocation, double angleInDegrees)
    {
        _exactX = startLocation.X;
        _exactY = startLocation.Y;

        double rad = angleInDegrees * (Math.PI / 180.0);
        _velocityX = Math.Cos(rad) * Speed;
        _velocityY = Math.Sin(rad) * Speed;

        IsActive = true;
    }

    // ── Per-tick movement ────────────────────────────────────────────────────
    public void Update()
    {
        if (!IsActive) return;
        _exactX += _velocityX;
        _exactY += _velocityY;
    }

    // ── Stop / return to pocket ──────────────────────────────────────────────
    /// <summary>Instantly deactivates the plunger (returns it to the digger's pocket).
    /// This prevents the freeze-bug where an out-of-range plunger blocks re-firing.</summary>
    public void Stop()
    {
        IsActive = false;
        _velocityX = 0;
        _velocityY = 0;
    }

    // ── Ceiling pin ──────────────────────────────────────────────────────────
    /// <summary>Pins the plunger at a fixed world position after it sticks to the ceiling.
    /// Velocity is zeroed so it no longer drifts. <see cref="IsActive"/> stays true
    /// so the rope continues to render until the digger detaches.</summary>
    public void PinTo(Point position)
    {
        _exactX = position.X;
        _exactY = position.Y;
        _velocityX = 0;
        _velocityY = 0;
        // IsActive intentionally left true — the rope must still be drawn.
    }

    // ── Hitbox ───────────────────────────────────────────────────────────────
    public Rectangle GetBounds()
    {
        return new Rectangle((int)_exactX, (int)_exactY, Size, Size);
    }
}
