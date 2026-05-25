
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Plunger.Models;
using Plunger.Presenters;
using Plunger.Views;
using WinFormsApp1.Properties;

namespace WinFormsApp1.Views
{
    public partial class Form1 : Form, IMainView
    {
        private static readonly Random _rng = new Random(42);
        private static readonly StringFormat _centerFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };
        private static readonly Font _fImpact54 = new Font("Impact", 54, FontStyle.Regular, GraphicsUnit.Pixel);
        private static readonly Font _fSegoe15Italic = new Font("Segoe UI", 15, FontStyle.Italic, GraphicsUnit.Pixel);
        private static readonly Font _fSegoe13 = new Font("Segoe UI", 13, FontStyle.Regular, GraphicsUnit.Pixel);
        private static readonly Font _fImpact21 = new Font("Impact", 21, FontStyle.Regular, GraphicsUnit.Pixel);
        private static readonly Font _fSegoe12Italic = new Font("Segoe UI", 12, FontStyle.Italic, GraphicsUnit.Pixel);
        private static readonly Font _fSegoe11 = new Font("Segoe UI", 11, FontStyle.Regular, GraphicsUnit.Pixel);
        private static readonly Font _fSegoe13Bold = new Font("Segoe UI", 13, FontStyle.Bold, GraphicsUnit.Pixel);
        private static readonly Font _fConsolas13 = new Font("Consolas", 13, FontStyle.Bold, GraphicsUnit.Pixel);
        private static readonly Font _fSegoe22 = new Font("Segoe UI", 22, FontStyle.Bold, GraphicsUnit.Pixel);
        private static readonly Font _fImpact80 = new Font("Impact", 80, FontStyle.Regular, GraphicsUnit.Pixel);
        private static readonly Font _fSegoe20 = new Font("Segoe UI", 20, FontStyle.Regular, GraphicsUnit.Pixel);
        private static readonly Font _fImpact36 = new Font("Impact", 36, FontStyle.Regular, GraphicsUnit.Pixel);
        private readonly System.Diagnostics.Stopwatch _sw = new System.Diagnostics.Stopwatch();
        private long _lastUpdateMs = 0;
        private long _lastDrawMs = 0;
        private int _frameCount = 0;
        private int _lastFps = 0;
        private long _fpsTimerStart = 0;
        private static readonly Pen _ropePen = new Pen(Color.FromArgb(210, 140, 90, 40), 3f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
        private static readonly Pen _aimPen = new Pen(Color.FromArgb(180, 100, 255, 100), 1.8f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
        private static readonly SolidBrush _groundBrush = new SolidBrush(Color.FromArgb(255, 80, 60, 30));
        private static readonly SolidBrush _grassBrush = new SolidBrush(Color.FromArgb(200, 70, 120, 40));
        private static readonly Pen _groundBorder = new Pen(Color.FromArgb(160, 100, 75, 30), 1.5f);
        private static readonly SolidBrush _ceilingBrush = new SolidBrush(Color.FromArgb(255, 35, 25, 55));
        private static readonly SolidBrush _ceilingStone = new SolidBrush(Color.FromArgb(160, 80, 65, 100));
        private static readonly Pen _ceilingGrid = new Pen(Color.FromArgb(40, 255, 255, 255), 1);
        private static readonly SolidBrush _wallBrush = new SolidBrush(Color.FromArgb(230, 60, 45, 80));
        private static readonly Pen _wallHigh = new Pen(Color.FromArgb(100, 150, 120, 200), 2);
        private static readonly Pen _platformBorder = new Pen(Color.FromArgb(170, 125, 95, 42), 1.5f);
        private static readonly Pen _gridPen = new Pen(Color.FromArgb(50, 0, 0, 0), 1);
        private readonly (float X, float Y, float Size, Color Col)[] _menuStars;

        public event Action? TimerTick;
        public event Action<Keys>? KeyDown;
        public event Action<Keys>? KeyUp;
        public new event Action<MouseButtons>? MouseClick;

        private int _levelTicksRemaining = GamePresenter.LevelDurationTicks;
        private int _totalCoins = 0;
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int LevelTicksRemaining { get => _levelTicksRemaining; set => _levelTicksRemaining = value; }
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int TotalCoins { get => _totalCoins; set => _totalCoins = value; }

        private Sucker? _player = null;
        private LevelData? _level = null;
        private Camera? _camera = null;
        private int _score = 0;

        private const int SpriteRows = 4, SpriteCols = 4;
        private const int RowRun = 0, RowGrapple = 1, RowSomersault = 2, RowFall = 3;
        private const int RunFrames = 4, GrappleFrames = 4;
        private const int SomersaultFrames = 3, FallFrames = 4;

        private int _animRow = RowRun, _animCol = 0, _frameTimer = 0;
        private const int FrameDelay = 5;
        private bool _isSomersaulting = false;

        private const int CoinCols = 8;
        private int _coinFrame = 0, _coinTimer = 0;

        private const int VantusCols = 2;

        private const int PW = 90, PH = 110;
        private const int PCX = PW / 2, PCY = PH / 2;
        private const int MenuButtonW = 380;
        private const int MenuButtonH = 84;

        private enum ScreenMode { Menu, Game, Death, Victory }
        private ScreenMode _screen = ScreenMode.Menu;
        private bool _showMenu => _screen == ScreenMode.Menu;
        private int _menuHover = -1;

        private int _victoryCoins = 0;
        private int _victoryTotal = 0;

        private float _overlayAlpha = 0f;
        private const float FadeSpeed = 0.035f;

        private GamePresenter _presenter = null!;

        public Form1()
        {
            InitializeComponent();

            base.KeyDown += new KeyEventHandler(HandleKeyDown);
            base.KeyUp += new KeyEventHandler(HandleKeyUp);
            MouseMove += new MouseEventHandler(HandleMouseMove);
            base.MouseClick += new MouseEventHandler(HandleMouseClick);

            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);

            _menuStars = new (float X, float Y, float Size, Color Col)[90];
            for (int i = 0; i < _menuStars.Length; i++)
            {
                float sz = (float)(_rng.NextDouble() * 2.5 + 0.4);
                var col = Color.FromArgb(_rng.Next(80, 255), 210, 215, 255);
                _menuStars[i] = (_rng.Next(0, 800), _rng.Next(0, 600), sz, col);
            }
        }

        private void DrawFlags(Graphics g, int scrollX)
        {
            if (_level == null || _level.Flags == null) return;

            foreach (var f in _level.Flags)
            {
                var sr = new System.Drawing.Rectangle(f.X - scrollX, f.Y, f.Width, f.Height);
                if (sr.Right < 0 || sr.Left > ClientSize.Width) continue;

                using (var pb = new SolidBrush(Color.SaddleBrown))
                    g.FillRectangle(pb, sr.X + 6, sr.Y + 4, 4, sr.Height - 8);
                using (var flagB = new SolidBrush(Color.FromArgb(230, 220, 40)))
                {
                    var p1 = new Point(sr.X + 10, sr.Y + 8);
                    var p2 = new Point(sr.X + sr.Width - 6, sr.Y + sr.Height / 2);
                    var p3 = new Point(sr.X + 10, sr.Y + sr.Height - 8);
                    g.FillPolygon(flagB, new[] { p1, p2, p3 });
                }
                using (var pen = new Pen(Color.FromArgb(200, 120, 80, 0), 1.2f))
                    g.DrawRectangle(pen, sr.X, sr.Y, sr.Width, sr.Height);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _presenter = new GamePresenter(this);
        }

        private void gameTimer_Tick(object sender, EventArgs e)
            => TimerTick?.Invoke();

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
            if (_showMenu)
            {
                int hit = MenuHitTest(e.Location);
                if (hit == 0) _presenter.StartLevel(1);
                else if (hit == 1) _presenter.StartLevel(2);
                else if (hit == 2) _presenter.StartLevel(3);
                return;
            }

            MouseClick?.Invoke(e.Button);
        }

        private int MenuHitTest(Point pt)
        {
            int cx = ClientSize.Width / 2;
            if (Rect(cx - MenuButtonW / 2, 245, MenuButtonW, MenuButtonH).Contains(pt)) return 0;
            if (Rect(cx - MenuButtonW / 2, 335, MenuButtonW, MenuButtonH).Contains(pt)) return 1;
            if (Rect(cx - MenuButtonW / 2, 425, MenuButtonW, MenuButtonH).Contains(pt)) return 2;
            return -1;
        }

        public void SetLevel(Sucker player, LevelData level, Camera camera)
        {
            if (_player != null)
                _player.StateChanged -= Player_StateChanged;

            _player = player;
            _level = level;
            _camera = camera;
            _score = 0;
            if (_player != null)
                _player.StateChanged += Player_StateChanged;

            _animRow = RowRun; _animCol = 0; _frameTimer = 0;
            _isSomersaulting = false;
        }

        private void Player_StateChanged()
        {
            if (_player == null) return;
            _score = _player.CoinsCollected;
            Text = $"Plunger Dash — {_score} coins";
            Invalidate();
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;

            if (_screen == ScreenMode.Menu) { DrawMenu(g); return; }
            if (_screen == ScreenMode.Death) { DrawDeath(g); return; }
            if (_screen == ScreenMode.Victory) { DrawVictory(g); return; }

            if (Resources.background != null)
                g.DrawImage(Resources.background,
                    new Rectangle(0, 0, ClientSize.Width, ClientSize.Height));
            else
                g.Clear(Color.FromArgb(18, 14, 34));

            if (_player == null || _level == null || _camera == null) return;
            int scrollX = _camera.ScrollX;

            if (!_sw.IsRunning) { _sw.Start(); _fpsTimerStart = _sw.ElapsedMilliseconds; }
            long t0 = _sw.ElapsedMilliseconds;

            StepAnimation();

            DrawTiles(g, scrollX);
            DrawFlags(g, scrollX);
            DrawCoins(g, scrollX);
            DrawRope(g, scrollX);
            DrawAimLaser(g, scrollX);
            DrawPlayer(g, scrollX);
            DrawHUD(g);

            long t1 = _sw.ElapsedMilliseconds;
            _lastDrawMs = t1 - t0;
            _frameCount++;
            if (t1 - _fpsTimerStart >= 1000)
            {
                _lastFps = _frameCount;
                _frameCount = 0;
                _fpsTimerStart = t1;
            }
            using (var fb = new SolidBrush(Color.White))
            {
                string info = $"FPS: {_lastFps}  Draw: {_lastDrawMs}ms";
                if (_presenter != null) info += $"  Update: {_presenter.LastUpdateMs}ms";
                g.DrawString(info, _fConsolas13, fb, 8, 8);
            }
        }

        private void DrawMenu(Graphics g)
        {
            int w = ClientSize.Width, h = ClientSize.Height, cx = w / 2;

            using (var bg = new SolidBrush(Color.FromArgb(28, 16, 55)))
                g.FillRectangle(bg, 0, 0, w, h);

            for (int i = 0; i < _menuStars.Length; i++)
            {
                var s = _menuStars[i];
                using var sb = new SolidBrush(s.Col);
                g.FillEllipse(sb, s.X % w - s.Size / 2, s.Y % h - s.Size / 2, s.Size, s.Size);
            }

            var tf = _fImpact54;
            const string title = "PLUNGER DASH";
            using (var shadow = new SolidBrush(Color.FromArgb(100, 255, 140, 0)))
                g.DrawString(title, tf, shadow, cx, 88 + 3, _centerFormat);
            using (var fill = new SolidBrush(Color.FromArgb(255, 255, 230, 80)))
                g.DrawString(title, tf, fill, cx, 88, _centerFormat);
            DrawCentred(g, "Grapple  •  Swing  •  Collect",
                _fSegoe15Italic,
                Color.FromArgb(170, 200, 205, 255), cx, 158);

            DrawCentred(g, "[W] Выстрел крюком     [A/D] Прицел     [Пробел] Отцеп     [Esc] Меню",
                _fSegoe13,
                Color.FromArgb(130, 170, 255, 170), cx, 195);

            DrawMenuButton(g, cx, 245, "УРОВЕНЬ 1 — БЕГ",
                "Цепляйся за потолок и пол  |  собери монеты", _menuHover == 0);
            DrawMenuButton(g, cx, 335, "УРОВЕНЬ 2 — ВЗЛЁТ ПО СТЕНЕ",
                "Стены, платформы и вертикальные подъёмы", _menuHover == 1);
            DrawMenuButton(g, cx, 425, "УРОВЕНЬ 3 — ПЕРЕВОРОТ ГРАВИТАЦИИ",
                "Инверсия гравитации  |  ЛКМ — переключение гравитации", _menuHover == 2);

            DrawCentred(g, "Click a level to begin",
                _fSegoe11,
                Color.FromArgb(70, 170, 170, 170), cx, h - 30);
        }

        private static void DrawCentred(Graphics g, string text, Font font, Color color, int cx, int y)
        {
            using var brush = new SolidBrush(color);
            g.DrawString(text, font, brush, cx, y, _centerFormat);
        }

        private void DrawMenuButton(Graphics g, int cx, int y, string label, string sub, bool hovered)
        {
            int bw = MenuButtonW, bh = MenuButtonH;
            int bx = cx - bw / 2;

            var bgCol = hovered ? Color.FromArgb(215, 45, 32, 85) : Color.FromArgb(150, 22, 14, 52);
            var brdCol = hovered ? Color.FromArgb(255, 255, 185, 0) : Color.FromArgb(170, 110, 85, 210);

            using (var bb = new SolidBrush(bgCol)) g.FillRoundedRect(bb, bx, y, bw, bh, 18);
            using (var bp = new Pen(brdCol, hovered ? 3f : 2f)) g.DrawRoundedRect(bp, bx, y, bw, bh, 18);

            using (var lb = new SolidBrush(hovered ? Color.FromArgb(255, 255, 235, 80) : Color.White))
            {
                g.DrawString(label, _fImpact21, lb, cx, y + 12, _centerFormat);
            }
            using (var sb2 = new SolidBrush(Color.FromArgb(hovered ? 215 : 130, 180, 205, 255)))
            {
                g.DrawString(sub, _fSegoe12Italic, sb2, cx, y + 46, _centerFormat);
            }
        }

        private void StepAnimation()
        {
            if (_player == null) return;

            if (_player.SomersaultThisFrame)
            {
                _player.ConsumeSomersault();
                _isSomersaulting = true;
                _animRow = RowSomersault; _animCol = 0; _frameTimer = 0;
            }

            if (_isSomersaulting)
            {
                if (_animCol >= SomersaultFrames - 1 && _frameTimer >= FrameDelay)
                    _isSomersaulting = false;
            }
            else
            {
                switch (_player.Condition)
                {
                    case Condition.Run:
                        if (_animRow != RowRun)
                        { _animRow = RowRun; _animCol = 0; _frameTimer = 0; }
                        break;

                    case Condition.Attached:
                        if (_animRow != RowGrapple)
                        { _animRow = RowGrapple; _animCol = 0; _frameTimer = 0; }
                        _animCol = AimToGrappleCol(_player.AimAngle);
                        break;

                    case Condition.Fall:
                        if (_animRow != RowFall)
                        { _animRow = RowFall; _animCol = 0; _frameTimer = 0; }
                        break;
                }
            }

            _frameTimer++;
            if (_frameTimer > FrameDelay)
            {
                _frameTimer = 0;
                if (_animRow != RowGrapple)
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

            _coinTimer++;
            if (_coinTimer > 4) { _coinFrame = (_coinFrame + 1) % CoinCols; _coinTimer = 0; }
        }

        private static int AimToGrappleCol(double angle)
        {
            if (angle <= -65) return 0;
            if (angle <= -25) return 1;
            if (angle <= 25) return 2;
            return 3;
        }

        private void DrawTiles(Graphics g, int scrollX)
        {
            if (_level == null) return;

            foreach (var tile in _level.Tiles)
            {
                var wb = tile.GetBounds();
                var sr = new Rectangle(wb.Left - scrollX, wb.Top, wb.Width, wb.Height);

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
                }
            }
        }

        private static void DrawGroundTile(Graphics g, Rectangle sr)
        {
            g.FillRectangle(_groundBrush, sr.X, sr.Y, sr.Width, sr.Height);
            g.FillRectangle(_grassBrush, sr.X, sr.Y, sr.Width, 5);
            for (int bx = sr.X; bx < sr.Right; bx += 32)
                g.DrawLine(_gridPen, bx, sr.Y, bx, sr.Bottom);
            g.DrawRectangle(_groundBorder, sr.X, sr.Y, sr.Width, sr.Height);
        }

        private static void DrawCeilingTile(Graphics g, Rectangle sr)
        {
            g.FillRectangle(_ceilingBrush, sr.X, sr.Y, sr.Width, sr.Height);
            g.FillRectangle(_ceilingStone, sr.X, sr.Bottom - 5, sr.Width, 5);
            for (int bx = sr.X; bx < sr.Right; bx += 48)
                g.DrawLine(_ceilingGrid, bx, sr.Y, bx, sr.Bottom);
        }

        private static void DrawWallTile(Graphics g, Rectangle sr)
        {
            g.FillRectangle(_wallBrush, sr.X, sr.Y, sr.Width, sr.Height);
            g.DrawLine(_wallHigh, sr.X + 1, sr.Y, sr.X + 1, sr.Bottom);
            for (int by = sr.Y; by < sr.Bottom; by += 20)
                g.DrawLine(_gridPen, sr.X, by, sr.Right, by);
            g.DrawRectangle(_platformBorder, sr.X, sr.Y, sr.Width, sr.Height);
        }

        private static void DrawPlatformTile(Graphics g, Rectangle sr)
        {
            g.FillRectangle(_groundBrush, sr.X, sr.Y, sr.Width, sr.Height);
            g.DrawLine(_wallHigh, sr.X, sr.Y + 1, sr.Right, sr.Y + 1);
            for (int bx = sr.X + 20; bx < sr.Right; bx += 20)
                g.DrawLine(_gridPen, bx, sr.Y, bx, sr.Bottom);
            g.DrawRectangle(_platformBorder, sr.X, sr.Y, sr.Width, sr.Height);
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
            if (!_player.EnableGrapple) return;
            bool ropeVisible = _player.Projectile.IsActive
                            || _player.Condition == Condition.Attached;
            if (!ropeVisible) return;

            int pcx = _player.Location.X - scrollX + PCX;
            int pcy = _player.Location.Y + PCY;
            int hx = _player.Projectile.Location.X - scrollX;
            int hy = _player.Projectile.Location.Y;

            using (var rope = new Pen(Color.FromArgb(210, 140, 90, 40), 3f))
            {
                rope.StartCap = LineCap.Round;
                rope.EndCap = LineCap.Round;
                g.DrawLine(rope, pcx, pcy, hx, hy);
            }

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
            if (!_player.EnableGrapple) return;
            if (_player.Projectile.IsActive || _player.Condition == Condition.Attached) return;

            float sx = _player.Location.X - scrollX + PCX;
            float sy = _player.Location.Y + PCY;

            double rad = _player.AimAngle * (Math.PI / 180.0);
            float dx = (float)Math.Cos(rad);
            float dy = (float)Math.Sin(rad);

            float ceilY = LevelBuilder.CeilBot;
            float floorY = LevelBuilder.FloorTop;

            float maxLen = 1600f;
            float tx = sx + dx * maxLen;
            float ty = sy + dy * maxLen;

            if (dy < 0 && ty < ceilY)
            {
                float t = (ceilY - sy) / dy;
                tx = sx + dx * t; ty = ceilY;
            }
            else if (dy > 0 && ty > floorY && _player.CanGrappleGround)
            {
                float t = (floorY - sy) / dy;
                tx = sx + dx * t; ty = floorY;
            }

            using (var lp = new Pen(Color.FromArgb(180, 100, 255, 100), 1.8f)
            { DashStyle = DashStyle.Dash })
                g.DrawLine(lp, sx, sy, tx, ty);

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
                using var fb = new SolidBrush(_player.IsDead ? Color.Red : Color.DodgerBlue);
                if (_player.GravityScale < 0)
                {
                    g.TranslateTransform(sx + PW / 2f, sy + PH / 2f);
                    g.RotateTransform(180);
                    g.FillRectangle(fb, -PW / 2f, -PH / 2f, PW, PH);
                    g.ResetTransform();
                }
                else
                {
                    g.FillRectangle(fb, sx, sy, PW, PH);
                }
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

            if (_player.GravityScale < 0)
            {
                g.TranslateTransform(sx + PW / 2f, sy + PH / 2f);
                g.RotateTransform(180);
                g.DrawImage(Resources.digger,
                    new Rectangle(-PW / 2, -PH / 2, PW, PH),
                    new Rectangle(col * dw, _animRow * dh, dw, dh),
                    GraphicsUnit.Pixel);
                g.ResetTransform();
            }
            else
            {
                g.DrawImage(Resources.digger,
                    new Rectangle(sx, sy, PW, PH),
                    new Rectangle(col * dw, _animRow * dh, dw, dh),
                    GraphicsUnit.Pixel);
            }
        }

        private void DrawHUD(Graphics g)
        {
            int w = ClientSize.Width;
            int barW = w - 40, barH = 16, barX = 20, barY = 5;

            float prog = TotalCoins > 0
                ? Math.Min(1f, (float)_score / TotalCoins) : 0f;

            using (var tr = new SolidBrush(Color.FromArgb(110, 0, 0, 0)))
                g.FillRectangle(tr, barX, barY, barW, barH);

            if (prog > 0)
            {
                using var fill = new LinearGradientBrush(
                    new Point(barX, barY), new Point(barX + barW, barY),
                    Color.FromArgb(220, 255, 205, 0),
                    Color.FromArgb(220, 255, 82, 0));
                g.FillRectangle(fill, barX, barY, (int)(barW * prog), barH);
            }

            using (var bp = new Pen(Color.FromArgb(170, 255, 205, 82), 1.5f))
                g.DrawRectangle(bp, barX, barY, barW, barH);

            using (var hf = new Font("Segoe UI", 13, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var hb = new SolidBrush(Color.FromArgb(225, 255, 235, 82)))
                g.DrawString($"  {_score} / {TotalCoins}", hf, hb, barX + 4, barY + barH + 2);

            int secs = Math.Max(0, LevelTicksRemaining) / 60;
            using (var tf2 = new Font("Consolas", 13, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var tb2 = new SolidBrush(secs <= 10 ? Color.OrangeRed
                                                        : Color.FromArgb(200, 200, 255, 200)))
            {
                string ts = $"{secs}s";
                var tsz = g.MeasureString(ts, tf2);
                g.DrawString(ts, tf2, tb2, w - tsz.Width - 12, barY + barH + 2);
            }

            using (var cb2 = new SolidBrush(Color.FromArgb(80, 200, 200, 200)))
                g.DrawString("[W] Hook   [A/D] Aim   [Space] Detach   [Esc] Menu",
                    _fSegoe11, cb2, 10, ClientSize.Height - 22);
        }

        private void DrawDeath(Graphics g)
        {
            int w = ClientSize.Width, h = ClientSize.Height, cx = w / 2, cy2 = h / 2;

            _overlayAlpha = Math.Min(1f, _overlayAlpha + FadeSpeed);

            using (var bg = new SolidBrush(Color.FromArgb((int)(_overlayAlpha * 200), 60, 0, 0)))
                g.FillRectangle(bg, 0, 0, w, h);

            if (_overlayAlpha < 0.3f) { Invalidate(); return; }

            float a = Math.Min(1f, (_overlayAlpha - 0.3f) / 0.7f);
            int ia = (int)(a * 255);

            using (var tf = new Font("Impact", 80, FontStyle.Regular, GraphicsUnit.Pixel))
            {
                string txt = "ВЫ ПРОИГРАЛИ";
                var sz = g.MeasureString(txt, tf);
                float tx = cx - sz.Width / 2;
                float ty = cy2 - sz.Height / 2 - 40;

                using (var sh = new SolidBrush(Color.FromArgb(ia, 180, 0, 0)))
                    g.DrawString(txt, tf, sh, tx + 4, ty + 4);
                using (var tb2 = new SolidBrush(Color.FromArgb(ia, 255, 220, 220)))
                    g.DrawString(txt, tf, tb2, tx, ty);
            }

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

            if (_overlayAlpha < 1f) Invalidate();
        }

        private void DrawVictory(Graphics g)
        {
            int w = ClientSize.Width, h = ClientSize.Height, cx = w / 2, cy2 = h / 2;

            _overlayAlpha = Math.Min(1f, _overlayAlpha + FadeSpeed);

            using (var bg = new SolidBrush(Color.FromArgb((int)(_overlayAlpha * 210), 30, 25, 0)))
                g.FillRectangle(bg, 0, 0, w, h);

            if (_overlayAlpha < 0.25f) { Invalidate(); return; }

            float a = Math.Min(1f, (_overlayAlpha - 0.25f) / 0.75f);
            int ia = (int)(a * 255);

            for (int i = 0; i < 60; i++)
            {
                float sx = _rng.Next(w), sy = _rng.Next(h);
                float sz = (float)(_rng.NextDouble() * 6 + 2);
                int ca = (int)(a * _rng.Next(80, 200));
                var col = Color.FromArgb(ca,
                    _rng.Next(180, 255), _rng.Next(140, 220), _rng.Next(0, 80));
                using var cb2 = new SolidBrush(col);
                g.FillEllipse(cb2, sx - sz / 2, sy - sz / 2, sz, sz);
            }

            string txt = "ВЫ ВЫИГРАЛИ!";
            float ty = cy2 - 120;
            using (var sh = new SolidBrush(Color.FromArgb(ia, 120, 90, 0)))
                g.DrawString(txt, _fImpact80, sh, cx + 4, ty + 4, _centerFormat);
            using (var fill = new SolidBrush(Color.FromArgb(ia, 255, 235, 80)))
                g.DrawString(txt, _fImpact80, fill, cx, ty, _centerFormat);

            if (a > 0.4f)
            {
                int ia2 = (int)(Math.Min(1f, (a - 0.4f) / 0.6f) * 255);

                string rating = GetRating(_victoryCoins, _victoryTotal);
                using (var rf = new Font("Impact", 36, FontStyle.Regular, GraphicsUnit.Pixel))
                using (var rb = new SolidBrush(Color.FromArgb(ia2, 255, 220, 60)))
                {
                    var rsz = g.MeasureString(rating, rf);
                    g.DrawString(rating, rf, rb, cx - rsz.Width / 2, cy2 - 10);
                }

                string coinStr = $"Монет собрано: {_victoryCoins} / {_victoryTotal}";
                using (var cf2 = new Font("Segoe UI", 22, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var cb3 = new SolidBrush(Color.FromArgb(ia2, 255, 240, 160)))
                {
                    var csz = g.MeasureString(coinStr, cf2);
                    g.DrawString(coinStr, cf2, cb3, cx - csz.Width / 2, cy2 + 45);
                }

                float pct = _victoryTotal > 0 ? (float)_victoryCoins / _victoryTotal * 100f : 0;
                string pctStr = $"{pct:F0}% завершения";
                using (var pf = new Font("Segoe UI", 18, FontStyle.Italic, GraphicsUnit.Pixel))
                using (var pb2 = new SolidBrush(Color.FromArgb(ia2, 200, 220, 180)))
                {
                    var psz = g.MeasureString(pctStr, pf);
                    g.DrawString(pctStr, pf, pb2, cx - psz.Width / 2, cy2 + 80);
                }

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

        private static Rectangle Rect(int x, int y, int w, int h)
            => new Rectangle(x, y, w, h);

        private static bool Contains(Rectangle r, Point p)
            => p.X >= r.X && p.X <= r.X + r.Width && p.Y >= r.Y && p.Y <= r.Y + r.Height;
    }

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
