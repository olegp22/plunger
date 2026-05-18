namespace Plunger.Models
{
    using Plunger.Models.Common;

    public class Platform
    {
        public Plunger.Point Location { get; }
        public int Width  { get; }
        public int Height { get; }

        public Platform(Plunger.Point location, int width, int height = 20)
        {
            Location = location;
            Width    = width;
            Height   = height;
        }

        public Rectangle GetBounds()
            => new Rectangle(Location.X, Location.Y, Width, Height);
    }

    public class PlatformList
    {
        public System.Collections.Generic.List<Platform> Items { get; }
            = new System.Collections.Generic.List<Platform>();

        public void Add(Platform p) => Items.Add(p);
    }
}
