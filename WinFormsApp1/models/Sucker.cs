namespace Plunger.Models;

using System;
using Plunger.Models.Common;

public class Sucker
{
    public Point Location { get; private set; }
    public int Speed { get; private set; }
    public Condition Condition { get; private set; }
    public Cannon Cannon { get; private set; }
    public int CoinsCollected { get; private set; }

    
    public double AimAngle { get; private set; } = 0; // Угол в градусах
    public PlungerProjectile Projectile { get; private set; }

    public Sucker(Point location, int speed, Condition condition, Cannon cannon = Cannon.Plunger)
    {
        Location = location;
        Speed = speed;
        Condition = condition;
        Cannon = cannon;
        CoinsCollected = 0;
        Projectile = new PlungerProjectile();
    }

    // Управление углом (клавиши W и S)
    public void AimUp() { AimAngle -= 5; } // В WinForms Y идет вниз, поэтому минус - это вверх
    public void AimDown() { AimAngle += 5; }

    // Отцепление (клавиша Пробел)
    public void Detach()
    {
        if (Condition == Condition.Attached)
        {
            Condition = Condition.Fall; // Начинаем падать после отцепления
            Projectile.Stop();
        }
    }

    // Выстрел
    public void Shoot()
    {
        if (!Projectile.IsActive && Condition != Condition.Attached)
        {
            Projectile.Launch(Location, AimAngle);
        }
    }

    public void UpdatePosition(double deltaTime)
    {
        if (Condition == Condition.Run)
        {
            Location += new Point(Speed, 0);
        }
        else if (Condition == Condition.Attached)
        {
            // Логика подтягивания диггера к присоске
            double dx = Projectile.Location.X - Location.X;
            double dy = Projectile.Location.Y - Location.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance > Speed)
            {
                // Двигаемся по вектору в сторону присоски с заданной скоростью
                int moveX = (int)((dx / distance) * Speed);
                int moveY = (int)((dy / distance) * Speed);
                Location += new Point(moveX, moveY);
            }
            else
            {
                // Если мы оказались почти вплотную к присоске, прилипаем к ней
                Location = Projectile.Location;
                // Тут можно либо оставлять его висеть, либо принудительно вызывать Detach()
            }
        }
        else if (Condition == Condition.Fall)
        {
            Location += new Point(0, Speed / 2); // Простая гравитация
        }
    }
}