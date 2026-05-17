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

    // ── Constructor ──────────────────────────────────────────────────────────
    public GamePresenter(IMainView view, Sucker player, List<Coin> coins)
    {
        _view = view;
        _player = player;
        _coins = coins;

        _view.SetGameData(_player, _coins);
        _view.KeyPressed += OnKeyPressed;
        _view.TimerTick += OnTimerTick;
    }

    // ── Game loop tick ───────────────────────────────────────────────────────
    private void OnTimerTick()
    {
        // UpdatePosition handles ALL physics internally:
        //   • projectile flight + ceiling attach check
        //   • out-of-range auto-return (no freeze)
        //   • pull physics toward plunger
        //   • auto-detach when digger reaches plunger
        //   • floor landing
        //   • somersault flag
        _player.UpdatePosition(1.0);

        // Coins are only collected by the digger's body — the projectile is excluded.
        CheckCoinCollisions();

        _view.UpdateScore(_player.CoinsCollected);
        _view.RefreshView();
    }

    // ── Coin collision ───────────────────────────────────────────────────────
    /// <summary>
    /// Coins are collected ONLY when the digger's 120×120 body hitbox intersects them.
    /// The flying plunger is strictly ignored here (requirement).
    /// </summary>
    private void CheckCoinCollisions()
    {
        // Sprite is drawn at 120 px wide, 150 px tall; use the same width for the hitbox.
        // Using 120×120 centres the hitbox on the upper body as the requirement states.
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

    // ── Input handling ───────────────────────────────────────────────────────
    private void OnKeyPressed(Keys key)
    {
        switch (key)
        {
            case Keys.A:
                _player.AimUp();        // Rotate aim upward (negative Y in screen space)
                break;
            case Keys.D:
                _player.AimDown();      // Rotate aim downward
                break;
            case Keys.W:
                _player.Shoot();        // Fire plunger / cancel in-flight / detach if attached
                break;
            case Keys.Space:
                _player.Detach();       // Force-detach while hanging
                break;
        }
    }
}
