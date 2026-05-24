// models/Sucker.cs
namespace Plunger.Models
{
    using System;
    using Plunger.Models.Common;

    public class Sucker
    {
        // Observer: notify views/presenters about state changes (position, condition, coins, etc.)
        public event Action? StateChanged;

        private void NotifyStateChanged() => StateChanged?.Invoke();

        // ── Public state ──────────────────────────────────────────────────────
        public Plunger.Point Location { get; private set; }
        public Condition Condition { get; private set; }
        public int CoinsCollected { get; private set; }
        public bool SomersaultThisFrame { get; private set; }
        public bool IsDead { get; private set; }
        public PlungerProjectile Projectile { get; private set; }
        public bool CanGrappleGround { get; set; } = true;
        // Enable full grapple mechanic (launching, attaching). Can be disabled for special levels.
        public bool EnableGrapple { get; set; } = true;

        public double RunSpeed { get; set; } = GameConfig.RunSpeedLevel1;

        // ── Aim ───────────────────────────────────────────────────────────────
        public double AimAngle { get; private set; } = -60.0;

        // ── Sprite / collision ────────────────────────────────────────────────
        // Спрайт рисуется SpriteW × SpriteH.
        // Коллизионный бокс уже и короче, прибит к нижней части спрайта.
        //   CollisionOffX = отступ от левого края спрайта
        //   CollisionOffY = отступ от верхнего края спрайта
        public const int SpriteW = 90;
        public const int SpriteH = 110;
        public const int CollisionW = 58;
        public const int CollisionH = 82;
        public const int CollisionOffX = (SpriteW - CollisionW) / 2;  // 16
        public const int CollisionOffY = SpriteH - CollisionH;         // 28

        // ── Physics ───────────────────────────────────────────────────────────
        // Начальная вертикальная скорость прыжка (px за тик). Отрицательное = вверх.
        // Делается присваиваемой свойством чтобы можно было настроить значение для каждого уровня.
        public double JumpVY { get; set; } = GameConfig.JumpVYLevel1;
        // Gravity scale: 1.0 = normal downwards gravity, -1.0 = inverted gravity.
        public double GravityScale { get; private set; } = 1.0;
        // Разрешено ли переключать гравитацию (включается только для уровня 3)
        public bool CanInvertGravity { get; set; } = false;

        // ── Attached: простой таймер ──────────────────────────────────────────
        // AttachMaxTicks — сколько тиков максимум висим.
        // ВАЖНО: счётчик НИКОГДА не сбрасывается пока мы в состоянии Attached.
        // Это гарантирует отцеп даже если снаряд постоянно пересекает тайл.
        private const int AttachMaxTicks = 30;
        private int _attachTicks = 0;
        private bool _justDetached = false;

        private double _vx = 0;
        private double _vy = 0;

        // Флаг: прыжок запрошен в этом тике (до UpdatePosition)
        private bool _jumpRequested = false;
        // Флаг: мы в воздухе из-за прыжка (true пока не приземлились).
        private bool _airborneFromJump = false;

        // ── Constructor ───────────────────────────────────────────────────────
        public Sucker(Plunger.Point location)
        {
            Location = location;
            Condition = Condition.Run;
            Projectile = new PlungerProjectile();
        }

        // ── Input (вызывается из Presenter до UpdatePosition) ─────────────────
        public void AimUp()
            => AimAngle = Math.Max(GameConfig.AimMin, AimAngle - GameConfig.AimStep);
        public void AimDown()
            => AimAngle = Math.Min(GameConfig.AimMax, AimAngle + GameConfig.AimStep);
        public void ConsumeSomersault() => SomersaultThisFrame = false;

        // Прыжок: ставим флаг, реальная физика применяется внутри UpdatePosition
        public void Jump()
        {
            if (Condition == Condition.Run)
            {
                // Request jump: initial vertical velocity will be set in UpdatePosition
                // Jump direction depends on current gravity (away from surface)
                _jumpRequested = true;
                NotifyStateChanged();
            }
        }

        public void Detach()
        {
            if (Condition != Condition.Attached) return;
            ApplyDetachImpulse();
            Condition = Condition.Fall;
            Projectile.Stop();
            SomersaultThisFrame = true;
            _attachTicks = 0;
            NotifyStateChanged();
        }

        // Переключить направление гравитации.
        public void ToggleGravity()
        {
            if (!CanInvertGravity) return;
            GravityScale = -GravityScale;
            // Визуальный эффект — сальто
            SomersaultThisFrame = true;
            // Инвертируем текущую вертикальную скорость so motion continues naturally
            _vy = -_vy;
            // Preserve running horizontal speed
            _vx = RunSpeed;

            // If running on surface, start a quick but non-instant transfer towards the opposite support
            if (Condition == Condition.Run)
            {
                // Enter Fall so physics moves the player toward new support
                Condition = Condition.Fall;
                // Give a strong initial velocity in gravity direction to move quickly (but not teleport)
                _vy = GameConfig.MaxFallSpeed * GravityScale * 0.9; // sign follows GravityScale
                // Preserve horizontal speed during the transit
                _airborneFromJump = true;
            }
            NotifyStateChanged();
        }

        public void Shoot()
        {
            if (!EnableGrapple) return;
            if (Condition == Condition.Attached) { Detach(); return; }
            if (Projectile.IsActive) { Projectile.Stop(); return; }
            var c = new Plunger.Point(Location.X + SpriteW / 2, Location.Y + SpriteH / 2);
            Projectile.Launch(c, AimAngle);
            NotifyStateChanged();
        }

        public void UpdatePosition(LevelData level)
        {
            if (IsDead) return;
            _justDetached = false;

            if (Projectile.IsActive)
            {
                Projectile.Update();

                if (Condition != Condition.Attached)
                    TryAttach(level);

                CheckOutOfRange();
            }

            // 2. Применяем запрошенный прыжок
            bool jumping = false;
            if (_jumpRequested && Condition == Condition.Run)
            {
                _jumpRequested = false;
                // Initial vertical velocity away from the current surface: depends on gravity direction
                _vy = -Math.Sign(GravityScale) * Math.Abs(JumpVY);
                // Preserve running horizontal speed
                _vx = RunSpeed;

                _airborneFromJump = true;
                Condition = Condition.Fall;
                jumping = true;
            }
            _jumpRequested = false;

            
            double dx = 0, dy = 0;

            switch (Condition)
            {
                case Condition.Run:
                    
                    _vx = RunSpeed;
                    _vy = 0;    
                    dx = _vx;
                    break;

                case Condition.Attached:
                    _attachTicks++;

                    if (_attachTicks >= AttachMaxTicks)
                    {
                        // Время вышло — принудительный отцеп (используем Detach чтобы гарантировать somersault)
                        Detach();
                        break;
                    }

                    double toX = Projectile.Location.X - (Location.X + SpriteW / 2.0);
                    double toY = Projectile.Location.Y - (Location.Y + SpriteH / 2.0);
                    double dist = Math.Sqrt(toX * toX + toY * toY);

                    if (dist < 100.0)
                    {
                        // Добрались до точки — отцеп
                        Detach();
                        break;
                    }

                    double spd = Math.Min(GameConfig.PullSpeed, dist);
                    _vx = (toX / dist) * spd;
                    _vy = (toY / dist) * spd;
                    dx = _vx;
                    dy = _vy;
                    break;

                case Condition.Fall:
                    if (!jumping)
                    {
                        // Применяем гравитацию с учётом направления (GravityScale может быть -1)
                        _vy += GameConfig.Gravity * GravityScale;
                        if (GravityScale > 0)
                            _vy = Math.Min(_vy, GameConfig.MaxFallSpeed);
                        else
                            _vy = Math.Max(_vy, -GameConfig.MaxFallSpeed);
                    }
                    dx = _vx;
                    dy = _vy;
                    break;
            }

            // 4. Если только что отцепились — пропускаем sweep этого тика
            if (_justDetached) return;

            var (nx, ny, blockedX, blockedY, landed) =
                SweepResolve(Location.X, Location.Y, dx, dy, level);

            Location = new Plunger.Point(nx, ny);

            if (blockedX) _vx = 0;

            // Гасим вертикальную скорость в направлении удара (учитываем направление гравитации)
            if (blockedY)
            {
                // Если столкнулись в направлении текущей гравитации — это посадка/опора
                bool hitSupport = (GravityScale > 0 && _vy >= 0) || (GravityScale < 0 && _vy <= 0);
                if (hitSupport)
                {
                    // Считаем это посадкой: возвращаем состояние Run и фиксируем горизонтальную скорость
                    if (Condition == Condition.Fall)
                    {
                        Condition = Condition.Run;
                        _vx = RunSpeed;
                        _vy = 0;
                        _airborneFromJump = false;
                    }
                }
                else
                {
                    // Удар в противоположном направлении — просто глушим скорость в этом направлении
                    if (GravityScale > 0 && _vy > 0) _vy = 0;
                    else if (GravityScale < 0 && _vy < 0) _vy = 0;
                }
            }

            int floorTop = LevelBuilder.GetFloorTop(level);
            int ceilBot = LevelBuilder.GetCeilBot(level);
            int clampY = Math.Max(ceilBot, Math.Min(floorTop - CollisionH, ny));
            if (clampY != ny)
            {
                if (ny > floorTop - CollisionH && Condition != Condition.Run)
                { Condition = Condition.Run; _vx = RunSpeed; _vy = 0; }
                Location = new Plunger.Point(nx, clampY);
            }

            if (Location.X < LevelBuilder.WallW)
                Location = new Plunger.Point(LevelBuilder.WallW, Location.Y);
        }

        // ── Принудительный отцеп ──────────────────────────────────────────────
        private void ForceDetach()
        {
            ApplyDetachImpulse();
            Condition = Condition.Fall;
            Projectile.Stop();
            SomersaultThisFrame = true;
            _justDetached = true;
            _attachTicks = 0;
            _airborneFromJump = false;
        }

        private void ApplyDetachImpulse()
        {
            double dx = Projectile.Location.X - (Location.X + SpriteW / 2.0);
            double dy = Projectile.Location.Y - (Location.Y + SpriteH / 2.0);
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist > 1.0)
            {
                _vx = (dx / dist) * GameConfig.PullSpeed * 0.5;
                _vy = (dy / dist) * GameConfig.PullSpeed * 0.5;
            }
            else
            {
                _vx = GameConfig.PullSpeed * 0.3;
                _vy = -GameConfig.PullSpeed * 0.2;
            }
        }

        // ── Sweep-коллизия ────────────────────────────────────────────────────
        private (int nx, int ny, bool blockedX, bool blockedY, bool landed)
            SweepResolve(int ox, int oy, double dx, double dy, LevelData level)
        {
            int cx0 = ox + CollisionOffX;
            int cy0 = oy + CollisionOffY;
            int cx = cx0 + (int)Math.Round(dx);
            int cy = cy0 + (int)Math.Round(dy);

            bool blockedX = false, blockedY = false, landed = false;

            int ceilBot = LevelBuilder.GetCeilBot(level);
            int floorTop = LevelBuilder.GetFloorTop(level);

            var boxY = new Rectangle(cx0, cy, CollisionW, CollisionH);
            foreach (var tile in level.Tiles)
            {
                var tb = tile.GetBounds();
                if (!boxY.IntersectsWith(tb)) continue;
                switch (tile.Type)
                {
                    case TileType.Ground:
                    case TileType.Floor:
                    case TileType.Platform:
                        if (dy > 0 && cy0 + CollisionH <= tb.Top + 12)
                        { cy = tb.Top - CollisionH; blockedY = true; landed = true; }
                        else if (dy < 0 && cy0 >= tb.Bottom - 6)
                        { cy = tb.Bottom; blockedY = true; }
                        break;
                    case TileType.Ceiling:
                        if (dy < 0) { cy = tb.Bottom; blockedY = true; }
                        break;
                }
                boxY = new Rectangle(cx0, cy, CollisionW, CollisionH);
            }
            if (cy < ceilBot) { cy = ceilBot; blockedY = true; }
            if (cy + CollisionH > floorTop)
            { cy = floorTop - CollisionH; blockedY = true; landed = true; }

            var boxX = new Rectangle(cx, cy, CollisionW, CollisionH);
            foreach (var tile in level.Tiles)
            {
                var tb = tile.GetBounds();
                if (!boxX.IntersectsWith(tb)) continue;
                switch (tile.Type)
                {
                    case TileType.Wall:
                        if (dx >= 0 && cx0 + CollisionW <= tb.Left + 10)
                        { cx = tb.Left - CollisionW; blockedX = true; }
                        else if (dx < 0 && cx0 >= tb.Right - 10)
                        { cx = tb.Right; blockedX = true; }
                        break;
                    case TileType.Ground:
                    case TileType.Floor:
                    case TileType.Platform:
                        {
                            int ov = Math.Min(cy + CollisionH, tb.Bottom) - Math.Max(cy, tb.Top);
                            if (ov > 12)
                            {
                                if (dx >= 0 && cx0 + CollisionW <= tb.Left + 10)
                                { cx = tb.Left - CollisionW; blockedX = true; }
                                else if (dx < 0 && cx0 >= tb.Right - 10)
                                { cx = tb.Right; blockedX = true; }
                            }
                            break;
                        }
                }
                boxX = new Rectangle(cx, cy, CollisionW, CollisionH);
            }
            if (cx < LevelBuilder.WallW) { cx = LevelBuilder.WallW; blockedX = true; }

            if (!landed && Condition == Condition.Run)
            {
                bool sup = false;
                if (GravityScale > 0)
                {
                    var feet = new Rectangle(cx + 4, cy + CollisionH, CollisionW - 8, 4);
                    foreach (var tile in level.Tiles)
                    {
                        if (tile.Type == TileType.Wall || tile.Type == TileType.Ceiling) continue;
                        if (feet.IntersectsWith(tile.GetBounds())) { sup = true; break; }
                    }
                    if (cy + CollisionH >= floorTop) sup = true;
                }
                else
                {
                    var head = new Rectangle(cx + 4, cy - 4, CollisionW - 8, 4);
                    foreach (var tile in level.Tiles)
                    {
                        if (tile.Type == TileType.Wall || tile.Type == TileType.Ground) continue;
                        if (head.IntersectsWith(tile.GetBounds())) { sup = true; break; }
                    }
                    if (cy <= ceilBot) sup = true;
                }
                if (!sup) Condition = Condition.Fall;
            }

            return (cx - CollisionOffX, cy - CollisionOffY, blockedX, blockedY, landed);
        }

        // ── Присоска ──────────────────────────────────────────────────────────
        private void TryAttach(LevelData level)
        {
            if (!Projectile.IsActive) return;
            if (!EnableGrapple) return; // disable attaching when grapple is turned off
            var pb = Projectile.GetBounds();

            foreach (var tile in level.Tiles)
            {
                if (tile.Type == TileType.Ground && !CanGrappleGround) continue;
                if (!pb.IntersectsWith(tile.GetBounds())) continue;

                var tb = tile.GetBounds();
                int px = Projectile.Location.X, py = Projectile.Location.Y;
                switch (tile.Type)
                {
                    case TileType.Ceiling: py = tb.Bottom; break;
                    case TileType.Ground:
                    case TileType.Floor:
                    case TileType.Platform: py = tb.Top; break;
                    case TileType.Wall:
                        px = (px < tb.Left + tb.Width / 2) ? tb.Left : tb.Right; break;
                }
                Projectile.PinTo(new Plunger.Point(px, py));

                if (Condition != Condition.Attached)
                {
                    Condition = Condition.Attached;
                    _attachTicks = 0;
                    SomersaultThisFrame = true;
                }
                return;
            }

            int ceil = LevelBuilder.GetCeilBot(level);
            int floor = LevelBuilder.GetFloorTop(level);
            if (Projectile.Location.Y <= ceil)
            {
                Projectile.PinTo(new Plunger.Point(Projectile.Location.X, ceil));
                if (Condition != Condition.Attached)
                { Condition = Condition.Attached; _attachTicks = 0; SomersaultThisFrame = true; }
            }
            else if (CanGrappleGround && Projectile.Location.Y >= floor)
            {
                Projectile.PinTo(new Plunger.Point(Projectile.Location.X, floor));
                if (Condition != Condition.Attached)
                { Condition = Condition.Attached; _attachTicks = 0; SomersaultThisFrame = true; }
            }
        }

        private void CheckOutOfRange()
        {
            double dx = Projectile.Location.X - (Location.X + SpriteW / 2.0);
            double dy = Projectile.Location.Y - (Location.Y + SpriteH / 2.0);
            if (dx * dx + dy * dy > GameConfig.GrappleRange * GameConfig.GrappleRange)
                Projectile.Stop();
        }

        public void Kill() { IsDead = true; }
        public void AddCoin() { CoinsCollected++; NotifyStateChanged(); }
        public Rectangle GetBounds()
            => new Rectangle(Location.X, Location.Y, SpriteW, SpriteH);
        public Rectangle GetCollisionBox()
            => new Rectangle(Location.X + CollisionOffX, Location.Y + CollisionOffY,
                             CollisionW, CollisionH);
    }
}