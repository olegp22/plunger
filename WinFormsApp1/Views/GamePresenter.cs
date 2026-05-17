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
        var playerBounds = _player.GetBounds(40, 40);

        foreach (var coin in _coins)
        {
            if (!coin.IsCollected && playerBounds.IntersectsWith(coin.GetBounds()))
            {
                coin.Collect();
                _player.AddCoin();
            }
        }
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

    private void OnTimerTick()
    {
        // 1. Обновляем полет присоски
        if (_player.Projectile.IsActive)
        {
            _player.Projectile.Update();

            // Если присоска улетела за пределы экрана, отменяем выстрел
            if (_player.Projectile.Location.X > 800 || _player.Projectile.Location.X < 0 ||
                _player.Projectile.Location.Y > 600 || _player.Projectile.Location.Y < 0)
            {
                _player.Projectile.Stop();
            }
        }

        // 2. Двигаем игрока (бег, подтягивание на тросе или падение)
        _player.UpdatePosition(1.0);

        // 3. Проверяем столкновения с монетками
        CheckCoinCollisions();

        // 4. Перерисовываем экран и обновляем счетчик монет
        _view.RefreshView();
        _view.UpdateScore(_player.CoinsCollected);
    }
}