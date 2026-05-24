namespace Plunger.Models
{
    using System;
    using System.IO;

    public static class LevelBuilder
    {
        public const int ScreenW = 1315;
        public const int ScreenH = 558;
        public const int WallW = LevelParser.CellW;  

        private static string LevelsDir =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "levels");

        public static LevelData BuildLevel1()
            => LevelParser.Parse(Path.Combine(LevelsDir, "level1.txt"));

        public static LevelData BuildLevel2()
            => LevelParser.Parse(Path.Combine(LevelsDir, "level2.txt"));

        

        public static int GetCeilBot(LevelData level)
        {
            int minBot = int.MaxValue;
            foreach (var t in level.Tiles)
                if (t.Type == TileType.Ceiling)
                    minBot = Math.Min(minBot, t.GetBounds().Bottom);
            return minBot == int.MaxValue ? LevelParser.CellH : minBot;
        }

        public static int GetFloorTop(LevelData level)
        {
            int maxTop = int.MinValue;
            foreach (var t in level.Tiles)
                if (t.Type == TileType.Ground || t.Type == TileType.Floor)
                    maxTop = Math.Max(maxTop, t.GetBounds().Top);
            return maxTop == int.MinValue ? level.WorldHeight - LevelParser.CellH : maxTop;
        }

        public static int CeilBot => LevelParser.CellH;     
        public static int FloorTop => ScreenH - LevelParser.CellH * 2;
    }
}