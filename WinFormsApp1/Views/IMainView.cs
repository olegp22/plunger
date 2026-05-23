// Views/IMainView.cs
using System;
using System.Windows.Forms;
using Plunger.Models;

namespace Plunger.Views
{
    public interface IMainView
    {
        // View → Presenter
        event Action TimerTick;
        event Action<Keys> KeyDown;
        event Action<Keys> KeyUp;
        // Mouse clicks forwarded to presenter (button: Left/Right/Middle)
        event Action<MouseButtons> MouseClick;

        // Presenter → View
        void SetLevel(Sucker player, LevelData level, Camera camera);
        void ShowMenu();
        void ShowGame();
        void ShowDeath();                        // экран смерти
        void ShowVictory(int coins, int total);  // экран победы
        void UpdateScore(int score);
        void RefreshView();

        // HUD-данные
        int LevelTicksRemaining { get; set; }
        int TotalCoins { get; set; }
    }
}