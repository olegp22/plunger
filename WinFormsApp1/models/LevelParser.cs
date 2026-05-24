// models/LevelParser.cs
// Формат карты:
//   #   — блок (тип определяется по положению: верх=Ceiling, низ=Ground, середина=Platform)
//   |   — вертикальная стена (Wall)
//   C   — монета
//   S   — старт игрока
//   пробел — пусто
//
// Правила определения типа горизонтального блока #:
//   Первые 2 строки        → Ceiling
//   Последние 2 строки     → Ground
//   Всё остальное          → Platform (можно стоять, гrapple)


using System;
using System.IO;

namespace Plunger.Models
{
    public static class LevelParser
    {
        public const int CellW = 60;
        public const int CellH = 35;

        public static LevelData Parse(string filePath)
        {
            string[] rawLines = File.ReadAllLines(filePath);
            if (rawLines.Length == 0)
                throw new InvalidDataException($"Level file is empty: {filePath}");

            int maxCols = 0;
            foreach (var l in rawLines) maxCols = Math.Max(maxCols, l.Length);

            // Нормализуем сетку
            char[,] grid = new char[rawLines.Length, maxCols];
            for (int r = 0; r < rawLines.Length; r++)
                for (int c = 0; c < maxCols; c++)
                    grid[r, c] = c < rawLines[r].Length ? rawLines[r][c] : ' ';

            int rows = rawLines.Length;
            int cols = maxCols;
            int worldW = cols * CellW;
            int worldH = rows * CellH;

            // Стартовая позиция
            var startPos = new Plunger.Point(CellW * 2, worldH - CellH * 3);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    if (grid[r, c] == 'S')
                    { startPos = new Plunger.Point(c * CellW, r * CellH); goto foundStart; }
        foundStart:

            var d = new LevelData(worldW, worldH, startPos);
            var visited = new bool[rows, cols];

            // ── Проход 1: горизонтальные run из # ────────────────────────────
            for (int r = 0; r < rows; r++)
            {
                int c = 0;
                while (c < cols)
                {
                    if (grid[r, c] == '#' && !visited[r, c])
                    {
                        // Считаем длину горизонтального run
                        int len = 0;
                        while (c + len < cols && grid[r, c + len] == '#') len++;

                        if (len >= 2)
                        {
                            TileType type = ClassifyRow(r, rows);
                            d.AddTile(c * CellW, r * CellH, len * CellW, CellH, type);
                            for (int i = 0; i < len; i++) visited[r, c + i] = true;
                            c += len;
                            continue;
                        }
                    }
                    c++;
                }
            }

            // ── Проход 2: вертикальные | и одиночные # → Wall ─────────────
            for (int c = 0; c < cols; c++)
            {
                int r = 0;
                while (r < rows)
                {
                    char ch = grid[r, c];
                    if ((ch == '#' || ch == '|') && !visited[r, c])
                    {
                        int len = 0;
                        while (r + len < rows)
                        {
                            char next = grid[r + len, c];
                            if ((next == '#' || next == '|') && !visited[r + len, c])
                                len++;
                            else break;
                        }
                        d.AddTile(c * CellW, r * CellH, CellW, len * CellH, TileType.Wall);
                        for (int i = 0; i < len; i++) visited[r + i, c] = true;
                        r += len;
                        continue;
                    }
                    r++;
                }
            }

            // ── Проход 3: монеты ──────────────────────────────────────────────
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    if (grid[r, c] == 'C')
                    {
                        int cx = c * CellW + CellW / 2 - 12;
                        int cy = r * CellH + CellH / 2 - 12;
                        d.AddCoin(cx, cy);
                    }

            // ── Проход 4: флаги 'F' ───────────────────────────────────────────
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    if (grid[r, c] == 'F')
                    {
                        d.AddFlag(c * CellW, r * CellH, CellW, CellH);
                    }

            return d;
        }

        /// <summary>
        /// Первые 2 строки = Ceiling, последние 2 = Ground, остальное = Platform.
        /// Platform — твёрдая горизонтальная поверхность в середине уровня.
        /// </summary>
        private static TileType ClassifyRow(int row, int totalRows)
        {
            if (row <= 1) return TileType.Ceiling;
            if (row >= totalRows - 2) return TileType.Ground;
            return TileType.Platform;
        }
    }
}