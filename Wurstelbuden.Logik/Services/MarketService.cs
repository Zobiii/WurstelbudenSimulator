using System;
using System.Collections.Generic;
using Wurstelbuden.Logik.Models;

namespace Wurstelbuden.Logik.Services
{
    /// <summary>
    /// Represents a simple supermarket where the player can buy stock.
    /// </summary>

    public sealed class MarketService
    {
        private readonly BankService _bank;
        private readonly InventoryService _inv;

        public MarketService(BankService bank, InventoryService inv)
        {
            _bank = bank;
            _inv = inv;
        }

        public bool TryBuy(GameState state, string itemName, int quantity)
        {
            if (quantity <= 0) return false;
            if (!state.Catalog.TryGetValue(itemName, out var item)) return false;

            var cost = item.BuyPrice * quantity;
            if (!_bank.TryWithdraw(state, cost)) return false;

            _inv.Add(state, itemName, quantity);
            return true;
        }

        public IReadOnlyDictionary<string, decimal> GetPriceList(GameState state)
        {
            var dict = new Dictionary<string, decimal>();
            foreach (var kvp in state.Catalog)
                dict[kvp.Key] = kvp.Value.BuyPrice;
            return dict;
        }
    }
}