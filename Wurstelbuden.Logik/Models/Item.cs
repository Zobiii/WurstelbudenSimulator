using System;

namespace Wurstelbuden.Logik.Models
{
    /// <summary>
    /// Represents an item that can be bought and sold in the game.
    /// </summary>

    public sealed class Item
    {
        public string Name { get; init; }
        public decimal SellPrice { get; init; }
        public decimal BuyPrice { get; init; }
        public Item(string name, decimal sellPrice, decimal buyPrice)
        {
            Name = name;
            SellPrice = sellPrice;
            BuyPrice = buyPrice;
        }
    }
}