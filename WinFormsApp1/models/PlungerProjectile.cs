namespace Plunger.Models
{
    using System;
    using Plunger.Models.Common;

    public class PlungerProjectile
    {
        private double _x, _y, _vx, _vy;

        public Plunger.Point Location => new Plunger.Point((int)_x, (int)_y);
        public bool IsActive { get; private set; }

        public const float Speed = 70f;
        private const int  Size  = 15;

        public void Launch(Plunger.Point start, double angleDeg)
        {
            _x = start.X; _y = start.Y;
            double rad = angleDeg * (Math.PI / 180.0);
            _vx = Math.Cos(rad) * Speed;
            _vy = Math.Sin(rad) * Speed;
            IsActive = true;
        }

        public void Update()
        {
            if (!IsActive) return;
            _x += _vx; _y += _vy;
        }

        public void Stop()
        {
            IsActive = false;
            _vx = _vy = 0;
        }

        public void PinTo(Plunger.Point pos)
        {
            _x = pos.X; _y = pos.Y;
            _vx = _vy = 0;
        }

        public Rectangle GetBounds()
            => new Rectangle((int)_x, (int)_y, Size, Size);
    }
}
