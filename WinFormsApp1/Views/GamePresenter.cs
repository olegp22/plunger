using Plunger; 
using Plunger.Models; 
using Plunger.Models.Common;
using Plunger.Views;
namespace Plunger.Presenters;


public class GamePresenter
{
    private readonly IMainView _view;
    private readonly Sucker _player;
    private readonly List<Coin> _coins;

    public GamePresenter(IMainView view, Sucker player, List<Coin> coins)
    {
        _view = view;
        _player = player;
        _coins = coins;

        _view.SetGameData(_player, _coins);
        _view.KeyPressed += OnKeyPressed;
        // Подписываемся на тик таймера формы
        _view.TimerTick += OnTimerTick;
    }


    private void CheckCoinCollisions()
    {
        // Указываем реальный визуальный размер 120x120 для проверки столкновений
        var playerBounds = _player.GetBounds(120, 120);

        foreach (var coin in _coins)
        {
            if (!coin.IsCollected && playerBounds.IntersectsWith(coin.GetBounds()))
            {
                coin.Collect();
                _player.AddCoin();
            }
        }
    }

    private void OnTimerTick()
    {
        if (_player.Projectile.IsActive)
        {
            _player.Projectile.Update();

            // ЛОГИКА "ПОТОЛКА": если присоска улетела выше Y=100, она прилипает
            if (_player.Projectile.Location.Y < 100)
            {
                // Здесь мы вручную меняем состояние игрока на Attached через рефлексию 
                // или просто добавив публичный сеттер в Sucker.cs для Condition
                // Для простоты предположим, что у вас есть доступ:
                // _player.SetAttached(); // Рекомендую добавить такой метод в Sucker.cs
            }

            // Если улетела слишком далеко (за экран) — сбрасываем
            if (_player.Projectile.Location.X > 2000 || _player.Projectile.Location.X < -500 ||
                _player.Projectile.Location.Y > 1000 || _player.Projectile.Location.Y < -500)
            {
                _player.Projectile.Stop();
            }
        }

        _player.UpdatePosition(1.0);
        CheckCoinCollisions();
        _view.RefreshView();
        _view.UpdateScore(_player.CoinsCollected);
    }
    

    

    private void OnKeyPressed(Keys key)
    {
        switch (key)
        {
            case Keys.A:
                _player.AimUp(); // Поворачиваем прицел вверх
                break;
            case Keys.D:
                _player.AimDown(); // Поворачиваем прицел вниз
                break;
            case Keys.W:
                _player.Shoot(); // Запускаем присоску по текущему углу
                break;
            case Keys.Space:
                _player.Detach(); // Отцепляемся и переходим в падение
                break;
        }
    }

}