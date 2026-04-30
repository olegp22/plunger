namespace Plunger.Models.Common;

public readonly struct Rectangle
{
    public Point Location { get; }
    public int Width { get; }
    public int Height { get; }

    public Rectangle(int x, int y, int width, int height)
    {
        Location = new Point(x, y);
        Width = width;
        Height = height;
    }

    public int Left => Location.X;
    public int Top => Location.Y;
    public int Right => Location.X + Width;
    public int Bottom => Location.Y + Height;

    public bool IntersectsWith(Rectangle other)
    {
        return Left < other.Right && Right > other.Left &&
               Top < other.Bottom && Bottom > other.Top;
    }
}