using Plunger;
using Plunger.Models;
using Plunger.Views;


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
    private int _coinCols = 4;
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
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // 1. ФОН
        if (Properties.Resources.background != null)
            g.DrawImage(Properties.Resources.background, 0, 0, this.ClientSize.Width, this.ClientSize.Height);
        else
            g.Clear(Color.FromArgb(40, 40, 40));

        // --- АНИМАЦИЯ МОНЕТОК (Общий таймер для всех монет) ---
        _coinFrameTimer++;
        if (_coinFrameTimer > 5) // Скорость вращения монетки
        {
            _coinCurrentFrame = (_coinCurrentFrame + 1) % _coinCols;
            _coinFrameTimer = 0;
        }

        // 2. РИСУЕМ МОНЕТКИ
        foreach (var coin in _coins)
        {
            if (!coin.IsCollected)
            {
                var bounds = coin.GetBounds();
                System.Drawing.Rectangle destRect = new System.Drawing.Rectangle(bounds.Left, bounds.Top, bounds.Width, bounds.Height);

                if (Properties.Resources.coin != null)
                {
                    Image coinSheet = Properties.Resources.coin;
                    int cWidth = coinSheet.Width / _coinCols; // Ширина одного кадра монетки
                    int cHeight = coinSheet.Height;
                    System.Drawing.Rectangle srcRect = new System.Drawing.Rectangle(_coinCurrentFrame * cWidth, 0, cWidth, cHeight);

                    g.DrawImage(coinSheet, destRect, srcRect, GraphicsUnit.Pixel);
                }
                else g.FillEllipse(Brushes.Gold, destRect);
            }
        }

        // 3. РИСУЕМ ТРОС (Коричневая линия)
        if (_player.Projectile.IsActive || _player.Condition == Condition.Attached)
        {
            Pen ropePen = new Pen(Color.SaddleBrown, 3);
            int playerCenterX = _player.Location.X + 20;
            int playerCenterY = _player.Location.Y + 20;
            var pBounds = _player.Projectile.GetBounds();
            int projCenterX = pBounds.Left + pBounds.Width / 2;
            int projCenterY = pBounds.Top + pBounds.Height / 2;
            g.DrawLine(ropePen, playerCenterX, playerCenterY, projCenterX, projCenterY);
        }

        // 4. РИСУЕМ ВАНТУЗ (Спрайт с 2 кадрами)
        if (_player.Projectile.IsActive || _player.Condition == Condition.Attached)
        {
            var pBounds = _player.Projectile.GetBounds();
            System.Drawing.Rectangle destRect = new System.Drawing.Rectangle(pBounds.Left, pBounds.Top, pBounds.Width, pBounds.Height);

            if (Properties.Resources.vantus != null)
            {
                Image vantusSheet = Properties.Resources.vantus;
                int vWidth = vantusSheet.Width / _vantusCols;
                int vHeight = vantusSheet.Height;

                // Выбираем кадр: 1 (второй) если присосался, иначе 0 (первый)
                int vFrameIndex = (_player.Condition == Condition.Attached) ? 1 : 0;
                System.Drawing.Rectangle srcRect = new System.Drawing.Rectangle(vFrameIndex * vWidth, 0, vWidth, vHeight);

                g.DrawImage(vantusSheet, destRect, srcRect, GraphicsUnit.Pixel);
            }
            else g.FillEllipse(Brushes.IndianRed, destRect);
        }

        // 5. РИСУЕМ ДИГГЕРА (Сложная сетка с анимациями)
        if (Properties.Resources.digger != null)
        {
            Image dSheet = Properties.Resources.digger;

            // Считаем размер одного кадра (учитывая и столбцы, и строки!)
            int dWidth = dSheet.Width / _diggerCols;
            int dHeight = dSheet.Height / _diggerRows;

            // Определяем, какую СТРОКУ (анимацию) сейчас играть
            int currentRow = 0; // По умолчанию бег
            if (_player.Condition == Condition.Run) currentRow = 0;
            else if (_player.Condition == Condition.Fall) currentRow = 1; // Замени на нужный номер строки (счет с нуля)
            else if (_player.Condition == Condition.Attached) currentRow = 2; // Замени на нужный номер

            // Определяем, какой СТОЛБЕЦ (кадр) сейчас вырезать
            int sourceX = _diggerCurrentFrame * dWidth;
            int sourceY = currentRow * dHeight;

            System.Drawing.Rectangle destRect = new System.Drawing.Rectangle(_player.Location.X, _player.Location.Y, 40, 40);
            System.Drawing.Rectangle srcRect = new System.Drawing.Rectangle(sourceX, sourceY, dWidth, dHeight);

            g.DrawImage(dSheet, destRect, srcRect, GraphicsUnit.Pixel);

            // Таймер для перебирания ногами
            if (_player.Condition == Condition.Run)
            {
                _diggerFrameTimer++;
                if (_diggerFrameTimer > 4)
                {
                    _diggerCurrentFrame = (_diggerCurrentFrame + 1) % _diggerCols;
                    _diggerFrameTimer = 0;
                }
            }
        }

        // 6. ЛИНИЯ ПРИЦЕЛА
        if (!_player.Projectile.IsActive && _player.Condition != Condition.Attached)
        {
            Pen aimPen = new Pen(Color.Lime, 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            int startX = _player.Location.X + 20;
            int startY = _player.Location.Y + 20;
            double aimRadians = _player.AimAngle * (Math.PI / 180.0);
            int endX = startX + (int)(Math.Cos(aimRadians) * 60);
            int endY = startY + (int)(Math.Sin(aimRadians) * 60);
            g.DrawLine(aimPen, startX, startY, endX, endY);
        }
    }
    private void Form1_Load(object sender, EventArgs e)
    {

    }
}