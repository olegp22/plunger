using Plunger;
using Plunger.Models;
using Plunger.Views;
using System.Drawing.Drawing2D;
using WinFormsApp1.Properties;


namespace WinFormsApp1;

public partial class Form1 : Form, IMainView
{
    public event Action TimerTick;
    // --- НАСТРОЙКИ АНИМАЦИИ ---
    // 1. Диггер (Сетка: столбцы = кадры, строки = состояния)
    private int _diggerCols = 4; // Скорее всего у тебя 4 или 6 кадров в ряд (поменяй если не так)
    private int _diggerRows = 3; // Сколько строк с разными действиями сверху вниз?
    private int _diggerCurrentFrame = 0;
    private int _diggerFrameTimer = 0;

    // 2. Монетки (4 кадра в ряд)
    private int _coinCols = 8;
    private int _coinCurrentFrame = 0;
    private int _coinFrameTimer = 0;

    // 3. Вантуз (2 кадра: 0 - летит, 1 - присосался)
    private int _vantusCols = 2;
    public event Action<Keys>? KeyPressed;

    private Sucker? _player;
    private List<Coin>? _coins;

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        // Отправляем информацию о нажатой клавише в Презентер
        KeyPressed?.Invoke(e.KeyCode);
    }
    public Form1()
    {
        InitializeComponent();
        // Настрой таймер в дизайнере или кодом (например, 30-60 FPS)
        gameTimer.Interval = 16; // ~60 кадров в секунду
        gameTimer.Start();
    }

    private void gameTimer_Tick(object sender, EventArgs e)
    {
        TimerTick?.Invoke();
    }

    public void RefreshView() => this.Invalidate(); // Заставляет вызвать OnPaint

    public void UpdateScore(int score)
    {
        this.Text = $"Plunger Dash | Coins: {score}";
    }

    public void SetGameData(Sucker player, List<Coin> coins)
    {
        _player = player;
        _coins = coins;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_player == null || _coins == null) return;

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // 1. ФОН
        if (Properties.Resources.background != null)
            g.DrawImage(Properties.Resources.background, 0, 0, this.ClientSize.Width, this.ClientSize.Height);

        // 2. КАМЕРА (25% слева)
        float screenAnchorX = this.ClientSize.Width * 0.25f;
        float cameraX = _player.Location.X - screenAnchorX;
        float cameraY = _player.Location.Y - (this.ClientSize.Height * 0.5f);
        g.TranslateTransform(-cameraX, -cameraY);

        // 3. НОВЫЙ ЦЕНТР (для размера 120 это +60)
        int playerCenterX = _player.Location.X + 60;
        int playerCenterY = _player.Location.Y + 60;

        // 4. ТРОС И ВАНТУЗ (Увеличен до 45x45)
        if (_player.Projectile.IsActive || _player.Condition == Condition.Attached)
        {
            g.DrawLine(new Pen(Color.SaddleBrown, 5), playerCenterX, playerCenterY,
                _player.Projectile.Location.X + 22, _player.Projectile.Location.Y + 22);

            if (Properties.Resources.vantus != null)
            {
                int vw = Properties.Resources.vantus.Width / _vantusCols;
                int vFrame = (_player.Condition == Condition.Attached) ? 1 : 0;
                // Размер вантуза 45x45
                g.DrawImage(Properties.Resources.vantus,
                    new Rectangle(_player.Projectile.Location.X, _player.Projectile.Location.Y, 45, 45),
                    new Rectangle(vFrame * vw, 0, vw, Properties.Resources.vantus.Height),
                    GraphicsUnit.Pixel);
            }
        }

        // 5. ДИГГЕР (Увеличен до 120x120)
        if (Properties.Resources.digger != null)
        {
            int dw = Properties.Resources.digger.Width / _diggerCols;
            int dh = Properties.Resources.digger.Height / _diggerRows;
            int row = (_player.Condition == Condition.Fall) ? 1 : (_player.Condition == Condition.Attached ? 2 : 0);

            g.DrawImage(Properties.Resources.digger,
                new Rectangle(_player.Location.X, _player.Location.Y, 120, 120),
                new Rectangle(_diggerCurrentFrame * dw, row * dh, dw, dh),
                GraphicsUnit.Pixel);

            // Анимация... (код остается прежним)
        }

        // 6. МОНЕТКИ (Оставляем 40x40, чтобы не были гигантскими)
        foreach (var coin in _coins)
        {
            if (!coin.IsCollected)
            {
                var b = coin.GetBounds();
                if (Properties.Resources.coin != null)
                {
                    int cw = Properties.Resources.coin.Width / _coinCols;
                    g.DrawImage(Properties.Resources.coin, new Rectangle(b.Left, b.Top, 40, 40),
                        new Rectangle(_coinCurrentFrame * cw, 0, cw, Properties.Resources.coin.Height),
                        GraphicsUnit.Pixel);
                }
            }
        }

        // 7. ЛИНИЯ ПРИЦЕЛА (от нового центра)
        if (!_player.Projectile.IsActive && _player.Condition != Condition.Attached)
        {
            double rad = _player.AimAngle * (Math.PI / 180.0);
            g.DrawLine(new Pen(Color.Lime, 3) { DashStyle = DashStyle.Dash },
                playerCenterX, playerCenterY,
                (int)(playerCenterX + Math.Cos(rad) * 100),
                (int)(playerCenterY + Math.Sin(rad) * 100));
        }

        g.ResetTransform();
    }
    private void Form1_Load(object sender, EventArgs e)
    {

    }
}