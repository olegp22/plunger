namespace Plunger.Models
{
    using System;
    using System.IO;

    public static class LevelBuilder
    {
        public const int ScreenW = 1315;
        public const int ScreenH = 558;
        public const int WallW = LevelParser.CellW;  // ширина левой стены = ширина ячейки

        // Папка с уровнями: рядом с exe
        private static string LevelsDir =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "levels");

        public static LevelData BuildLevel1()
            => LevelParser.Parse(Path.Combine(LevelsDir, "level1.txt"));

        public static LevelData BuildLevel2()
            => LevelParser.Parse(Path.Combine(LevelsDir, "level2.txt"));

        // ── Хелперы для получения границ мира из данных уровня ───────────────
        // Ищем самый верхний Ceiling тайл и самый нижний Ground тайл.
        // Если тайлов нет — возвращаем дефолты.

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
            // fallback: нижняя граница мира
            return maxTop == int.MinValue ? level.WorldHeight - LevelParser.CellH : maxTop;
        }

        // Для обратной совместимости с хардкодными ссылками в GamePresenter
        public static int CeilBot => LevelParser.CellH;     // приблизительно
        public static int FloorTop => ScreenH - LevelParser.CellH * 2;
    }
}