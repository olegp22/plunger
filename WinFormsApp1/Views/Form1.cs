// Form1.cs  —  View layer (MVP)
// Responsibilities: render, raise events, implement IMainView.
// Must NOT contain game logic — only drawing and event forwarding.

using System;
using System.Collections.Generic;
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
        // ── IMainView events ──────────────────────────────────────────────────
        // Nullable because WinForms convention; Presenter always subscribes before use.
        public event Action?       TimerTick;
        public event Action<Keys>? KeyPressed;

        // ── IMainView HUD properties (written by Presenter; read by DrawHUD) ──
        public int LevelTicksRemaining { get; private set; } = GamePresenter.LevelDurationTicks;
        public int TotalCoins          { get; private set; } = 0;

        // ── Sprite sheet config (digger.png: 4 rows × 4 cols) ─────────────────
        private const int SpriteRows = 4, SpriteCols = 4;
        private const int RowRun = 0, RowGrapple = 1, RowSomersault = 2, RowFall = 3;
        private const int RunFrameCount = 4, GrappleFrameCount = 4;
        private const int SomersaultFrameCount = 3, FallFrameCount = 4;

        // ── Animation state ───────────────────────────────────────────────────
        private int  _row = RowRun, _col = 0, _frameTimer = 0;
        private const int FrameDelay = 5;
        private bool _somersaulting = false;

        // ── Coin sheet ────────────────────────────────────────────────────────
        private const int CoinCols = 8;
        private int _coinFrame = 0, _coinTimer = 0;

        // ── Plunger sheet ─────────────────────────────────────────────────────
        private const int VantusCols = 2;

        // ── Game data (supplied by Presenter via SetGameData) ─────────────────
        private Sucker?       _player    = null;
        private List<Coin>?   _coins     = null;
        private PlatformList? _platforms = null;
        private int           _score     = 0;

        // ── Player render constants ───────────────────────────────────────────
        private const int PW = 90, PH = 110, PCX = 45, PCY = 55;

        // ── UI mode ───────────────────────────────────────────────────────────
        private bool _menu     = true;
        private int  _menuHover = -1;

        // Presenter — assigned in Form1_Load, so always non-null during painting.
        private GamePresenter _presenter = null!;

        // ═════════════════════════════════════════════════════════════════════
        public Form1()
        {
            InitializeComponent();

            // Explicit delegate casts suppress CS8622 for nullable-sender signatures.
            KeyDown    += new KeyEventHandler   (OnKeyDown);
            MouseMove  += new MouseEventHandler (OnMouseMove);
            MouseClick += new MouseEventHandler (OnMouseClick);

            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint  |
                     ControlStyles.UserPaint, true);
        }

        // Form1_Load wired in Designer via new EventHandler(Form1_Load)
        private void Form1_Load(object sender, EventArgs e)
        {
            _presenter = new GamePresenter(this);
        }

        // gameTimer_Tick wired in Designer via new EventHandler(gameTimer_Tick)
        private void gameTimer_Tick(object sender, EventArgs e)
            => TimerTick?.Invoke();

        // ── Keyboard / mouse ──────────────────────────────────────────────────
        private void OnKeyDown(object? sender, KeyEventArgs e)
            => KeyPressed?.Invoke(e.KeyCode);

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            if (!_menu) return;
            _menuHover = HitTest(e.Location);
            Invalidate();
        }

        private void OnMouseClick(object? sender, MouseEventArgs e)
        {
            if (!_menu) return;
            int hit = HitTest(e.Location);
            if      (hit == 0) _presenter.StartLevel(1);
            else if (hit == 1) _presenter.StartLevel(2);
        }

        private int HitTest(Point pt)
        {
            int cx = ClientSize.Width / 2;
            if (new Rectangle(cx - 150, 240, 300, 65).Contains(pt)) return 0;
            if (new Rectangle(cx - 150, 330, 300, 65).Contains(pt)) return 1;
            return -1;
        }

        // ── IMainView implementation ──────────────────────────────────────────
        public void SetGameData(Sucker player, List<Coin> coins, PlatformList platforms)
        {
            _player    = player;
            _coins     = coins;
            _platforms = platforms;
            TotalCoins = coins.Count;
            _score     = 0;
            _row = RowRun; _col = 0; _frameTimer = 0; _somersaulting = false;
        }

        public void RefreshView()  => Invalidate();
        public void UpdateScore(int score)
        {
            _score = score;
            Text   = $"Plunger Dash — Score: {_score}";
        }
        public void ShowMenu() { _menu = true;  LevelTicksRemaining = GamePresenter.LevelDurationTicks; gameTimer.Start(); Invalidate(); }
        public void ShowGame() { _menu = false; gameTimer.Start(); Invalidate(); }

        // ── Paint ─────────────────────────────────────────────────────────────
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;

            // Always pull latest timer value from presenter before drawing
            LevelTicksRemaining = _presenter?.LevelTicksRemaining
                                  ?? GamePresenter.LevelDurationTicks;

            if (_menu) { DrawMenu(g); return; }

            if (Resources.background != null)
                g.DrawImage(Resources.background,
                    new Rectangle(0, 0, ClientSize.Width, ClientSize.Height));
            else
                g.Clear(Color.FromArgb(15, 12, 30));

            if (_player == null) return;

            UpdateAnimState();
            AdvanceTimers();
            DrawPlatforms(g);
            DrawCoins(g);
            DrawRope(g);
            DrawLaser(g);
            DrawPlayer(g);
            DrawHUD(g);
        }

        // ═════════════════════════════════════════════════════════════════════
        // MENU
        // ═════════════════════════════════════════════════════════════════════
        private void DrawMenu(Graphics g)
        {
            int w = ClientSize.Width, h = ClientSize.Height, cx = w / 2;

            using (var bg = new LinearGradientBrush(
                    new Point(0,0), new Point(0,h),
                    Color.FromArgb(10,8,25), Color.FromArgb(25,15,50)))
                g.FillRectangle(bg, 0, 0, w, h);

            var rng = new Random(42);
            for (int i = 0; i < 80; i++)
            {
                float sz = (float)(rng.NextDouble() * 2.5 + 0.5);
                using var sb = new SolidBrush(Color.FromArgb(rng.Next(100,255), 220, 220, 255));
                g.FillEllipse(sb, rng.Next(w) - sz/2, rng.Next(h) - sz/2, sz, sz);
            }

            using (var tf  = new Font("Impact", 52, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var sh  = new SolidBrush(Color.FromArgb(120, 255, 180, 0)))
            using (var tb  = new LinearGradientBrush(
                    new Point(cx-250, 90), new Point(cx+250, 150),
                    Color.FromArgb(255,255,220,80), Color.FromArgb(255,255,140,0)))
            {
                string t = "PLUNGER DASH";
                var sz = g.MeasureString(t, tf);
                g.DrawString(t, tf, sh, cx - sz.Width/2 + 3, 93);
                g.DrawString(t, tf, tb, cx - sz.Width/2,     90);
            }

            DrawCentred(g, "Grapple  •  Swing  •  Collect",
                new Font("Segoe UI", 15, FontStyle.Italic, GraphicsUnit.Pixel),
                Color.FromArgb(180,200,200,255), cx, 158);

            DrawCentred(g, "[W] Shoot    [A / D] Aim    [Space] Detach",
                new Font("Segoe UI", 13, FontStyle.Regular, GraphicsUnit.Pixel),
                Color.FromArgb(140,180,255,180), cx, 192);

            DrawMenuBtn(g, cx, 240, "LEVEL 1 — The Run",
                "Ceiling grapple  |  coin chase", _menuHover == 0);
            DrawMenuBtn(g, cx, 330, "LEVEL 2 — Platforms",
                "Blocks & ground grapple  |  precision swing", _menuHover == 1);

            DrawCentred(g, "Click a level to start",
                new Font("Segoe UI", 11, FontStyle.Regular, GraphicsUnit.Pixel),
                Color.FromArgb(80,180,180,180), cx, h - 28);
        }

        private static void DrawCentred(Graphics g, string text, Font font, Color color, int cx, int y)
        {
            using var brush = new SolidBrush(color);
            var sz = g.MeasureString(text, font);
            g.DrawString(text, font, brush, cx - sz.Width / 2, y);
            font.Dispose();
        }

        private void DrawMenuBtn(Graphics g, int cx, int y, string label, string sub, bool hovered)
        {
            const int bw = 300, bh = 65;
            int bx = cx - bw / 2;

            var bgC = hovered ? Color.FromArgb(220,40,30,80) : Color.FromArgb(160,20,15,50);
            var brC = hovered ? Color.FromArgb(255,255,180,0) : Color.FromArgb(180,100,80,200);

            using (var bb = new SolidBrush(bgC)) g.FillRoundedRect(bb, bx, y, bw, bh, 12);
            using (var bp = new Pen(brC, hovered ? 2.5f : 1.5f)) g.DrawRoundedRect(bp, bx, y, bw, bh, 12);

            using (var lf = new Font("Impact", 20, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var lb = new SolidBrush(hovered ? Color.FromArgb(255,255,230,80) : Color.White))
            { var ls = g.MeasureString(label, lf); g.DrawString(label, lf, lb, cx - ls.Width/2, y+7); }

            using (var sf = new Font("Segoe UI", 12, FontStyle.Italic, GraphicsUnit.Pixel))
            using (var sb2 = new SolidBrush(Color.FromArgb(hovered?220:140, 180,200,255)))
            { var ss = g.MeasureString(sub, sf); g.DrawString(sub, sf, sb2, cx - ss.Width/2, y+36); }
        }

        // ═════════════════════════════════════════════════════════════════════
        // ANIMATION
        // ═════════════════════════════════════════════════════════════════════
        private void UpdateAnimState()
        {
            if (_player == null) return;

            if (_player.SomersaultThisFrame)
            {
                _player.ConsumeSomersault();
                _somersaulting = true;
                _row = RowSomersault; _col = 0; _frameTimer = 0;
            }

            if (_somersaulting)
            {
                if (_col >= SomersaultFrameCount - 1 && _frameTimer >= FrameDelay)
                    _somersaulting = false;
                return;
            }

            switch (_player.Condition)
            {
                case Condition.Run:
                    if (_row != RowRun)        { _row = RowRun;        _col = 0; _frameTimer = 0; } break;
                case Condition.Attached:
                    if (_row != RowGrapple)    { _row = RowGrapple;    _col = 0; _frameTimer = 0; }
                    _col = AngleToGrappleFrame(_player.AimAngle); break;
                case Condition.Fall:
                    if (_row != RowFall)       { _row = RowFall;       _col = 0; _frameTimer = 0; } break;
            }
        }

        private static int AngleToGrappleFrame(double a)
        {
            if (a <= -120) return 0;
            if (a <=  -70) return 1;
            if (a <=  -30) return 2;
            return 3;
        }

        private void AdvanceTimers()
        {
            if (_player == null) return;

            _frameTimer++;
            if (_frameTimer > FrameDelay)
            {
                _frameTimer = 0;
                int maxF = _row switch
                {
                    RowRun        => RunFrameCount,
                    RowGrapple    => GrappleFrameCount,
                    RowSomersault => SomersaultFrameCount,
                    RowFall       => FallFrameCount,
                    _             => 1
                };
                if (_row != RowGrapple)   // grapple col is angle-driven
                    _col = (_col + 1) % maxF;
                if (_row == RowRun && _player.Condition != Condition.Run)
                    _col = 0;
            }

            _coinTimer++;
            if (_coinTimer > 4) { _coinFrame = (_coinFrame + 1) % CoinCols; _coinTimer = 0; }
        }

        // ═════════════════════════════════════════════════════════════════════
        // GAME RENDERING
        // ═════════════════════════════════════════════════════════════════════
        private void DrawPlatforms(Graphics g)
        {
            if (_platforms == null) return;
            foreach (var plat in _platforms.Items)
            {
                var pb   = plat.GetBounds();
                var rect = new Rectangle(pb.Left, pb.Top, pb.Width, pb.Height);

                using (var body = new LinearGradientBrush(
                        new Point(pb.Left, pb.Top), new Point(pb.Left, pb.Bottom),
                        Color.FromArgb(200,90,70,40), Color.FromArgb(220,50,35,15)))
                    g.FillRectangle(body, rect);

                using (var hl = new Pen(Color.FromArgb(160,200,160,80), 3))
                    g.DrawLine(hl, pb.Left, pb.Top+1, pb.Right, pb.Top+1);

                using (var bk = new Pen(Color.FromArgb(60,0,0,0), 1))
                    for (int bx = pb.Left+20; bx < pb.Right; bx += 20)
                        g.DrawLine(bk, bx, pb.Top, bx, pb.Bottom);

                using (var bdr = new Pen(Color.FromArgb(180,120,90,40), 1.5f))
                    g.DrawRectangle(bdr, rect);
            }
        }

        private void DrawCoins(Graphics g)
        {
            if (_coins == null) return;
            if (Resources.coin != null)
            {
                int cw = Resources.coin.Width / CoinCols, ch = Resources.coin.Height;
                foreach (var c in _coins)
                {
                    if (c.IsCollected) continue;
                    var b = c.GetBounds();
                    g.DrawImage(Resources.coin,
                        new Rectangle(b.X, b.Y, b.Width, b.Height),
                        new Rectangle(_coinFrame * cw, 0, cw, ch),
                        GraphicsUnit.Pixel);
                }
            }
            else
            {
                foreach (var c in _coins)
                {
                    if (c.IsCollected) continue;
                    var b = c.GetBounds();
                    using var cb = new SolidBrush(Color.Gold);
                    g.FillEllipse(cb, b.X, b.Y, b.Width, b.Height);
                }
            }
        }

        private void DrawRope(Graphics g)
        {
            if (_player == null) return;
            if (!_player.Projectile.IsActive && _player.Condition != Condition.Attached) return;

            int cx = _player.Location.X + PCX, cy = _player.Location.Y + PCY;
            int px = _player.Projectile.Location.X, py = _player.Projectile.Location.Y;

            using (var rope = new Pen(Color.SaddleBrown, 3))
                g.DrawLine(rope, cx, cy, px, py);

            if (Resources.vantus != null)
            {
                int vw = Resources.vantus.Width / VantusCols, vh = Resources.vantus.Height;
                int vf = _player.Condition == Condition.Attached ? 1 : 0;
                g.DrawImage(Resources.vantus,
                    new Rectangle(px-18, py-18, 36, 36),
                    new Rectangle(vf * vw, 0, vw, vh), GraphicsUnit.Pixel);
            }
            else
            {
                using var pb = new SolidBrush(Color.OrangeRed);
                g.FillEllipse(pb, px-8, py-8, 16, 16);
            }
        }

        private void DrawLaser(Graphics g)
        {
            if (_player == null) return;
            if (_player.Projectile.IsActive || _player.Condition == Condition.Attached) return;

            float sx = _player.Location.X + PCX, sy = _player.Location.Y + PCY;
            double rad = _player.AimAngle * (Math.PI / 180.0);
            float ex = sx + (float)Math.Cos(rad) * 1400;
            float ey = sy + (float)Math.Sin(rad) * 1400;

            float cY = Sucker.CeilingY, fY = Sucker.FloorY, tx = ex, ty = ey;

            if (ey < cY && ey < sy)
            { float r = (cY-sy)/(ey-sy); tx = sx+(ex-sx)*r; ty = cY; }
            else if (ey > fY && _player.CanGrappleGround)
            { float r = (fY-sy)/(ey-sy); tx = sx+(ex-sx)*r; ty = fY; }

            using (var lp = new Pen(Color.Lime, 2) { DashStyle = DashStyle.Dash })
                g.DrawLine(lp, sx, sy, tx, ty);

            // Arrow near player
            float al = 22f, ba = (float)rad, aa = (float)(Math.PI/5.5);
            var tip = new PointF(sx+(float)Math.Cos(ba)*75, sy+(float)Math.Sin(ba)*75);
            var L   = new PointF(tip.X-al*(float)Math.Cos(ba-aa), tip.Y-al*(float)Math.Sin(ba-aa));
            var R   = new PointF(tip.X-al*(float)Math.Cos(ba+aa), tip.Y-al*(float)Math.Sin(ba+aa));
            using (var ab = new SolidBrush(Color.FromArgb(200,255,220,50)))
                g.FillPolygon(ab, new[]{tip, L, R});

            // Target circle
            if (ty <= cY+2f || ty >= fY-2f)
            {
                float r = 12f;
                using (var glow = new Pen(Color.FromArgb(100,255,180,0), 5f))
                    g.DrawEllipse(glow, tx-r-3, ty-r-3, (r+3)*2, (r+3)*2);
                using (var fill = new SolidBrush(Color.FromArgb(200,255,220,50)))
                    g.FillEllipse(fill, tx-r, ty-r, r*2, r*2);
                using (var cp = new Pen(Color.FromArgb(160,80,50,0), 1.5f))
                { g.DrawLine(cp, tx-r, ty, tx+r, ty); g.DrawLine(cp, tx, ty-r, tx, ty+r); }
            }
        }

        private void DrawPlayer(Graphics g)
        {
            if (_player == null) return;
            if (Resources.digger == null)
            {
                using var fb = new SolidBrush(Color.DodgerBlue);
                g.FillRectangle(fb, _player.Location.X, _player.Location.Y, PW, PH);
                return;
            }

            int dw = Resources.digger.Width / SpriteCols;
            int dh = Resources.digger.Height / SpriteRows;

            int maxCol = _row switch
            {
                RowRun        => RunFrameCount-1,
                RowGrapple    => GrappleFrameCount-1,
                RowSomersault => SomersaultFrameCount-1,
                RowFall       => FallFrameCount-1,
                _             => 0
            };

            g.DrawImage(Resources.digger,
                new Rectangle(_player.Location.X, _player.Location.Y, PW, PH),
                new Rectangle(Math.Min(_col, maxCol) * dw, _row * dh, dw, dh),
                GraphicsUnit.Pixel);
        }

        private void DrawHUD(Graphics g)
        {
            int w = ClientSize.Width, barW = w-40, barH = 16, barX = 20, barY = 6;
            float prog = TotalCoins > 0 ? Math.Min(1f, (float)_score / TotalCoins) : 0f;

            using (var tr = new SolidBrush(Color.FromArgb(120,0,0,0)))
                g.FillRectangle(tr, barX, barY, barW, barH);

            if (prog > 0)
            {
                using var fill = new LinearGradientBrush(
                    new Point(barX, barY), new Point(barX+barW, barY),
                    Color.FromArgb(220,255,200,0), Color.FromArgb(220,255,80,0));
                g.FillRectangle(fill, barX, barY, (int)(barW*prog), barH);
            }

            using (var bp = new Pen(Color.FromArgb(180,255,200,80), 1.5f))
                g.DrawRectangle(bp, barX, barY, barW, barH);

            using (var hf = new Font("Segoe UI", 13, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var hb = new SolidBrush(Color.FromArgb(230,255,230,80)))
                g.DrawString($"  {_score} / {TotalCoins}  coins", hf, hb, barX+4, barY+barH+3);

            int secs = Math.Max(0, LevelTicksRemaining) / 60;
            using (var tf = new Font("Consolas", 13, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var tb = new SolidBrush(secs <= 10 ? Color.OrangeRed : Color.FromArgb(200,200,255,200)))
            {
                string ts = $"  {secs}s";
                var sz = g.MeasureString(ts, tf);
                g.DrawString(ts, tf, tb, w-sz.Width-10, barY+barH+3);
            }
        }
    }

    // ── Graphics helpers ───────────────────────────────────────────────────────
    internal static class GfxExt
    {
        public static void FillRoundedRect(this Graphics g, Brush b,
            int x, int y, int w, int h, int r)
        { using var p = Path(x,y,w,h,r); g.FillPath(b,p); }

        public static void DrawRoundedRect(this Graphics g, Pen pen,
            int x, int y, int w, int h, int r)
        { using var p = Path(x,y,w,h,r); g.DrawPath(pen,p); }

        private static GraphicsPath Path(int x, int y, int w, int h, int r)
        {
            var p = new GraphicsPath();
            p.AddArc(x,         y,         r*2, r*2, 180, 90);
            p.AddArc(x+w-r*2,   y,         r*2, r*2, 270, 90);
            p.AddArc(x+w-r*2,   y+h-r*2,   r*2, r*2,   0, 90);
            p.AddArc(x,         y+h-r*2,   r*2, r*2,  90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
