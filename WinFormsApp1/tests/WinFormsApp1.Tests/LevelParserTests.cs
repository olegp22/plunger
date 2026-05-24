using System;
using System.IO;
using Plunger.Models;
using Xunit;

namespace WinFormsApp1.Tests
{
    public class LevelParserTests
    {
        [Fact]
        public void Parse_ValidFile_ReturnsLevelData()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "levels", "level1.txt");
            Assert.True(File.Exists(path), $"level file not found: {path}");
            var data = LevelParser.Parse(path);
            Assert.NotNull(data);
            Assert.True(data.WorldWidth > 0 && data.WorldHeight > 0);
            Assert.NotNull(data.Tiles);
        }
    }
}
