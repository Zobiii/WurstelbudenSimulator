using System;
using System.Collections.Generic;
using Wurstelbuden.Logik.Models;

namespace Wurstelbuden.Logik.Services
{
    /// <summary>
    /// Generates and interprets weather forecasts influencing demand.
    /// </summary>

    public sealed class WeatherService
    {
        private readonly Random _rng;

        public WeatherService(int? seed = null)
        {
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public void EnsureForecast(GameState state)
        {
            if (state.Forecast.Count == 0)
                state.Forecast.AddRange(GenerateForecast(5));
        }

        public IEnumerable<WeatherType> GenerateForecast(int days)
        {
            for (int i = 0; i < days; i++)
            {
                var roll = _rng.NextDouble();
                yield return roll switch
                {
                    < 0.35 => WeatherType.Sunny,
                    < 0.60 => WeatherType.Cloudy,
                    < 0.80 => WeatherType.Rainy,
                    < 0.92 => WeatherType.Cold,
                    _ => WeatherType.Stormy
                };
            }
        }

        public Dictionary<string, double> GetDemandFactors(WeatherType w)
        {
            return w switch
            {
                WeatherType.Sunny => new() { ["Würstel"] = 1.1, ["Semmeln"] = 1.1, ["Getränke"] = 1.6 },
                WeatherType.Cloudy => new() { ["Würstel"] = 1.0, ["Semmeln"] = 1.0, ["Getränke"] = 1.0 },
                WeatherType.Rainy => new() { ["Würstel"] = 0.8, ["Semmeln"] = 0.8, ["Getränke"] = 0.6 },
                WeatherType.Cold => new() { ["Würstel"] = 1.2, ["Semmeln"] = 1.1, ["Getränke"] = 0.5 },
                WeatherType.Stormy => new() { ["Würstel"] = 0.4, ["Semmeln"] = 0.4, ["Getränke"] = 0.3 },
                _ => new()
            };
        }

        public (Dictionary<string, int> sold, decimal revenue, WeatherType weather) SimulateDay(GameState state)
        {
            EnsureForecast(state);
            var todayWeather = state.Forecast[0];

            var demand = GetDemandFactors(todayWeather);
            var sold = new Dictionary<string, int>();
            decimal revenue = 0;

            foreach (var kvp in state.Catalog)
            {
                var name = kvp.Key;
                var item = kvp.Value;
                if (item.SellPrice <= 0) continue;

                var baseWant = 8;
                var factor = demand.TryGetValue(name, out var f) ? f : 1.0;
                var want = (int)Math.Round(baseWant * factor + _rng.Next(0, 6));
                var available = state.Inventory.TryGetValue(name, out var q) ? q : 0;
                var canSell = Math.Min(want, available);

                if (canSell > 0)
                {
                    state.Inventory[name] = available - canSell;
                    sold[name] = canSell;
                    revenue += canSell * item.SellPrice;
                }
                else
                {
                    sold[name] = 0;
                }
            }

            state.Balance += revenue;

            state.Forecast.RemoveAt(0);
            state.Forecast.AddRange(GenerateForecast(1));
            state.Day += 1;

            return (sold, revenue, todayWeather);
        }
    }
}