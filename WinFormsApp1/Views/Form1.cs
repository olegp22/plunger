using Plunger;
using Plunger.Models;
using Plunger.Views;
using System.Drawing.Drawing2D;
using WinFormsApp1.Properties;

namespace WinFormsApp1;

public partial class Form1 : Form, IMainView
{
    // ── View events ──────────────────────────────────────────────────────────
    public event Action? TimerTick;
    public event Action<Keys>? KeyPressed;

    // ── Sprite sheet config ──────────────────────────────────────────────────
    // Digger sprite sheet layout: columns = animation frames, rows = states
    //   Row 0 → Run
    //   Row 1 → Fall / somersault
    //   Row 2 → Attached (hanging)
    private const int DiggerCols = 4;   // frames per row — adjust to match your sheet
    private const int DiggerRows = 3;   // number of state rows

    // Coin sheet: 8 frames in a single horizontal strip
    private const int CoinCols = 8;

    // Plunger sheet: 2 frames — 0 = flying, 1 = stuck
    private const int VantusCols = 2;

    // Drawn size of the digger sprite — tall enough to show full body (no knee cutoff)
    private const int DiggerDrawW = 120;
    private const int DiggerDrawH = 150;   // was 190, which caused visible cutoff

    // ── Animation counters ───────────────────────────────────────────────────
    private int _diggerCurrentFrame = 0;
    private int _diggerFrameTimer   = 0;

    private int _coinCurrentFrame = 0;
    private int _coinFrameTimer   = 0;

    // ── Game data ────────────────────────────────────────────────────────────
    private Sucker?    _player;
    private List<Coin>? _coins;

    // ── Boundary constants in sync with Sucker.cs ────────────────────────────
    // These are world-space Y values (5 % of a 558 px client area).
    // If the window is resizable, recompute them from ClientSize instead.
    private int CeilingPx => (int)(ClientSize.Height * 0.05f); // ≈ 28 px
    private int FloorPx   => ClientSize.Height - CeilingPx;

    // ── Constructor ──────────────────────────────────────────────────────────
    public Form1()
    {
        InitializeComponent();

        // Ensure DoubleBuffered is set in code too — eliminates the "ghost" duplicate
        this.DoubleBuffered = true;

        gameTimer.Interval = 16; // ~60 FPS
        gameTimer.Start();
    }

    // ── IMainView implementation ─────────────────────────────────────────────
    public void RefreshView()   => this.Invalidate();
    public void UpdateScore(int score) => this.Text = $"Plunger Dash | Coins: {score}";

    public void SetGameData(Sucker player, List<Coin> coins)
    {
        _player = player;
        _coins  = coins;
    }

    // ── Input ────────────────────────────────────────────────────────────────
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        KeyPressed?.Invoke(e.KeyCode);
    }

    // ── Timer ────────────────────────────────────────────────────────────────
    private void gameTimer_Tick(object sender, EventArgs e)
    {
        TimerTick?.Invoke();
    }

    // ── Rendering ────────────────────────────────────────────────────────────
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // 1. BACKGROUND — drawn before everything, covers full client area.
        //    Must be done before any camera transform is applied.
        if (Resources.background != null)
            g.DrawImage(Resources.background, 0, 0, ClientSize.Width, ClientSize.Height);
        else
            g.Clear(Color.CornflowerBlue);

        // Guard: nothing else to draw until the presenter provides data
        if (_player == null || _coins == null) return;

        // 2. CAMERA TRANSFORM
        //    The digger is pinned to 25 % from the left edge of the screen.
        float screenAnchorX = ClientSize.Width * 0.25f;
        float cameraX = _player.Location.X - screenAnchorX;
        // Keep the vertical camera fixed — only scroll horizontally.
        // Vertical scrolling causes the boundary bars to move, making them
        // appear in the middle of the screen (the original bug).
        g.TranslateTransform(-cameraX, 0);

        // 3. COINS (drawn behind digger)
        AdvanceCoinAnimation();
        if (Resources.coin != null)
        {
            int srcCoinW = Resources.coin.Width  / CoinCols;
            int srcCoinH = Resources.coin.Height;
            var srcCoin  = new Rectangle(_coinCurrentFrame * srcCoinW, 0, srcCoinW, srcCoinH);

            foreach (var coin in _coins)
            {
                if (!coin.IsCollected)
                {
                    var b = coin.GetBounds();
                    g.DrawImage(Resources.coin,
                        new Rectangle(b.Left, b.Top, 40, 40),
                        srcCoin,
                        GraphicsUnit.Pixel);
                }
            }
        }

        // 4. ROPE & PLUNGER (drawn between coins and digger so rope goes behind digger)
        if (Resources.vantus != null &&
            (_player.Projectile.IsActive || _player.Condition == Condition.Attached))
        {
            int pCenterX = _player.Location.X + DiggerDrawW / 2;
            int pCenterY = _player.Location.Y + DiggerDrawH / 2;

            int vCenterX = _player.Projectile.Location.X + 22;
            int vCenterY = _player.Projectile.Location.Y + 22;

            // Rope
            using var ropePen = new Pen(Color.SaddleBrown, 4);
            g.DrawLine(ropePen, pCenterX, pCenterY, vCenterX, vCenterY);

            // Plunger sprite: frame 0 = flying, frame 1 = stuck to ceiling
            int vw     = Resources.vantus.Width / VantusCols;
            int vFrame = (_player.Condition == Condition.Attached) ? 1 : 0;
            g.DrawImage(Resources.vantus,
                new Rectangle(_player.Projectile.Location.X, _player.Projectile.Location.Y, 45, 45),
                new Rectangle(vFrame * vw, 0, vw, Resources.vantus.Height),
                GraphicsUnit.Pixel);
        }

        // 5. DIGGER
        if (Resources.digger != null)
        {
            int srcDiggerW = Resources.digger.Width  / DiggerCols;
            int srcDiggerH = Resources.digger.Height / DiggerRows;

            // Choose animation row:
            //   Row 0 → Run
            //   Row 1 → Fall OR somersault
            //   Row 2 → Attached (hanging)
            int row;
            if (_player.SomersaultThisFrame)
                row = 1; // Somersault = Fall row (triggered on attach AND detach)
            else if (_player.Condition == Condition.Attached)
                row = 2;
            else if (_player.Condition == Condition.Fall)
                row = 1;
            else
                row = 0; // Run

            // Advance frame only while running; freeze at frame 0 otherwise
            if (_player.Condition == Condition.Run)
            {
                _diggerFrameTimer++;
                if (_diggerFrameTimer > 5)
                {
                    _diggerCurrentFrame = (_diggerCurrentFrame + 1) % DiggerCols;
                    _diggerFrameTimer   = 0;
                }
            }
            else
            {
                _diggerCurrentFrame = 0;
                _diggerFrameTimer   = 0;
            }

            // Clamp frame index so we never sample empty space at the edge of the sheet
            int safeFrame = Math.Clamp(_diggerCurrentFrame, 0, DiggerCols - 1);
            int safeRow   = Math.Clamp(row, 0, DiggerRows - 1);

            var srcRect = new Rectangle(safeFrame * srcDiggerW, safeRow * srcDiggerH,
                                        srcDiggerW, srcDiggerH);
            var dstRect = new Rectangle(_player.Location.X, _player.Location.Y,
                                        DiggerDrawW, DiggerDrawH);

            g.DrawImage(Resources.digger, dstRect, srcRect, GraphicsUnit.Pixel);
        }

        // 6. RESET TRANSFORM before drawing HUD elements so they stay screen-fixed
        g.ResetTransform();

        // 7. CEILING & FLOOR BARS — drawn in screen space (no camera offset).
        //    They are always at the absolute top and bottom of the window.
        int borderH = CeilingPx;
        using var borderBrush = new SolidBrush(Color.DarkSlateGray);
        g.FillRectangle(borderBrush, 0, 0,                           ClientSize.Width, borderH);
        g.FillRectangle(borderBrush, 0, ClientSize.Height - borderH, ClientSize.Width, borderH);
    }

    // ── Animation helpers ────────────────────────────────────────────────────
    private void AdvanceCoinAnimation()
    {
        _coinFrameTimer++;
        if (_coinFrameTimer > 8) // Slightly slower than digger
        {
            _coinCurrentFrame = (_coinCurrentFrame + 1) % CoinCols;
            _coinFrameTimer   = 0;
        }
    }

    // ── Designer hook ────────────────────────────────────────────────────────
    private void Form1_Load(object sender, EventArgs e) { }
}
