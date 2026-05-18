namespace Plunger.Models.Common
{
    public readonly struct Rectangle
    {
        public Plunger.Point Location { get; }
        public int Width  { get; }
        public int Height { get; }

        public Rectangle(int x, int y, int width, int height)
        {
            Location = new Plunger.Point(x, y);
            Width    = width;
            Height   = height;
        }

        public int X      => Location.X;
        public int Y      => Location.Y;
        public int Left   => Location.X;
        public int Top    => Location.Y;
        public int Right  => Location.X + Width;
        public int Bottom => Location.Y + Height;

        public bool IntersectsWith(Rectangle o)
            => Left < o.Right && Right > o.Left && Top < o.Bottom && Bottom > o.Top;

        public bool Contains(Plunger.Point p)
            => p.X >= Left && p.X <= Right && p.Y >= Top && p.Y <= Bottom;
    }
}
