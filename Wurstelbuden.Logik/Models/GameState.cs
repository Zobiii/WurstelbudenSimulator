namespace Wurstelbuden.Logik.Models
{
    /// <summary>
    /// Serializable snapshot of the entire game state.
    /// </summary>

    public sealed class GameState
    {
        public int Day { get; set; } = 1;
        public decimal Balance { get; set; } = 100m;

        public List<WeatherType> Forecast { get; set; } = new();
        public Dictionary<string, Item> Catalog { get; set; } = new();
        public List<InventoryBatch> InventoryBatches { get; set; } = new();
        public int SchemaVersion { get; set; } = 1;
    }
}