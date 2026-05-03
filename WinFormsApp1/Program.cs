using Plunger;
using Plunger.Models;
using Plunger.Models.Common;
using Plunger.Presenters;
using System.Collections.Generic;

namespace WinFormsApp1;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // 1. Создаем игрока (Диггера)[cite: 9]
        // Начальная позиция (100, 200), скорость 5, состояние - бег[cite: 11, 12]
        var player = new Sucker(new Plunger.Point(100, 200), 5, Condition.Run);

        // 2. Создаем список монеток на уровне[cite: 7]
        var coins = new List<Coin>
        {
            new Coin(new Plunger.Point(400, 200)),
            new Coin(new Plunger.Point(600, 150)),
            new Coin(new Plunger.Point(300, 350)),
            new Coin(new Plunger.Point(700, 100))
        };

        // 3. Создаем форму (View)
        var form = new Form1();

        // 4. Создаем Презентер, который свяжет форму и данные
        // Важно: Презентер должен "жить" всё время работы приложения
        var presenter = new GamePresenter(form, player, coins);

        // 5. Запускаем приложение
        Application.Run(form);
    }
}