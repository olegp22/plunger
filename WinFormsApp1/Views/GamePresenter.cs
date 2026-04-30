using Plunger; // Чтобы видеть Sucker, Point и Condition
using Plunger.Models; // Чтобы видеть Coin
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

        // Подписываемся на тик таймера формы
        _view.TimerTick += OnTimerTick;
    }

    private void OnTimerTick()
    {
        // 1. Двигаем игрока (deltaTime пока упростим до 1)
        _player.UpdatePosition(1.0);

        // 2. Проверяем столкновения с монетками
        CheckCoinCollisions();

        // 3. Просим форму обновиться
        _view.RefreshView();
        _view.UpdateScore(_player.CoinsCollected);
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
}