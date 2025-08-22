using System;
using System.Data;
using Wurstelbuden.Logik.Models;

namespace Wurstelbuden.Logik.Services
{
    /// <summary>
    /// Handles money operations. Keeps arithmetic in one place for clarity.
    /// </summary>

    public sealed class BankService
    {
        public void Deposit(GameState state, decimal amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            state.Balance += amount;
        }

        public bool TryWithdraw(GameState state, decimal amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (state.Balance >= amount)
            {
                state.Balance -= amount;
                return true;
            }
            return false;
        }
    }
}