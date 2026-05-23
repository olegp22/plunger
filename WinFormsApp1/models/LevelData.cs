// models/LevelData.cs
// All world geometry + coins for one level.
using System.Collections.Generic;
using Plunger.Models.Common;

namespace Plunger.Models
{
    public class LevelData
    {
        public int WorldWidth  { get; }
        public int WorldHeight { get; }
        public Plunger.Point PlayerStart { get; }

        public List<Tile> Tiles { get; } = new List<Tile>();
        public List<Coin> Coins { get; } = new List<Coin>();
        public List<Plunger.Models.Common.Rectangle> Flags { get; } = new List<Plunger.Models.Common.Rectangle>();

        public LevelData(int worldWidth, int worldHeight, Plunger.Point playerStart)
        {
            WorldWidth   = worldWidth;
            WorldHeight  = worldHeight;
            PlayerStart  = playerStart;
        }

        public void AddTile(int x, int y, int w, int h, TileType t = TileType.Platform)
            => Tiles.Add(new Tile(x, y, w, h, t));

        public void AddCoin(int x, int y)
            => Coins.Add(new Coin(new Plunger.Point(x, y)));

        public void AddFlag(int x, int y, int w, int h)
            => Flags.Add(new Plunger.Models.Common.Rectangle(x, y, w, h));
    }
}
