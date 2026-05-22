// models/Enums.cs — single source of truth for all game enums
// Place in the models/ folder. Keep Condition.cs and Cannon.cs blank.
namespace Plunger.Models
{
    public enum Condition { Run, Attached, Fall }
    public enum Cannon    { Plunger }
    public enum TileType  { Ground, Ceiling, Wall, Platform, Floor }
}
