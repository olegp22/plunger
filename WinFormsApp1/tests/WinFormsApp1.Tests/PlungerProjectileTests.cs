using System;
using Plunger.Models;
using Xunit;

namespace WinFormsApp1.Tests
{
    public class PlungerProjectileTests
    {
        [Fact]
        public void Launch_SetsActiveAndVelocity()
        {
            var p = new PlungerProjectile();
            var start = new Plunger.Point(10, 20);
            p.Launch(start, 0);
            Assert.True(p.IsActive);
            var before = p.Location;
            p.Update();
            var after = p.Location;
            Assert.NotEqual(before, after);
        }

        [Fact]
        public void Stop_Deactivates()
        {
            var p = new PlungerProjectile();
            p.Launch(new Plunger.Point(0,0), 45);
            p.Stop();
            Assert.False(p.IsActive);
        }
    }
}
