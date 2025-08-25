using System.Collections.Specialized;
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
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            state.Balance += decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        }

        public bool TryWithdraw(GameState state, decimal amount)
        {
            if (amount <= 0) return false;
            amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
            if (state.Balance >= amount)
            {
                state.Balance -= amount;
                return true;
            }
            return false;
        }

        public bool TransferToSavings(GameState state, decimal amount)
        {
            if (!TryWithdraw(state, amount)) return false;

            state.SavingsBalance += amount;
            state.SavingsBalance = decimal.Round(state.SavingsBalance, 2, MidpointRounding.AwayFromZero);
            return true;
        }

        public bool TransferFromSavings(GameState state, decimal amount)
        {
            if (amount <= 0) return false;

            amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

            if (state.SavingsBalance >= amount)
            {
                state.SavingsBalance -= amount;
                state.SavingsBalance = decimal.Round(state.SavingsBalance, 2, MidpointRounding.AwayFromZero);
                state.Balance += amount;
                state.Balance = decimal.Round(state.Balance, 2, MidpointRounding.AwayFromZero);
                return true;
            }
            return false;
        }

        public void TakeLoan(GameState state, decimal amount)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));

            amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
            state.LoanBalance += amount;
            state.Balance += amount;
            state.LoanBalance = decimal.Round(state.LoanBalance, 2, MidpointRounding.AwayFromZero);
            state.Balance = decimal.Round(state.Balance, 2, MidpointRounding.AwayFromZero);
        }

        public bool RepayLoan(GameState state, decimal amount)
        {
            if (amount <= 0) return false;
            amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

            var pay = Math.Min(amount, state.LoanBalance);
            if (!TryWithdraw(state, pay)) return false;

            state.LoanBalance -= pay;
            state.LoanBalance = decimal.Round(state.LoanBalance, 2, MidpointRounding.AwayFromZero);
            return true;
        }

        public void ApplyDailyInterest(GameState state)
        {
            const decimal daysPerYear = 365m;

            if (state.SavingsBalance > 0 && state.SavingsRateAnnual > 0)
            {
                var dailyRate = state.SavingsRateAnnual / daysPerYear;
                var interest = state.SavingsBalance * dailyRate;
                state.SavingsBalance += interest;
                state.SavingsBalance = decimal.Round(state.SavingsBalance, 2, MidpointRounding.AwayFromZero);
            }

            if (state.LoanBalance > 0 && state.LoanRateAnnual > 0)
            {
                var dailyRate = state.LoanRateAnnual / daysPerYear;
                var interest = state.LoanBalance * dailyRate;
                state.LoanBalance += interest;
                state.LoanBalance = decimal.Round(state.LoanBalance, 2, MidpointRounding.AwayFromZero);
            }
        }
    }
}