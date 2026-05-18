namespace Plunger.Models
{
    using System;
    using Plunger.Models.Common;

    // Condition and Cannon defined in Enums.cs — do NOT redefine here.

    public class Sucker
    {
        // ── State ─────────────────────────────────────────────────────────────
        public Plunger.Point   Location       { get; private set; }
        public int             Speed          { get; private set; }
        public Condition       Condition      { get; private set; }   // not a name clash because
        public Cannon          WeaponType     { get; private set; }   //  the property *type* is Condition
        public int             CoinsCollected { get; private set; }
        public bool            SomersaultThisFrame { get; private set; }
        public double          AimAngle       { get; private set; } = -45.0;
        public PlungerProjectile Projectile   { get; private set; }
        public bool            CanGrappleGround { get; set; } = true;

        // ── Physics ───────────────────────────────────────────────────────────
        private const int    RunSpeed     = 9;
        private const int    PullSpeed    = RunSpeed * 2;   // 18 px / tick
        private const int    FallSpeed    = PullSpeed;
        private const double MaxRange     = 900;
        private const double AimDelta     = 12.5;           // 2.5× original 5°

        // ── World ─────────────────────────────────────────────────────────────
        public const int CeilingY   = 28;
        public const int FloorY     = 380;
        public const int WorldWidth = 1315;

        // ── Constructor ───────────────────────────────────────────────────────
        public Sucker(Plunger.Point location, int speed, Condition condition,
                      Cannon cannon = Cannon.Plunger)
        {
            Location  = location;
            Speed     = speed;
            Condition = condition;
            WeaponType = cannon;
            Projectile = new PlungerProjectile();
        }

        // ── Input ─────────────────────────────────────────────────────────────
        public void AimUp()   { AimAngle -= AimDelta; if (AimAngle < -180) AimAngle = -180; }
        public void AimDown() { AimAngle += AimDelta; if (AimAngle >    0) AimAngle =    0; }

        public void ConsumeSomersault() => SomersaultThisFrame = false;

        public void Detach()
        {
            if (Condition != Condition.Attached) return;
            Condition = Condition.Fall;
            Projectile.Stop();
            SomersaultThisFrame = true;
        }

        public void Shoot()
        {
            if (Condition == Condition.Attached) { Detach(); return; }
            if (Projectile.IsActive) { Projectile.Stop(); return; }
            Projectile.Launch(new Plunger.Point(Location.X + 60, Location.Y + 60), AimAngle);
        }

        // ── Physics update ────────────────────────────────────────────────────
        public void UpdatePosition(double dt, PlatformList? platforms = null)
        {
            if (Projectile.IsActive)
            {
                Projectile.Update();
                TryCeilingAttach();
                if (CanGrappleGround) TryGroundAttach();
                if (platforms != null) TryPlatformAttach(platforms);
                if (!Projectile.IsActive) goto afterProjectile; // out-of-range stops it
                CheckOutOfRange();
            }
            afterProjectile:

            switch (Condition)
            {
                case Condition.Run:
                    Location = new Plunger.Point(Location.X + RunSpeed, Location.Y);
                    if (Location.X > WorldWidth) Location = new Plunger.Point(-120, Location.Y);
                    break;

                case Condition.Attached:
                {
                    double dx   = Projectile.Location.X - (Location.X + 60);
                    double dy   = Projectile.Location.Y - (Location.Y + 60);
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist > PullSpeed)
                    {
                        Location = new Plunger.Point(
                            Location.X + (int)(dx / dist * PullSpeed),
                            Location.Y + (int)(dy / dist * PullSpeed));
                    }
                    else
                    {
                        Condition = Condition.Fall;
                        Projectile.Stop();
                        SomersaultThisFrame = true;
                    }
                    break;
                }

                case Condition.Fall:
                {
                    Location = new Plunger.Point(Location.X, Location.Y + FallSpeed);
                    if (platforms != null)
                        foreach (var plat in platforms.Items)
                        {
                            var pb   = plat.GetBounds();
                            int bot  = Location.Y + 120;
                            int prev = bot - FallSpeed;
                            if (bot >= pb.Top && prev <= pb.Top
                                && Location.X + 100 > pb.Left
                                && Location.X + 20  < pb.Right)
                            {
                                Location  = new Plunger.Point(Location.X, pb.Top - 120);
                                Condition = Condition.Run;
                                break;
                            }
                        }
                    if (Location.Y >= FloorY)
                    { Location = new Plunger.Point(Location.X, FloorY); Condition = Condition.Run; }
                    break;
                }
            }

            // Clamp Y
            int cy = Math.Max(CeilingY, Math.Min(FloorY, Location.Y));
            if (cy != Location.Y) Location = new Plunger.Point(Location.X, cy);
        }

        // ── Projectile attach helpers ─────────────────────────────────────────
        private void TryCeilingAttach()
        {
            if (Projectile.Location.Y > CeilingY) return;
            Projectile.PinTo(new Plunger.Point(Projectile.Location.X, CeilingY));
            Condition = Condition.Attached;
            SomersaultThisFrame = true;
        }

        private void TryGroundAttach()
        {
            if (Projectile.Location.Y < FloorY) return;
            Projectile.PinTo(new Plunger.Point(Projectile.Location.X, FloorY));
            Condition = Condition.Attached;
            SomersaultThisFrame = true;
        }

        private void TryPlatformAttach(PlatformList platforms)
        {
            var pb = Projectile.GetBounds();
            foreach (var plat in platforms.Items)
                if (pb.IntersectsWith(plat.GetBounds()))
                {
                    Projectile.PinTo(new Plunger.Point(Projectile.Location.X, plat.GetBounds().Top));
                    Condition = Condition.Attached;
                    SomersaultThisFrame = true;
                    return;
                }
        }

        private void CheckOutOfRange()
        {
            double dx = Projectile.Location.X - (Location.X + 60);
            double dy = Projectile.Location.Y - (Location.Y + 60);
            if (dx * dx + dy * dy > MaxRange * MaxRange) Projectile.Stop();
        }

        // ── Misc ──────────────────────────────────────────────────────────────
        public void AddCoin() => CoinsCollected++;

        public Rectangle GetBounds(int w, int h)
            => new Rectangle(Location.X, Location.Y, w, h);
    }
}
