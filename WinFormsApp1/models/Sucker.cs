namespace Plunger;

public class Sucker
{
    public Point Location;
    public int Speed;
    public Condition Condition;
    public Cannon Cannon;
    public int CoinsCollected { get; private set; }

    public Sucker(Point location, int speed, Condition condition, Cannon cannon = Cannon.Plunger)
    {
        Location = location;
        Speed = speed;
        Condition = condition;
        Cannon = cannon;
        CoinsCollected = 0;
    }

    public void AddCoin()
    {
        CoinsCollected++;
    }

    public void UpdatePosition(double deltaTime)
    {
        if (this.Condition == Condition.Run)
            Location += new Point(Speed, 0);

        else if (this.Condition == Condition.Attached)
            Location += new Point(Speed, Speed / 2);//на приктике стоит определить лучшею формулу определения изменения по y

        else if (this.Condition == Condition.Fall)
            Location += new Point(Speed, -Speed / 2);

        else if (this.Condition == Condition.ToStand)
            Location += new Point(0,0);
    }

    public void Reset(Point startLocation)
    {
        Location = startLocation;
        CoinsCollected = 0;
        Condition = Condition.Death;
    }

    public Plunger.Models.Common.Rectangle GetBounds(int w, int h)
    {
        return new Plunger.Models.Common.Rectangle(Location.X, Location.Y, w, h);
    }

    public void Die() { this.Condition = Condition.Death; }

    
}
