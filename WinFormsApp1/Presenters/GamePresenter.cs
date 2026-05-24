using System;
using System.IO;
using System.Windows.Forms;
using Plunger.Models;
using Plunger.Views;

namespace Plunger.Presenters
{
    public class GamePresenter
    {
        private readonly IMainView _view;

        private Sucker? _player = null;
        private LevelData? _level = null;
        private Camera _camera = new Camera();

        private int _levelNum = 0;
        private int _levelTick = 0;

        public const int LevelDurationTicks = 3600; 

        private bool _aimUpHeld = false;
        private bool _aimDownHeld = false;

        public GamePresenter(IMainView view)
        {
            _view = view;
            _view.TimerTick += OnTimerTick;
            _view.KeyDown += OnKeyDown;
            _view.KeyUp += OnKeyUp;
            _view.MouseClick += OnMouseClick;
            _view.ShowMenu();
        }

        public void StartLevel(int level)
        {
            _levelNum = level;
            _levelTick = 0;
            _aimUpHeld = _aimDownHeld = false;
            _level = (level == 1) ? LevelBuilder.BuildLevel1()
                                   : (level == 2 ? LevelBuilder.BuildLevel2() : LevelParser.Parse(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "levels", "level3.txt")));
            _player = new Sucker(_level.PlayerStart);
            _player.RunSpeed = (level == 1) ? GameConfig.RunSpeedLevel1 : GameConfig.RunSpeedLevel2;
            _player.JumpVY = (level == 1) ? GameConfig.JumpVYLevel1 : GameConfig.JumpVYLevel2;
            _player.CanInvertGravity = (level == 3);
            _player.EnableGrapple = (level != 3);
            _player.CanGrappleGround = true;

            _camera = new Camera();
            _camera.SetSpeed((level == 1) ? GameConfig.CameraSpeedLevel1 : GameConfig.CameraSpeedLevel2);
            _camera.Reset(_player.Location.X);

            _view.TotalCoins = _level.Coins.Count;
            _view.LevelTicksRemaining = LevelDurationTicks;
            _view.SetLevel(_player, _level, _camera);
            _view.ShowGame();
        }

        private void OnPlayerStateChanged()
        {
            if (_player == null) return;
            _view.UpdateScore(_player.CoinsCollected);
            _view.RefreshView();
        }

        public int LevelTicksRemaining => LevelDurationTicks - _levelTick;
        public int LastUpdateMs { get; private set; } = 0;

        private void OnTimerTick()
        {
            if (_levelNum == 0 || _player == null || _level == null) return;

            if (_aimUpHeld) _player.AimUp();
            if (_aimDownHeld) _player.AimDown();

            var sw = System.Diagnostics.Stopwatch.StartNew();
            _player.UpdatePosition(_level);
            sw.Stop();
            LastUpdateMs = (int)sw.ElapsedMilliseconds;

            _camera.Update(_player.Location.X, _level.WorldWidth, LevelBuilder.ScreenW);

            
            int playerRightEdge = _player.Location.X + Sucker.SpriteW;
            if (playerRightEdge < _camera.ScrollX)
            {
                _player.Kill();
                _levelNum = 0;
                _aimUpHeld = _aimDownHeld = false;
                _view.ShowDeath();
                return;
            }

            var pb = _player.GetBounds();
            foreach (var coin in _level.Coins)
                if (!coin.IsCollected && pb.IntersectsWith(coin.GetBounds()))
                { coin.Collect(); _player.AddCoin(); }

            if (_level.Flags != null)
            {
                foreach (var flag in _level.Flags)
                {
                    if (pb.IntersectsWith(flag))
                    {
                        int coins = _player.CoinsCollected;
                        int total = _level.Coins.Count;
                        _levelNum = 0;
                        _aimUpHeld = _aimDownHeld = false;
                        _view.ShowVictory(coins, total);
                        return;
                    }
                }
            }

            _view.LevelTicksRemaining = LevelDurationTicks - _levelTick;

            _levelTick++;

            try { _view.RefreshView(); } catch { }

            if (_levelTick >= LevelDurationTicks)
            {
                int coins = _player.CoinsCollected;
                int total = _level.Coins.Count;
                _levelNum = 0;
                _aimUpHeld = _aimDownHeld = false;
                _view.ShowVictory(coins, total);
            }
        }

        private void OnKeyDown(Keys key)
        {
            if (key == Keys.Escape)
            {
                _levelNum = 0;
                _aimUpHeld = _aimDownHeld = false;
                _view.ShowMenu();
                return;
            }
            if (_levelNum == 0 || _player == null) return;

            switch (key)
            {
                case Keys.A: _aimUpHeld = true; break;
                case Keys.D: _aimDownHeld = true; break;
                case Keys.W: _player.Shoot(); break;
                case Keys.Space:
                    if (_player.Condition == Condition.Run)
                        _player.Jump();
                    else
                        _player.Detach();
                    break;
            }
        }

        private void OnMouseClick(MouseButtons button)
        {
            if (_levelNum == 0 || _player == null) return;
            if (button == MouseButtons.Left && _levelNum == 3)
            {
                if (_player.CanInvertGravity)
                    _player.ToggleGravity();
            }
        }

        private void OnKeyUp(Keys key)
        {
            switch (key)
            {
                case Keys.A: _aimUpHeld = false; break;
                case Keys.D: _aimDownHeld = false; break;
            }
        }
    }
}