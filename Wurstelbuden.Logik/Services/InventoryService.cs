using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using Wurstelbuden.Logik.Models;

namespace Wurstelbuden.Logik.Services
{
    /// <summary>
    /// Manages warehouse stock (add, remove, read quantities)
    /// </summary>

    public sealed class InventoryService
    {
        public int GetQty(GameState state, string itemName)
        {
            return state.InventoryBatches
                        .Where(b => b.ItemName == itemName)
                        .Sum(b => b.Quantity);
        }


        public void Add(GameState state, string itemName, int quantity)
        {
            if (!state.Catalog.TryGetValue(itemName, out var item)) return;

            int expiryDay = item.ShelfLifeDays > 0 ? state.Day + item.ShelfLifeDays : int.MaxValue;
            state.InventoryBatches.Add(new InventoryBatch(itemName, quantity, expiryDay));
        }

        public int Consume(GameState state, string itemName, int quantity)
        {
            int remaining = quantity;
            foreach (var batch in state.InventoryBatches.Where(b => b.ItemName == itemName).OrderBy(b => b.ExpiryDay))
            {
                if (remaining <= 0) break;
                int take = Math.Min(batch.Quantity, remaining);
                batch.Quantity -= take;
                remaining -= take;
            }
            state.InventoryBatches.RemoveAll(b => b.Quantity <= 0);
            return quantity - remaining;
        }

        public void EnsureCatalogDefaults(GameState state)
        {
            void AddIfMissing(string name, decimal sell, decimal buy, int shelfLifeDays)
            {
                if (!state.Catalog.ContainsKey(name))
                    state.Catalog[name] = new Item(name, sell, buy, shelfLifeDays);

            }

            AddIfMissing("Würstel", 3.50m, 1.20m, 3);
            AddIfMissing("Semmeln", 0.80m, 0.25m, 2);
            AddIfMissing("Getränke", 2.80m, 0.90m, 10);
            AddIfMissing("Ketchup", 0.00m, 0.05m, 30);
            AddIfMissing("Senf", 0.00m, 0.05m, 30);
        }

        public Dictionary<string, int> RemoveExpired(GameState state)
        {
            var expired = state.InventoryBatches
                                .Where(b => b.ExpiryDay <= state.Day)
                                .GroupBy(b => b.ItemName)
                                .ToDictionary(g => g.Key, g => g.Sum(b => b.Quantity));

            state.InventoryBatches.RemoveAll(b => b.ExpiryDay <= state.Day);
            return expired;
        }
    }
}