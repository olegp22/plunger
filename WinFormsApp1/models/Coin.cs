namespace Plunger.Models;

using Plunger.Models.Common;

public class Coin
{
    public Point Location { get; private set; }
    public int Value { get; private set; }
    public bool IsCollected { get; private set; }

    // Радиус или размер монетки 
    private const int Size = 20;

    public Coin(Point location, int value = 1)
    {
        Location = location;
        Value = value;
        IsCollected = false;
    }

    /// Возвращает хитбокс монетки для проверки столкновения с игроком
    public Plunger.Models.Common.Rectangle GetBounds()
    {
        return new Plunger.Models.Common.Rectangle(Location.X, Location.Y, Size, Size);
    }

    /// Помечает монетку как собранную
    public void Collect()
    {
        IsCollected = true;
    }
}