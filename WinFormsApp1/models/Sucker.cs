namespace Plunger.Models;

using System;
using Plunger.Models.Common;

public class Sucker
{
    public Point Location { get; private set; }
    public int Speed { get; private set; }
    public Condition Condition { get; private set; }
    public Cannon Cannon { get; private set; }
    public int CoinsCollected { get; private set; }

    // True for exactly one frame after attaching or detaching — used to trigger somersault row in View
    public bool SomersaultThisFrame { get; private set; }

    public double AimAngle { get; private set; } = -45; // Default: fire up-right
    public PlungerProjectile Projectile { get; private set; }

    // ── Tuneable constants ───────────────────────────────────────────────────
    // Horizontal run speed (pixels/tick)
    private const int HorizontalRunSpeed = 5;

    // Vertical pull speed = 2× horizontal run speed (requirement)
    private const int VerticalPullSpeed = HorizontalRunSpeed * 2;

    // Max distance (px) the plunger may travel before auto-returning
    private const double MaxProjectileRange = 900;

    // Ceiling Y boundary (pixels from top of world)
    public const int CeilingY = 28; // 5% of 558 ≈ 28 px  — kept in sync with Form1

    // Floor Y boundary
    public const int FloorY = 530;  // 558 - 28

    // ────────────────────────────────────────────────────────────────────────

    public Sucker(Point location, int speed, Condition condition, Cannon cannon = Cannon.Plunger)
    {
        Location = location;
        Speed = HorizontalRunSpeed;
        Condition = condition;
        Cannon = cannon;
        CoinsCollected = 0;
        Projectile = new PlungerProjectile();
    }

    // ── Aiming (A / D keys) ─────────────────────────────────────────────────
    // In WinForms Y grows downward, so negative angle = upward
    public void AimUp()   { AimAngle -= 5; }
    public void AimDown() { AimAngle += 5; }

    // ── Shoot / cancel (W key) ───────────────────────────────────────────────
    public void Shoot()
    {
        if (Projectile.IsActive)
        {
            // Cancel in-flight plunger — return it instantly
            Projectile.Stop();
            // If we were somehow attached, drop into fall
            if (Condition == Condition.Attached)
            {
                TriggerSomersault();
                Condition = Condition.Fall;
            }
        }
        else if (Condition == Condition.Attached)
        {
            // Already attached — treat as detach
            Detach();
        }
        else
        {
            // Fire a fresh plunger from the centre of the digger sprite
            var firePoint = new Point(Location.X + 60, Location.Y + 60);
            Projectile.Launch(firePoint, AimAngle);
        }
    }

    // ── Detach (Space key) ───────────────────────────────────────────────────
    public void Detach()
    {
        if (Condition == Condition.Attached)
        {
            TriggerSomersault();
            Condition = Condition.Fall;
            Projectile.Stop();
        }
    }

    // ── Called by Presenter every tick ──────────────────────────────────────
    public void UpdatePosition(double deltaTime)
    {
        // Clear the single-frame somersault flag at the top of each tick
        SomersaultThisFrame = false;

        switch (Condition)
        {
            case Condition.Run:
                UpdateRun();
                break;

            case Condition.Attached:
                UpdateAttached();
                break;

            case Condition.Fall:
                UpdateFall();
                break;
        }

        ClampToBounds();
    }

    // ── State updaters ───────────────────────────────────────────────────────
    private void UpdateRun()
    {
        Location += new Point(HorizontalRunSpeed, 0);

        // Advance the in-flight plunger; check range and ceiling attachment
        if (Projectile.IsActive)
        {
            Projectile.Update();
            CheckProjectileOutOfRange();
            CheckProjectileCeilingAttach();
        }
    }

    private void UpdateAttached()
    {
        // Pull the digger toward the attached plunger.
        // Horizontal: same as run speed; Vertical: 2× run speed (requirement).
        double dx = Projectile.Location.X - Location.X;
        double dy = Projectile.Location.Y - Location.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);

        if (distance <= Speed)
        {
            // Close enough — snap to plunger and auto-detach (requirement)
            Location = Projectile.Location;
            TriggerSomersault();
            Condition = Condition.Fall;
            Projectile.Stop();
        }
        else
        {
            // Move horizontally at HorizontalRunSpeed, vertically at VerticalPullSpeed
            // Normalise each axis independently so the speeds are independent.
            int moveX = (dx == 0) ? 0 : (int)(Math.Sign(dx) * Math.Min(HorizontalRunSpeed, Math.Abs(dx)));
            int moveY = (dy == 0) ? 0 : (int)(Math.Sign(dy) * Math.Min(VerticalPullSpeed, Math.Abs(dy)));
            Location += new Point(moveX, moveY);
        }
    }

    private void UpdateFall()
    {
        // Simple gravity; half the run speed downward
        Location += new Point(HorizontalRunSpeed, HorizontalRunSpeed / 2);

        // Land on floor
        if (Location.Y >= FloorY)
        {
            Location = new Point(Location.X, FloorY);
            Condition = Condition.Run;
        }
    }

    // ── Projectile helpers ───────────────────────────────────────────────────
    private void CheckProjectileOutOfRange()
    {
        double dx = Projectile.Location.X - Location.X;
        double dy = Projectile.Location.Y - Location.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);

        if (distance > MaxProjectileRange)
        {
            Projectile.Stop(); // Instantly returns to pocket — no freeze
        }
    }

    private void CheckProjectileCeilingAttach()
    {
        // The plunger attaches when it reaches or passes the ceiling boundary
        if (Projectile.Location.Y <= CeilingY)
        {
            // Pin the plunger exactly on the ceiling so the rope looks correct
            Projectile.PinTo(new Point(Projectile.Location.X, CeilingY));
            TriggerSomersault();          // Somersault on attach (requirement)
            Condition = Condition.Attached;
        }
    }

    // ── Utilities ────────────────────────────────────────────────────────────
    private void TriggerSomersault()
    {
        SomersaultThisFrame = true;
    }

    private void ClampToBounds()
    {
        int x = Location.X;
        int y = Math.Max(CeilingY, Math.Min(FloorY, Location.Y));
        if (y != Location.Y)
            Location = new Point(x, y);
    }

    public void AddCoin()
    {
        CoinsCollected++;
    }

    // Hitbox used for coin collision — strictly the main body only
    public Rectangle GetBounds(int w, int h)
    {
        return new Rectangle(Location.X, Location.Y, w, h);
    }
}
