namespace Plunger.Models
{
    using Plunger.Models.Common;

    public class Tile
    {
        public Plunger.Point WorldLocation { get; }
        public int      Width  { get; }
        public int      Height { get; }
        public TileType Type   { get; }

        public Tile(int x, int y, int w, int h, TileType type = TileType.Platform)
        {
            WorldLocation = new Plunger.Point(x, y);
            Width  = w;
            Height = h;
            Type   = type;
        }

        public Rectangle GetBounds()
            => new Rectangle(WorldLocation.X, WorldLocation.Y, Width, Height);
    }
}
