using System;
using Plunger.Models;
using Xunit;

namespace WinFormsApp1.Tests
{
    public class SuckerTests
    {
        [Fact]
        public void AddCoin_IncrementsCoinsAndNotifies()
        {
            var s = new Sucker(new Plunger.Point(100, 100));
            int calls = 0;
            s.StateChanged += () => calls++;
            s.AddCoin();
            Assert.Equal(1, s.CoinsCollected);
            Assert.Equal(1, calls);
        }

        [Fact]
        public void Jump_FromRun_EnqueuesJump()
        {
            var s = new Sucker(new Plunger.Point(0, 0));
            s.Jump();
            var level = new LevelData(800, 600, new Plunger.Point(0, 0));
            level.AddTile(0, 580, 800, 20, TileType.Floor);
            s.UpdatePosition(level);
            Assert.NotEqual(Condition.Run, s.Condition);
        }

        [Fact]
        public void ToggleGravity_InvertsGravityScale()
        {
            var s = new Sucker(new Plunger.Point(0, 0));
            s.CanInvertGravity = true;
            double prev = s.GravityScale;
            s.ToggleGravity();
            Assert.Equal(-prev, s.GravityScale);
        }

        [Fact]
        public void Shoot_LaunchesProjectile()
        {
            var s = new Sucker(new Plunger.Point(50, 50));
            s.Shoot();
            Assert.True(s.Projectile.IsActive);
        }
    }
}
