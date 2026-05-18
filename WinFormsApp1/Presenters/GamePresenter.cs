// Presenters/GamePresenter.cs
using System.Collections.Generic;
using System.Windows.Forms;
using Plunger.Models;
using Plunger.Views;

namespace Plunger.Presenters
{
    /// <summary>
    /// Owns game logic and wires View events to model updates (MVP Presenter).
    /// Does NOT reference Form1 — only IMainView.
    /// </summary>
    public class GamePresenter
    {
        private readonly IMainView _view;

        private Sucker       _player    = null!;
        private List<Coin>   _coins     = null!;
        private PlatformList _platforms = new PlatformList();

        private int _level     = 0;   // 0 = menu
        private int _levelTick = 0;

        public const int LevelDurationTicks = 3600; // ~60 s at 60 fps

        // ── Construction ──────────────────────────────────────────────────────
        public GamePresenter(IMainView view)
        {
            _view = view;
            _view.TimerTick  += OnTimerTick;
            _view.KeyPressed += OnKeyPressed;
            _view.ShowMenu();
        }

        // ── Called by View when a menu button is clicked ──────────────────────
        public void StartLevel(int level)
        {
            _level     = level;
            _levelTick = 0;

            if (level == 1) BuildLevel1();
            else            BuildLevel2();

            _view.SetGameData(_player, _coins, _platforms);
            _view.ShowGame();
        }

        // ── Exposed to View for the HUD timer ─────────────────────────────────
        public int LevelTicksRemaining => LevelDurationTicks - _levelTick;

        // ── Level builders ────────────────────────────────────────────────────
        private void BuildLevel1()
        {
            _player = new Sucker(new Plunger.Point(100, 380), 9, Condition.Run);
            _player.CanGrappleGround = false;
            _platforms = new PlatformList();

            _coins = new List<Coin>
            {
                // Floor coins
                new Coin(new Plunger.Point(250, 345)),
                new Coin(new Plunger.Point(380, 345)),
                new Coin(new Plunger.Point(510, 345)),
                // Mid-air
                new Coin(new Plunger.Point(450, 250)),
                new Coin(new Plunger.Point(600, 220)),
                new Coin(new Plunger.Point(750, 200)),
                new Coin(new Plunger.Point(900, 230)),
                // Near ceiling
                new Coin(new Plunger.Point(320,  90)),
                new Coin(new Plunger.Point(560,  80)),
                new Coin(new Plunger.Point(800,  70)),
                new Coin(new Plunger.Point(1050, 85)),
                new Coin(new Plunger.Point(1200, 90)),
                // Bonus clusters
                new Coin(new Plunger.Point(660, 150)),
                new Coin(new Plunger.Point(680, 130)),
                new Coin(new Plunger.Point(700, 150)),
                new Coin(new Plunger.Point(980, 160)),
                new Coin(new Plunger.Point(1000,140)),
                new Coin(new Plunger.Point(1020,160)),
            };
        }

        private void BuildLevel2()
        {
            _player = new Sucker(new Plunger.Point(100, 380), 9, Condition.Run);
            _player.CanGrappleGround = true;

            _platforms = new PlatformList();
            var defs = new (int x, int y, int w)[]
            {
                (200,300,150),(420,240,150),(620,180,130),
                (820,280,150),(1000,200,150),(1150,300,120),
                (300,150,100),(550,100,100),(760,130,100),(950,100,120),
            };
            foreach (var (x, y, w) in defs)
                _platforms.Add(new Platform(new Plunger.Point(x, y), w));

            _coins = new List<Coin>();
            foreach (var plat in _platforms.Items)
            {
                var pb = plat.GetBounds();
                int midX = pb.Left + pb.Width / 2;
                _coins.Add(new Coin(new Plunger.Point(midX - 15, pb.Top - 30)));
                _coins.Add(new Coin(new Plunger.Point(midX + 15, pb.Top - 30)));
            }
            foreach (var x in new[] { 350, 500, 700, 900, 1100 })
                _coins.Add(new Coin(new Plunger.Point(x, 55)));
            foreach (var x in new[] { 260, 480, 700 })
                _coins.Add(new Coin(new Plunger.Point(x, 345)));
        }

        // ── Game loop ─────────────────────────────────────────────────────────
        private void OnTimerTick()
        {
            if (_level == 0) return;

            _levelTick++;
            _player.UpdatePosition(1.0, _platforms);
            CollectCoins();
            _view.UpdateScore(_player.CoinsCollected);
            _view.RefreshView();

            if (_levelTick >= LevelDurationTicks)
            {
                _level = 0;
                _view.ShowMenu();
            }
        }

        private void CollectCoins()
        {
            var pb = _player.GetBounds(100, 120);
            foreach (var coin in _coins)
                if (!coin.IsCollected && pb.IntersectsWith(coin.GetBounds()))
                { coin.Collect(); _player.AddCoin(); }
        }

        // ── Key input ─────────────────────────────────────────────────────────
        private void OnKeyPressed(Keys key)
        {
            if (_level == 0) return;
            switch (key)
            {
                case Keys.A:     _player.AimUp();   break;
                case Keys.D:     _player.AimDown();  break;
                case Keys.W:     _player.Shoot();    break;
                case Keys.Space: _player.Detach();   break;
            }
        }
    }
}
