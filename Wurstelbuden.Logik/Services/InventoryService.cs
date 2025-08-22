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
            => state.Inventory.TryGetValue(itemName, out var q) ? q : 0;

        public void Add(GameState state, string itemName, int quantity)
        {
            if (!state.Inventory.ContainsKey(itemName))
                state.Inventory[itemName] = 0;
            state.Inventory[itemName] += quantity;
            if (state.Inventory[itemName] < 0)
                state.Inventory[itemName] = 0;
        }

        public void EnsureCatalogDefaults(GameState state)
        {
            void AddIfMissing(string name, decimal sell, decimal buy)
            {
                if (!state.Catalog.ContainsKey(name))
                    state.Catalog[name] = new Item(name, sell, buy);
                if (!state.Inventory.ContainsKey(name))
                    state.Inventory[name] = 0;
            }

            AddIfMissing("Würstel", 3.50m, 1.20m);
            AddIfMissing("Semmeln", 0.80m, 0.25m);
            AddIfMissing("Getränke", 2.80m, 0.90m);
            AddIfMissing("Ketchup", 0.00m, 0.05m);
            AddIfMissing("Senf", 0.00m, 0.05m);
        }
    }
}