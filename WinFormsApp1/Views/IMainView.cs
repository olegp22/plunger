using System;
using System.Windows.Forms;
using Plunger.Models;

namespace Plunger.Views
{
    public interface IMainView
    {
        event Action TimerTick;
        event Action<Keys> KeyDown;
        event Action<Keys> KeyUp;
        event Action<MouseButtons> MouseClick;

        void SetLevel(Sucker player, LevelData level, Camera camera);
        void ShowMenu();
        void ShowGame();
        void ShowDeath();                        
        void ShowVictory(int coins, int total);  
        void UpdateScore(int score);
        void RefreshView();

        int LevelTicksRemaining { get; set; }
        int TotalCoins { get; set; }
    }
}