namespace WinFormsApp1;



using Plunger.Views;

public partial class Form1 : Form, IMainView
{
    public event Action TimerTick;

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

    protected override void OnPaint(PaintEventArgs e)
    {
        // Здесь мы будем рисовать игрока и монетки через e.Graphics
        // Но для этого Презентер должен как-то передать данные
    }

    private void Form1_Load(object sender, EventArgs e)
    {

    }
}