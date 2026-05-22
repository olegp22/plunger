namespace Plunger
{
    public record Point(int X = 0, int Y = 0)
    {
        public static readonly Point Null = new(-1, -1);
        public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
        public static Point operator -(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);
        public bool IsNull    => X == -1 && Y == -1;
        public bool HasValue  => !IsNull;
        public override string ToString() => $"{{X={X},Y={Y}}}";
    }
}
