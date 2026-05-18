// Views/IMainView.cs
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Plunger.Models;

namespace Plunger.Views
{
    /// <summary>
    /// Contract between Presenter and View (MVP pattern).
    /// Presenter only knows this interface — never the concrete Form.
    /// </summary>
    public interface IMainView
    {
        // ── View → Presenter (events) ─────────────────────────────────────────
        event Action          TimerTick;
        event Action<Keys>    KeyPressed;

        // ── Presenter → View (commands) ───────────────────────────────────────
        void RefreshView();
        void UpdateScore(int score);
        void SetGameData(Sucker player, List<Coin> coins, PlatformList platforms);
        void ShowMenu();
        void ShowGame();

        // ── View exposes HUD data so OnPaint can read it cleanly ──────────────
        // (Presenter writes these via UpdateHud; View reads them in DrawHUD)
        int  LevelTicksRemaining { get; }
        int  TotalCoins          { get; }
    }
}
