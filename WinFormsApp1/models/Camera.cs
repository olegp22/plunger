// models/Camera.cs
// Камера с автоматическим скроллингом.
// Скорости настраиваются через GameConfig.
namespace Plunger.Models
{
    using System;

    public class Camera
    {
        public int ScrollX { get; private set; } = 0;

        private double _speed = GameConfig.CameraSpeedLevel1;
        private double _exactX = 0;
        private double _targetX = 0;

        // Установить скорость под конкретный уровень
        public void SetSpeed(double speed) => _speed = speed;

        public void Update(int playerWorldX, int worldWidth, int screenWidth)
        {
            // 1. Автоскролл
            _exactX += _speed;
            _targetX += _speed;

            // 2. Если диггер далеко справа — ускоряем камеру
            int screenX = playerWorldX - (int)_exactX;
            if (screenX > GameConfig.CameraSafeRight)
                _targetX += (screenX - GameConfig.CameraSafeRight) * GameConfig.CameraChaseK;

            // 3. Lerp
            _exactX += (_targetX - _exactX) * 0.18;

            // 4. Зажим по границам мира
            _exactX = Math.Max(0, Math.Min(_exactX, worldWidth - screenWidth));
            _targetX = Math.Max(0, Math.Min(_targetX, worldWidth - screenWidth));

            ScrollX = (int)_exactX;
        }

        public void Reset(int playerWorldX)
        {
            _exactX = Math.Max(0, playerWorldX - 200);
            _targetX = _exactX;
            ScrollX = (int)_exactX;
        }
    }
}