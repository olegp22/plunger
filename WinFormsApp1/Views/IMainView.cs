namespace Plunger.Views;

public interface IMainView
{
    // Событие, которое форма будет "поднимать" при каждом тике таймера
    event Action TimerTick;

    // Метод для принудительной перерисовки экрана
    void RefreshView();

    // Метод для обновления текста счета на форме
    void UpdateScore(int score);
}