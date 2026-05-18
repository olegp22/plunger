namespace Plunger.Models
{
    using Plunger.Models.Common;

    public class Coin
    {
        public Plunger.Point Location  { get; private set; }
        public int            Value     { get; private set; }
        public bool           IsCollected { get; private set; }

        private const int Size = 24;

        public Coin(Plunger.Point location, int value = 1)
        {
            Location    = location;
            Value       = value;
            IsCollected = false;
        }

        public Rectangle GetBounds()
            => new Rectangle(Location.X, Location.Y, Size, Size);

        public void Collect() => IsCollected = true;
    }
}
