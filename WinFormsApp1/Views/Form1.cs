using Plunger;
using Plunger.Models;
using Plunger.Views;


namespace WinFormsApp1;

public partial class Form1 : Form, IMainView
{
    public event Action TimerTick;
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

        // Если данные еще не загрузились, ничего не рисуем
        if (_player == null || _coins == null) return;

        Graphics g = e.Graphics;
        // Включаем сглаживание, чтобы круги и линии были ровными, без "лесенок"
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // 1. Рисуем фон (темно-серый цвет пещеры/земли)
        g.Clear(Color.FromArgb(40, 40, 40));

        // 2. Рисуем монетки (золотые круги)
        Brush goldBrush = new SolidBrush(Color.Gold);
        foreach (var coin in _coins)
        {
            if (!coin.IsCollected)
            {
                var bounds = coin.GetBounds();
                g.FillEllipse(goldBrush, bounds.Left, bounds.Top, bounds.Width, bounds.Height);
            }
        }

        // 3. Рисуем трос (линию), если присоска летит или прицеплена
        if (_player.Projectile.IsActive || _player.Condition == Condition.Attached)
        {
            Pen ropePen = new Pen(Color.White, 2);

            // Центр диггера (мы условились, что его размер 40x40)
            int playerCenterX = _player.Location.X + 20;
            int playerCenterY = _player.Location.Y + 20;

            // Центр присоски
            var projBounds = _player.Projectile.GetBounds();
            int projCenterX = projBounds.Left + projBounds.Width / 2;
            int projCenterY = projBounds.Top + projBounds.Height / 2;

            g.DrawLine(ropePen, playerCenterX, playerCenterY, projCenterX, projCenterY);
        }

        // 4. Рисуем присоску (красный круг)
        if (_player.Projectile.IsActive || _player.Condition == Condition.Attached)
        {
            Brush plungerBrush = new SolidBrush(Color.IndianRed);
            var pBounds = _player.Projectile.GetBounds();
            g.FillEllipse(plungerBrush, pBounds.Left, pBounds.Top, pBounds.Width, pBounds.Height);
        }

        // 5. Рисуем Диггера (синий квадрат)
        Brush playerBrush = new SolidBrush(Color.DodgerBlue);
        g.FillRectangle(playerBrush, _player.Location.X, _player.Location.Y, 40, 40);

        // 6. Рисуем линию прицела, когда диггер просто бежит (чтобы понимать, куда полетит крюк)
        if (!_player.Projectile.IsActive && _player.Condition != Condition.Attached)
        {
            Pen aimPen = new Pen(Color.Lime, 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            int startX = _player.Location.X + 20;
            int startY = _player.Location.Y + 20;

            int aimLength = 60; // Длина прицела
            double aimRadians = _player.AimAngle * (Math.PI / 180.0);

            // Вычисляем конец линии прицела с помощью синуса и косинуса
            int endX = startX + (int)(Math.Cos(aimRadians) * aimLength);
            int endY = startY + (int)(Math.Sin(aimRadians) * aimLength);

            g.DrawLine(aimPen, startX, startY, endX, endY);
        }
    }

    private void Form1_Load(object sender, EventArgs e)
    {

    }
}