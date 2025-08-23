namespace Wurstelbuden.Logik.Models
{
    /// <summary>
    /// Represents a batch of items with quantity and expiration day.
    /// </summary>

    public sealed class InventoryBatch
    {
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int ExpiryDay { get; set; }

        public InventoryBatch() { }

        public InventoryBatch(string itemName, int quantity, int expiryDay)
        {
            ItemName = itemName;
            Quantity = quantity;
            ExpiryDay = expiryDay;
        }
    }
}