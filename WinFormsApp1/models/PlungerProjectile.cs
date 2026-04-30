namespace Plunger.Models;

using System;
using Plunger.Models.Common;

public class PlungerProjectile
{
    
    private double _exactX;
    private double _exactY;

    private double _velocityX;
    private double _velocityY;

    public Point Location => new Point((int)_exactX, (int)_exactY);
    public bool IsActive { get; private set; }
    public float Speed { get; private set; } = 20f; // Присоска летит быстрее диггера

    private const int Size = 15;

    public void Launch(Point startLocation, double angleInDegrees)
    {
        _exactX = startLocation.X;
        _exactY = startLocation.Y;

        // Переводим градусы в радианы для математических функций
        double angleInRadians = angleInDegrees * (Math.PI / 180.0);

        // Вычисляем вектор скорости полета
        _velocityX = Math.Cos(angleInRadians) * Speed;
        _velocityY = Math.Sin(angleInRadians) * Speed;

        IsActive = true;
    }

    public void Update()
    {
        if (!IsActive) return;

        // Плавно обновляем координаты
        _exactX += _velocityX;
        _exactY += _velocityY;
    }

    public void Stop()
    {
        IsActive = false;
    }

    public Rectangle GetBounds()
    {
        return new Rectangle((int)_exactX, (int)_exactY, Size, Size);
    }
}