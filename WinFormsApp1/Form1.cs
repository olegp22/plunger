// Form1.cs — View layer (MVP)
// Responsibilities: rendering, input event forwarding, IMainView implementation.
// Contains ZERO game logic — all logic lives in GamePresenter.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Plunger.Models;
using Plunger.Presenters;
using Plunger.Views;
using WinFormsApp1.Properties;

namespace WinFormsApp1
{
    public partial class Form1 : Form, IMainView
    {
        // ═════════════════════════════════════════════════════════════════════
        // IMainView — events (View → Presenter)
        // ═════════════════════════════════════════════════════════════════════
        public event Action? TimerTick;
        public event Action<Keys>? KeyDown;
        public event Action<Keys>? KeyUp;

        // ═════════════════════════════════════════════════════════════════════
        // IMainView — HUD data (Presenter writes; OnPaint reads)
        // ═════════════════════════════════════════════════════════════════════
        public int LevelTicksRemaining { get; set; } = GamePresenter.LevelDurationTicks;
        public int TotalCoins { get; set; } = 0;

        // ═════════════════════════════════════════════════════════════════════
        // Rendering state
        // ═════════════════════════════════════════════════════════════════════

        // Game-object references (set by Presenter via SetLevel)
        private Sucker? _player = null;
        private LevelData? _level = null;
        private Camera? _camera = null;
        private int _score = 0;

        // ── Sprite sheet: digger.png — 4 rows × 4 cols ───────────────────────
        private const int SpriteRows = 4, SpriteCols = 4;
        private const int RowRun = 0, RowGrapple = 1, RowSomersault = 2, RowFall = 3;
        private const int RunFrames = 4, GrappleFrames = 4;
        private const int SomersaultFrames = 3, FallFrames = 4;

        // ── Animation state ───────────────────────────────────────────────────
        private int _animRow = RowRun, _animCol = 0, _frameTimer = 0;
        private const int FrameDelay = 5;   // ticks per frame
        private bool _isSomersaulting = false;

        // ── Coin animation ────────────────────────────────────────────────────
        private const int CoinCols = 8;
        private int _coinFrame = 0, _coinTimer = 0;

        // ── Plunger head sprite ───────────────────────────────────────────────
        private const int VantusCols = 2;

        // ── Player render size & center offset ───────────────────────────────
        private const int PW = 90, PH = 110;    // render size
        private const int PCX = PW / 2, PCY = PH / 2;   // center offset

        // ── UI mode ───────────────────────────────────────────────────────────
        private enum ScreenMode { Menu, Game, Death, Victory }
        private ScreenMode _screen = ScreenMode.Menu;
        private bool _showMenu => _screen == ScreenMode.Menu;
        private int _menuHover = -1;   // -1=none, 0=lvl1, 1=lvl2

        // Данные для экрана победы
        private int _victoryCoins = 0;
        private int _victoryTotal = 0;

        // Анимация оверлея: 0..1 alpha для fade-in
        private float _overlayAlpha = 0f;
        private const float FadeSpeed = 0.035f;

        // Presenter (assigned in Form1_Load before any paint)
        private GamePresenter _presenter = null!;

        // ═════════════════════════════════════════════════════════════════════
        // Construction
        // ═════════════════════════════════════════════════════════════════════
        public Form1()
        {
            InitializeComponent();

            // Explicit delegate casts → suppress CS8622 nullable-sender warnings
            base.KeyDown += new KeyEventHandler(HandleKeyDown);
            base.KeyUp += new KeyEventHandler(HandleKeyUp);
            MouseMove += new MouseEventHandler(HandleMouseMove);
            MouseClick += new MouseEventHandler(HandleMouseClick);

            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
        }

        // Form1_Load wired by Designer (new EventHandler(Form1_Load))
        private void Form1_Load(object sender, EventArgs e)
        {
            _presenter = new GamePresenter(this);
        }

        // gameTimer_Tick wired by Designer (new EventHandler(gameTimer_Tick))
        private void gameTimer_Tick(object sender, EventArgs e)
            => TimerTick?.Invoke();

        // ═════════════════════════════════════════════════════════════════════
        // Input — forward raw keys to Presenter via IMainView events
        // ═════════════════════════════════════════════════════════════════════
        private void HandleKeyDown(object? sender, KeyEventArgs e)
            => KeyDown?.Invoke(e.KeyCode);

        private void HandleKeyUp(object? sender, KeyEventArgs e)
            => KeyUp?.Invoke(e.KeyCode);

        private void HandleMouseMove(object? sender, MouseEventArgs e)
        {
            if (!_showMenu) return;
            _menuHover = MenuHitTest(e.Location);
            Invalidate();
        }

        private void HandleMouseClick(object? sender, MouseEventArgs e)
        {
            if (!_showMenu) return;
            int hit = MenuHitTest(e.Location);
            if (hit == 0) _presenter.StartLevel(1);
            else if (hit == 1) _presenter.StartLevel(2);
        }

        private int MenuHitTest(Point pt)
        {
            int cx = ClientSize.Width / 2;
            if (Rect(cx - 150, 245, 300, 65).Contains(pt)) return 0;
            if (Rect(cx - 150, 335, 300, 65).Contains(pt)) return 1;
            return -1;
        }

        // ═════════════════════════════════════════════════════════════════════
        // IMainView implementation
        // ═════════════════════════════════════════════════════════════════════
        public void SetLevel(Sucker player, LevelData level, Camera camera)
        {
            _player = player;
            _level = level;
            _camera = camera;
            _score = 0;
            // Reset animation
            _animRow = RowRun; _animCol = 0; _frameTimer = 0;
            _isSomersaulting = false;
        }

        public void ShowMenu()
        {
            _screen = ScreenMode.Menu;
            _overlayAlpha = 0f;
            LevelTicksRemaining = GamePresenter.LevelDurationTicks;
            gameTimer.Start();
            Invalidate();
        }

        public void ShowGame()
        {
            _screen = ScreenMode.Game;
            _overlayAlpha = 0f;
            gameTimer.Start();
            Invalidate();
        }

        public void ShowDeath()
        {
            _screen = ScreenMode.Death;
            _overlayAlpha = 0f;
            Invalidate();
        }

        public void ShowVictory(int coins, int total)
        {
            _screen = ScreenMode.Victory;
            _victoryCoins = coins;
            _victoryTotal = total;
            _overlayAlpha = 0f;
            Invalidate();
        }

        public void UpdateScore(int score)
        {
            _score = score;
            Text = $"Plunger Dash — {_score} coins";
        }

        public void RefreshView() => Invalidate();

        // ═════════════════════════════════════════════════════════════════════
        // Paint dispatcher
        // ═════════════════════════════════════════════════════════════════════
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;

            if (_screen == ScreenMode.Menu) { DrawMenu(g); return; }
            if (_screen == ScreenMode.Death) { DrawDeath(g); return; }
            if (_screen == ScreenMode.Victory) { DrawVictory(g); return; }

            // ── Background ────────────────────────────────────────────────────
            if (Resources.background != null)
                g.DrawImage(Resources.background,
                    new Rectangle(0, 0, ClientSize.Width, ClientSize.Height));
            else
                g.Clear(Color.FromArgb(18, 14, 34));

            if (_player == null || _level == null || _camera == null) return;

            int scrollX = _camera.ScrollX;

            // ── Update animation state (read-only from model) ─────────────────
            StepAnimation();

            // ── Render world elements, all offset by -scrollX ─────────────────
            DrawTiles(g, scrollX);
            DrawCoins(g, scrollX);
            DrawRope(g, scrollX);
            DrawAimLaser(g, scrollX);
            DrawPlayer(g, scrollX);
            DrawHUD(g);
        }

        // ═════════════════════════════════════════════════════════════════════
        // MENU
        // ═════════════════════════════════════════════════════════════════════
        private void DrawMenu(Graphics g)
        {
            int w = ClientSize.Width, h = ClientSize.Height, cx = w / 2;

            // Dark gradient background
            using (var bg = new LinearGradientBrush(
                    new Point(0, 0), new Point(0, h),
                    Color.FromArgb(10, 8, 25), Color.FromArgb(28, 16, 55)))
                g.FillRectangle(bg, 0, 0, w, h);

            // Stars
            var rng = new Random(42);
            for (int i = 0; i < 90; i++)
            {
                float sz = (float)(rng.NextDouble() * 2.5 + 0.4);
                using var sb = new SolidBrush(Color.FromArgb(rng.Next(80, 255), 210, 215, 255));
                g.FillEllipse(sb, rng.Next(w) - sz / 2, rng.Next(h) - sz / 2, sz, sz);
            }

            // Title
            using (var tf = new Font("Impact", 54, FontStyle.Regular, GraphicsUnit.Pixel))
            {
                const string title = "PLUNGER DASH";
                var tsz = g.MeasureString(title, tf);
                float tx = cx - tsz.Width / 2;
                // Drop shadow
                using (var shadow = new SolidBrush(Color.FromArgb(100, 255, 140, 0)))
                    g.DrawString(title, tf, shadow, tx + 3, 88 + 3);
                // Gradient fill
                using (var grad = new LinearGradientBrush(
                        new Point(cx - 260, 88), new Point(cx + 260, 148),
                        Color.FromArgb(255, 255, 230, 80),
                        Color.FromArgb(255, 255, 130, 0)))
                    g.DrawString(title, tf, grad, tx, 88);
            }

            DrawCentred(g, "Grapple  •  Swing  •  Collect",
                new Font("Segoe UI", 15, FontStyle.Italic, GraphicsUnit.Pixel),
                Color.FromArgb(170, 200, 205, 255), cx, 158);

            DrawCentred(g, "[W] Shoot hook     [A / D] Aim     [Space] Detach     [Esc] Menu",
                new Font("Segoe UI", 13, FontStyle.Regular, GraphicsUnit.Pixel),
                Color.FromArgb(130, 170, 255, 170), cx, 195);

            DrawMenuButton(g, cx, 245, "LEVEL 1 — The Run",
                "Ceiling & floor grapple  |  coin sprint", _menuHover == 0);
            DrawMenuButton(g, cx, 335, "LEVEL 2 — Wall Climb",
                "Walls, platforms & vertical climbing", _menuHover == 1);

            DrawCentred(g, "Click a level to begin",
                new Font("Segoe UI", 11, FontStyle.Regular, GraphicsUnit.Pixel),
                Color.FromArgb(70, 170, 170, 170), cx, h - 30);
        }

        private static void DrawCentred(Graphics g, string text, Font font, Color color, int cx, int y)
        {
            using var brush = new SolidBrush(color);
            var sz = g.MeasureString(text, font);
            g.DrawString(text, font, brush, cx - sz.Width / 2, y);
            font.Dispose();
        }

        private void DrawMenuButton(Graphics g, int cx, int y, string label, string sub, bool hovered)
        {
            const int bw = 310, bh = 66;
            int bx = cx - bw / 2;

            var bgCol = hovered ? Color.FromArgb(215, 45, 32, 85) : Color.FromArgb(150, 22, 14, 52);
            var brdCol = hovered ? Color.FromArgb(255, 255, 185, 0) : Color.FromArgb(170, 110, 85, 210);

            using (var bb = new SolidBrush(bgCol)) g.FillRoundedRect(bb, bx, y, bw, bh, 13);
            using (var bp = new Pen(brdCol, hovered ? 2.5f : 1.5f)) g.DrawRoundedRect(bp, bx, y, bw, bh, 13);

            using (var lf = new Font("Impact", 21, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var lb = new SolidBrush(hovered ? Color.FromArgb(255, 255, 235, 80) : Color.White))
            {
                var ls = g.MeasureString(label, lf);
                g.DrawString(label, lf, lb, cx - ls.Width / 2, y + 7);
            }
            using (var sf = new Font("Segoe UI", 12, FontStyle.Italic, GraphicsUnit.Pixel))
            using (var sb2 = new SolidBrush(Color.FromArgb(hovered ? 215 : 130, 180, 205, 255)))
            {
                var ss = g.MeasureString(sub, sf);
                g.DrawString(sub, sf, sb2, cx - ss.Width / 2, y + 37);
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // ANIMATION — purely presentation logic, reads Sucker state
        // ═════════════════════════════════════════════════════════════════════
        private void StepAnimation()
        {
            if (_player == null) return;

            // Somersault trigger from model
            if (_player.SomersaultThisFrame)
            {
                _player.ConsumeSomersault();
                _isSomersaulting = true;
                _animRow = RowSomersault; _animCol = 0; _frameTimer = 0;
            }

            if (_isSomersaulting)
            {
                // Finish somersault when last frame fully played
                if (_animCol >= SomersaultFrames - 1 && _frameTimer >= FrameDelay)
                    _isSomersaulting = false;
                // Still advance timer below
            }
            else
            {
                // Map physics state → animation row
                switch (_player.Condition)
                {
                    case Condition.Run:
                        if (_animRow != RowRun)
                        { _animRow = RowRun; _animCol = 0; _frameTimer = 0; }
                        break;

                    case Condition.Attached:
                        if (_animRow != RowGrapple)
                        { _animRow = RowGrapple; _animCol = 0; _frameTimer = 0; }
                        // Grapple col driven by aim angle — not looping
                        _animCol = AimToGrappleCol(_player.AimAngle);
                        break;

                    case Condition.Fall:
                        if (_animRow != RowFall)
                        { _animRow = RowFall; _animCol = 0; _frameTimer = 0; }
                        break;
                }
            }

            // Advance frame timer for looping rows
            _frameTimer++;
            if (_frameTimer > FrameDelay)
            {
                _frameTimer = 0;
                if (_animRow != RowGrapple)  // grapple col is angle-driven
                {
                    int maxF = _animRow switch
                    {
                        RowRun => RunFrames,
                        RowSomersault => SomersaultFrames,
                        RowFall => FallFrames,
                        _ => 1
                    };
                    _animCol = (_animCol + 1) % maxF;
                }
            }

            // Coin animation (independent)
            _coinTimer++;
            if (_coinTimer > 4) { _coinFrame = (_coinFrame + 1) % CoinCols; _coinTimer = 0; }
        }

        private static int AimToGrappleCol(double angle)
        {
            // angle: -90 = up, 0 = forward, +85 = down
            // Map to 4 frames: 0=up, 1=45° up-forward, 2=forward, 3=downward
            if (angle <= -65) return 0;
            if (angle <= -25) return 1;
            if (angle <= 25) return 2;
            return 3;
        }

        // ═════════════════════════════════════════════════════════════════════
        // GAME RENDERING — all world coordinates offset by -scrollX
        // ═════════════════════════════════════════════════════════════════════

        private void DrawTiles(Graphics g, int scrollX)
        {
            if (_level == null) return;

            foreach (var tile in _level.Tiles)
            {
                var wb = tile.GetBounds();
                // Screen-space rectangle
                var sr = new Rectangle(wb.Left - scrollX, wb.Top, wb.Width, wb.Height);

                // Cull tiles outside the viewport
                if (sr.Right < 0 || sr.Left > ClientSize.Width) continue;

                switch (tile.Type)
                {
                    case TileType.Ground:
                    case TileType.Floor:
                        DrawGroundTile(g, sr);
                        break;
                    case TileType.Ceiling:
                        DrawCeilingTile(g, sr);
                        break;
                    case TileType.Wall:
                        DrawWallTile(g, sr);
                        break;
                    case TileType.Platform:
                        DrawPlatformTile(g, sr);
                        break;
                        // TileType.Platform: levels no longer use platforms; kept in model only
                }
            }
        }

        private static void DrawGroundTile(Graphics g, Rectangle sr)
        {
            using (var b = new LinearGradientBrush(
                    new Point(sr.X, sr.Y), new Point(sr.X, sr.Y + sr.Height),
                    Color.FromArgb(255, 80, 60, 30), Color.FromArgb(255, 45, 30, 10)))
                g.FillRectangle(b, sr.X, sr.Y, sr.Width, sr.Height);
            // Grass top strip
            using (var gp = new SolidBrush(Color.FromArgb(200, 70, 120, 40)))
                g.FillRectangle(gp, sr.X, sr.Y, sr.Width, 5);
            // Brick grid
            using (var bp = new Pen(Color.FromArgb(50, 0, 0, 0), 1))
                for (int bx = sr.X; bx < sr.Right; bx += 32)
                    g.DrawLine(bp, bx, sr.Y, bx, sr.Bottom);
            using (var border = new Pen(Color.FromArgb(160, 100, 75, 30), 1.5f))
                g.DrawRectangle(border, sr.X, sr.Y, sr.Width, sr.Height);
        }

        private static void DrawCeilingTile(Graphics g, Rectangle sr)
        {
            using (var b = new LinearGradientBrush(
                    new Point(sr.X, sr.Y), new Point(sr.X, sr.Y + sr.Height),
                    Color.FromArgb(255, 35, 25, 55), Color.FromArgb(255, 55, 40, 80)))
                g.FillRectangle(b, sr.X, sr.Y, sr.Width, sr.Height);
            // Stone bottom strip
            using (var sp = new SolidBrush(Color.FromArgb(160, 80, 65, 100)))
                g.FillRectangle(sp, sr.X, sr.Bottom - 5, sr.Width, 5);
            using (var bp = new Pen(Color.FromArgb(40, 255, 255, 255), 1))
                for (int bx = sr.X; bx < sr.Right; bx += 48)
                    g.DrawLine(bp, bx, sr.Y, bx, sr.Bottom);
        }

        private static void DrawWallTile(Graphics g, Rectangle sr)
        {
            using (var b = new LinearGradientBrush(
                    new Point(sr.X, sr.Y), new Point(sr.X + sr.Width, sr.Y),
                    Color.FromArgb(230, 60, 45, 80), Color.FromArgb(230, 40, 28, 58)))
                g.FillRectangle(b, sr.X, sr.Y, sr.Width, sr.Height);
            using (var hl = new Pen(Color.FromArgb(100, 150, 120, 200), 2))
                g.DrawLine(hl, sr.X + 1, sr.Y, sr.X + 1, sr.Bottom);
            // Horizontal mortar lines
            using (var mp = new Pen(Color.FromArgb(50, 0, 0, 0), 1))
                for (int by = sr.Y; by < sr.Bottom; by += 20)
                    g.DrawLine(mp, sr.X, by, sr.Right, by);
            using (var border = new Pen(Color.FromArgb(140, 90, 70, 120), 1.5f))
                g.DrawRectangle(border, sr.X, sr.Y, sr.Width, sr.Height);
        }

        private static void DrawPlatformTile(Graphics g, Rectangle sr)
        {
            using (var b = new LinearGradientBrush(
                    new Point(sr.X, sr.Y), new Point(sr.X, sr.Y + sr.Height),
                    Color.FromArgb(220, 95, 72, 38), Color.FromArgb(220, 58, 40, 16)))
                g.FillRectangle(b, sr.X, sr.Y, sr.Width, sr.Height);
            // Highlight top edge
            using (var hl = new Pen(Color.FromArgb(170, 210, 170, 80), 3))
                g.DrawLine(hl, sr.X, sr.Y + 1, sr.Right, sr.Y + 1);
            // Brick marks
            using (var bp = new Pen(Color.FromArgb(55, 0, 0, 0), 1))
                for (int bx = sr.X + 20; bx < sr.Right; bx += 20)
                    g.DrawLine(bp, bx, sr.Y, bx, sr.Bottom);
            using (var border = new Pen(Color.FromArgb(170, 125, 95, 42), 1.5f))
                g.DrawRectangle(border, sr.X, sr.Y, sr.Width, sr.Height);
        }

        private void DrawCoins(Graphics g, int scrollX)
        {
            if (_level == null) return;

            foreach (var coin in _level.Coins)
            {
                if (coin.IsCollected) continue;
                var wb = coin.GetBounds();
                int sx = wb.X - scrollX;
                if (sx + wb.Width < 0 || sx > ClientSize.Width) continue;

                var sr = new Rectangle(sx, wb.Y, wb.Width, wb.Height);

                if (Resources.coin != null)
                {
                    int cw = Resources.coin.Width / CoinCols;
                    g.DrawImage(Resources.coin,
                        sr,
                        new Rectangle(_coinFrame * cw, 0, cw, Resources.coin.Height),
                        GraphicsUnit.Pixel);
                }
                else
                {
                    using var cb = new SolidBrush(Color.Gold);
                    g.FillEllipse(cb, sr.X, sr.Y, sr.Width, sr.Height);
                    using var co = new Pen(Color.FromArgb(180, 200, 150, 0), 1);
                    g.DrawEllipse(co, sr.X, sr.Y, sr.Width, sr.Height);
                }
            }
        }

        private void DrawRope(Graphics g, int scrollX)
        {
            if (_player == null) return;
            bool ropeVisible = _player.Projectile.IsActive
                            || _player.Condition == Condition.Attached;
            if (!ropeVisible) return;

            // Screen-space endpoints
            int pcx = _player.Location.X - scrollX + PCX;
            int pcy = _player.Location.Y + PCY;
            int hx = _player.Projectile.Location.X - scrollX;
            int hy = _player.Projectile.Location.Y;

            // Rope line with slight thickness
            using (var rope = new Pen(Color.FromArgb(210, 140, 90, 40), 3f))
            {
                rope.StartCap = LineCap.Round;
                rope.EndCap = LineCap.Round;
                g.DrawLine(rope, pcx, pcy, hx, hy);
            }

            // Plunger head
            if (Resources.vantus != null)
            {
                int vw = Resources.vantus.Width / VantusCols;
                int vf = (_player.Condition == Condition.Attached) ? 0 : 1;
                g.DrawImage(Resources.vantus,
                    new Rectangle(hx - 18, hy - 18, 36, 36),
                    new Rectangle(vf * vw, 0, vw, Resources.vantus.Height),
                    GraphicsUnit.Pixel);
            }
            else
            {
                using var hb = new SolidBrush(Color.OrangeRed);
                g.FillEllipse(hb, hx - 9, hy - 9, 18, 18);
                using var ho = new Pen(Color.FromArgb(200, 255, 120, 0), 1.5f);
                g.DrawEllipse(ho, hx - 9, hy - 9, 18, 18);
            }
        }

        private void DrawAimLaser(Graphics g, int scrollX)
        {
            if (_player == null) return;
            if (_player.Projectile.IsActive || _player.Condition == Condition.Attached) return;

            // Player center in screen space
            float sx = _player.Location.X - scrollX + PCX;
            float sy = _player.Location.Y + PCY;

            double rad = _player.AimAngle * (Math.PI / 180.0);
            float dx = (float)Math.Cos(rad);
            float dy = (float)Math.Sin(rad);

            // Trace ray to world bounds — ceiling / floor in world Y
            float ceilY = LevelBuilder.CeilBot;
            float floorY = LevelBuilder.FloorTop;

            // Find intersection with horizontal bounds
            float maxLen = 1600f;
            float tx = sx + dx * maxLen;
            float ty = sy + dy * maxLen;

            // Clamp to ceiling (dy < 0 means going up)
            if (dy < 0 && ty < ceilY)
            {
                float t = (ceilY - sy) / dy;
                tx = sx + dx * t; ty = ceilY;
            }
            // Clamp to floor (dy > 0 means going down)
            else if (dy > 0 && ty > floorY && _player.CanGrappleGround)
            {
                float t = (floorY - sy) / dy;
                tx = sx + dx * t; ty = floorY;
            }

            // Dashed laser beam
            using (var lp = new Pen(Color.FromArgb(180, 100, 255, 100), 1.8f)
            { DashStyle = DashStyle.Dash })
                g.DrawLine(lp, sx, sy, tx, ty);

            // Arrow indicator (small, near player)
            float al = 20f;
            float aa = (float)(Math.PI / 5.8);
            float bax = (float)rad;
            var tip = new PointF(sx + dx * 72, sy + dy * 72);
            var lpt = new PointF(tip.X - al * (float)Math.Cos(bax - aa),
                                 tip.Y - al * (float)Math.Sin(bax - aa));
            var rpt = new PointF(tip.X - al * (float)Math.Cos(bax + aa),
                                 tip.Y - al * (float)Math.Sin(bax + aa));
            using (var ab = new SolidBrush(Color.FromArgb(190, 255, 220, 50)))
                g.FillPolygon(ab, new[] { tip, lpt, rpt });

            // Target indicator at hit point
            bool hitsHorizontal = (ty <= ceilY + 3f) || (ty >= floorY - 3f);
            if (hitsHorizontal)
            {
                const float r = 11f;
                using (var glow = new Pen(Color.FromArgb(90, 255, 175, 0), 5f))
                    g.DrawEllipse(glow, tx - r - 3, ty - r - 3, (r + 3) * 2, (r + 3) * 2);
                using (var fill = new SolidBrush(Color.FromArgb(190, 255, 220, 50)))
                    g.FillEllipse(fill, tx - r, ty - r, r * 2, r * 2);
                using (var cp = new Pen(Color.FromArgb(150, 80, 48, 0), 1.5f))
                {
                    g.DrawLine(cp, tx - r, ty, tx + r, ty);
                    g.DrawLine(cp, tx, ty - r, tx, ty + r);
                }
            }
        }

        private void DrawPlayer(Graphics g, int scrollX)
        {
            if (_player == null) return;

            int sx = _player.Location.X - scrollX;
            int sy = _player.Location.Y;

            if (Resources.digger == null)
            {
                // Fallback: colored rectangle
                using var fb = new SolidBrush(_player.IsDead ? Color.Red : Color.DodgerBlue);
                g.FillRectangle(fb, sx, sy, PW, PH);
                return;
            }

            int dw = Resources.digger.Width / SpriteCols;
            int dh = Resources.digger.Height / SpriteRows;

            int maxCol = _animRow switch
            {
                RowRun => RunFrames - 1,
                RowGrapple => GrappleFrames - 1,
                RowSomersault => SomersaultFrames - 1,
                RowFall => FallFrames - 1,
                _ => 0
            };
            int col = Math.Min(_animCol, maxCol);

            g.DrawImage(Resources.digger,
                new Rectangle(sx, sy, PW, PH),
                new Rectangle(col * dw, _animRow * dh, dw, dh),
                GraphicsUnit.Pixel);
        }

        private void DrawHUD(Graphics g)
        {
            int w = ClientSize.Width;
            int barW = w - 40, barH = 16, barX = 20, barY = 5;

            float prog = TotalCoins > 0
                ? Math.Min(1f, (float)_score / TotalCoins) : 0f;

            // Track
            using (var tr = new SolidBrush(Color.FromArgb(110, 0, 0, 0)))
                g.FillRectangle(tr, barX, barY, barW, barH);

            // Fill
            if (prog > 0)
            {
                using var fill = new LinearGradientBrush(
                    new Point(barX, barY), new Point(barX + barW, barY),
                    Color.FromArgb(220, 255, 205, 0),
                    Color.FromArgb(220, 255, 82, 0));
                g.FillRectangle(fill, barX, barY, (int)(barW * prog), barH);
            }

            // Border
            using (var bp = new Pen(Color.FromArgb(170, 255, 205, 82), 1.5f))
                g.DrawRectangle(bp, barX, barY, barW, barH);

            // Coin label
            using (var hf = new Font("Segoe UI", 13, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var hb = new SolidBrush(Color.FromArgb(225, 255, 235, 82)))
                g.DrawString($"  {_score} / {TotalCoins}", hf, hb, barX + 4, barY + barH + 2);

            // Timer
            int secs = Math.Max(0, LevelTicksRemaining) / 60;
            using (var tf2 = new Font("Consolas", 13, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var tb2 = new SolidBrush(secs <= 10 ? Color.OrangeRed
                                                        : Color.FromArgb(200, 200, 255, 200)))
            {
                string ts = $"{secs}s";
                var tsz = g.MeasureString(ts, tf2);
                g.DrawString(ts, tf2, tb2, w - tsz.Width - 12, barY + barH + 2);
            }

            // Controls reminder (small, bottom-left)
            using (var cf = new Font("Segoe UI", 11, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var cb2 = new SolidBrush(Color.FromArgb(80, 200, 200, 200)))
                g.DrawString("[W] Hook   [A/D] Aim   [Space] Detach   [Esc] Menu",
                    cf, cb2, 10, ClientSize.Height - 22);
        }

        // ═════════════════════════════════════════════════════════════════════
        // DEATH SCREEN
        // ═════════════════════════════════════════════════════════════════════
        private void DrawDeath(Graphics g)
        {
            int w = ClientSize.Width, h = ClientSize.Height, cx = w / 2, cy2 = h / 2;

            // Прокачиваем fade-in
            _overlayAlpha = Math.Min(1f, _overlayAlpha + FadeSpeed);

            // Тёмный красноватый фон
            using (var bg = new SolidBrush(Color.FromArgb((int)(_overlayAlpha * 200), 60, 0, 0)))
                g.FillRectangle(bg, 0, 0, w, h);

            if (_overlayAlpha < 0.3f) { Invalidate(); return; }

            float a = Math.Min(1f, (_overlayAlpha - 0.3f) / 0.7f);
            int ia = (int)(a * 255);

            // Большая надпись "ВЫ ПРОИГРАЛИ"
            using (var tf = new Font("Impact", 80, FontStyle.Regular, GraphicsUnit.Pixel))
            {
                string txt = "ВЫ ПРОИГРАЛИ";
                var sz = g.MeasureString(txt, tf);
                float tx = cx - sz.Width / 2;
                float ty = cy2 - sz.Height / 2 - 40;

                // Красная тень
                using (var sh = new SolidBrush(Color.FromArgb(ia, 180, 0, 0)))
                    g.DrawString(txt, tf, sh, tx + 4, ty + 4);
                // Белый текст
                using (var tb2 = new SolidBrush(Color.FromArgb(ia, 255, 220, 220)))
                    g.DrawString(txt, tf, tb2, tx, ty);
            }

            // Подсказка снизу
            if (a > 0.6f)
            {
                int ia2 = (int)(Math.Min(1f, (a - 0.6f) / 0.4f) * 220);
                using (var sf = new Font("Segoe UI", 20, FontStyle.Regular, GraphicsUnit.Pixel))
                using (var sb2 = new SolidBrush(Color.FromArgb(ia2, 200, 200, 200)))
                {
                    string hint = "Нажмите ESC для выхода в меню";
                    var sz2 = g.MeasureString(hint, sf);
                    g.DrawString(hint, sf, sb2, cx - sz2.Width / 2, cy2 + 60);
                }
            }

            if (_overlayAlpha < 1f) Invalidate();  // продолжаем fade-in
        }

        // ═════════════════════════════════════════════════════════════════════
        // VICTORY SCREEN
        // ═════════════════════════════════════════════════════════════════════
        private void DrawVictory(Graphics g)
        {
            int w = ClientSize.Width, h = ClientSize.Height, cx = w / 2, cy2 = h / 2;

            _overlayAlpha = Math.Min(1f, _overlayAlpha + FadeSpeed);

            // Тёмный золотистый фон
            using (var bg = new SolidBrush(Color.FromArgb((int)(_overlayAlpha * 210), 30, 25, 0)))
                g.FillRectangle(bg, 0, 0, w, h);

            if (_overlayAlpha < 0.25f) { Invalidate(); return; }

            float a = Math.Min(1f, (_overlayAlpha - 0.25f) / 0.75f);
            int ia = (int)(a * 255);

            // Звёздочки-конфетти (статичные но красивые)
            var rng = new Random(77);
            for (int i = 0; i < 60; i++)
            {
                float sx = rng.Next(w), sy = rng.Next(h);
                float sz = (float)(rng.NextDouble() * 6 + 2);
                int ca = (int)(a * rng.Next(80, 200));
                var col = Color.FromArgb(ca,
                    rng.Next(180, 255), rng.Next(140, 220), rng.Next(0, 80));
                using var cb2 = new SolidBrush(col);
                g.FillEllipse(cb2, sx - sz / 2, sy - sz / 2, sz, sz);
            }

            // "ВЫ ВЫИГРАЛИ!"
            using (var tf = new Font("Impact", 80, FontStyle.Regular, GraphicsUnit.Pixel))
            {
                string txt = "ВЫ ВЫИГРАЛИ!";
                var sz = g.MeasureString(txt, tf);
                float tx = cx - sz.Width / 2;
                float ty = cy2 - sz.Height / 2 - 70;

                using (var sh = new SolidBrush(Color.FromArgb(ia, 120, 90, 0)))
                    g.DrawString(txt, tf, sh, tx + 4, ty + 4);
                using (var grad = new LinearGradientBrush(
                        new Point((int)tx, (int)ty),
                        new Point((int)tx, (int)(ty + sz.Height)),
                        Color.FromArgb(ia, 255, 235, 80),
                        Color.FromArgb(ia, 255, 150, 0)))
                    g.DrawString(txt, tf, grad, tx, ty);
            }

            // Счёт монет
            if (a > 0.4f)
            {
                int ia2 = (int)(Math.Min(1f, (a - 0.4f) / 0.6f) * 255);

                // Рейтинг звёздами
                string rating = GetRating(_victoryCoins, _victoryTotal);
                using (var rf = new Font("Impact", 36, FontStyle.Regular, GraphicsUnit.Pixel))
                using (var rb = new SolidBrush(Color.FromArgb(ia2, 255, 220, 60)))
                {
                    var rsz = g.MeasureString(rating, rf);
                    g.DrawString(rating, rf, rb, cx - rsz.Width / 2, cy2 - 10);
                }

                // Монеты
                string coinStr = $"Монет собрано: {_victoryCoins} / {_victoryTotal}";
                using (var cf2 = new Font("Segoe UI", 22, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var cb3 = new SolidBrush(Color.FromArgb(ia2, 255, 240, 160)))
                {
                    var csz = g.MeasureString(coinStr, cf2);
                    g.DrawString(coinStr, cf2, cb3, cx - csz.Width / 2, cy2 + 45);
                }

                // Процент
                float pct = _victoryTotal > 0 ? (float)_victoryCoins / _victoryTotal * 100f : 0;
                string pctStr = $"{pct:F0}% завершения";
                using (var pf = new Font("Segoe UI", 18, FontStyle.Italic, GraphicsUnit.Pixel))
                using (var pb2 = new SolidBrush(Color.FromArgb(ia2, 200, 220, 180)))
                {
                    var psz = g.MeasureString(pctStr, pf);
                    g.DrawString(pctStr, pf, pb2, cx - psz.Width / 2, cy2 + 80);
                }

                // Подсказка
                if (a > 0.7f)
                {
                    int ia3 = (int)(Math.Min(1f, (a - 0.7f) / 0.3f) * 180);
                    using (var hf = new Font("Segoe UI", 17, FontStyle.Regular, GraphicsUnit.Pixel))
                    using (var hb = new SolidBrush(Color.FromArgb(ia3, 180, 180, 180)))
                    {
                        string hint = "ESC — в главное меню";
                        var hsz = g.MeasureString(hint, hf);
                        g.DrawString(hint, hf, hb, cx - hsz.Width / 2, cy2 + 125);
                    }
                }
            }

            if (_overlayAlpha < 1f) Invalidate();
        }

        private static string GetRating(int coins, int total)
        {
            if (total == 0) return "★★★";
            float pct = (float)coins / total;
            if (pct >= 0.9f) return "★★★  ОТЛИЧНО!";
            if (pct >= 0.6f) return "★★☆  ХОРОШО";
            if (pct >= 0.3f) return "★☆☆  НЕПЛОХО";
            return "☆☆☆  ПОПРОБУЙ ЕЩЁ";
        }

        // ═════════════════════════════════════════════════════════════════════
        // Graphics helpers
        // ═════════════════════════════════════════════════════════════════════
        private static Rectangle Rect(int x, int y, int w, int h)
            => new Rectangle(x, y, w, h);

        private static bool Contains(Rectangle r, Point p)
            => p.X >= r.X && p.X <= r.X + r.Width && p.Y >= r.Y && p.Y <= r.Y + r.Height;
    }

    // ── Rounded-rectangle extension methods ───────────────────────────────────
    internal static class GfxExt
    {
        public static void FillRoundedRect(this Graphics g, Brush b,
            int x, int y, int w, int h, int r)
        { using var p = RPath(x, y, w, h, r); g.FillPath(b, p); }

        public static void DrawRoundedRect(this Graphics g, Pen pen,
            int x, int y, int w, int h, int r)
        { using var p = RPath(x, y, w, h, r); g.DrawPath(pen, p); }

        private static GraphicsPath RPath(int x, int y, int w, int h, int r)
        {
            var p = new GraphicsPath();
            p.AddArc(x, y, r * 2, r * 2, 180, 90);
            p.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
            p.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
            p.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}