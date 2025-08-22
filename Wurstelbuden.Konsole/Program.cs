using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Wurstelbuden.Konsole.UI;
using Wurstelbuden.Logik.Models;
using Wurstelbuden.Logik.Services;

namespace Wurstelbuden.Konsole
{
    internal static class Program
    {
        private static readonly string SavePath = Path.Combine("saves", "spielstand.json");

        private static GameState _state = new();
        private static readonly BankService _bank = new();
        private static readonly InventoryService _inv = new();
        private static readonly WeatherService _weather = new();
        private static readonly SaveLoadService _persistence = new();
        private static readonly MarketService _market = new(_bank, _inv);

        private static void Main()
        {
            _inv.EnsureCatalogDefaults(_state);
            _weather.EnsureForecast(_state);

            var menu = new Menu("Würstelbuden-Simulator", new[]{
                "Bank",
                "Supermarkt",
                "Lager",
                "Wettervorhersage",
                "Tag beenden",
                "Spiel speichern",
                "Spiel laden",
                "Beenden"
            });

            bool running = true;
            while (running)
            {
                var choice = menu.Show(StatusText);
                Console.Clear();
                switch (choice)
                {
                    case 0: ShowBank(); break;
                    case 1: ShowSupermarkt(); break;
                    case 2: ShowLager(); break;
                    case 3: ShowWetter(); break;
                    case 4: EndDay(); break;
                    case 5: SaveGame(); break;
                    case 6: LoadGame(); break;
                    case 7: running = false; break;
                }
                if (running)
                {
                    Console.WriteLine("\nDrücke eine Taste, um zum Menü zurückzukehren…");
                    Console.ReadKey(true);
                }
            }
            Console.WriteLine("Bis bald!\n");
        }

        private static string StatusText()
            => $"Tag {_state.Day} | Kontostand: {_state.Balance:0.00} €";

        private static void ShowBank()
        {
            Console.WriteLine("BANK");
            Console.WriteLine("────");
            Console.WriteLine(StatusText());
            Console.WriteLine("\n(Info) Hier gibt es aktuell nur den Kontostand. Später werden Kredite ergänzt.");
        }

        private static void ShowLager()
        {
            Console.WriteLine("LAGER");
            Console.WriteLine("─────");
            foreach (var kvp in _state.Inventory.OrderBy(k => k.Key))
                Console.WriteLine($"{kvp.Key,-12} : {kvp.Value,4}");
        }

        private static void ShowWetter()
        {
            Console.WriteLine("WETTERVORHERSAGE (heute + 4 Tage)");
            Console.WriteLine("──────────────────────────────");
            for (int i = 0; i < _state.Forecast.Count; i++)
            {
                var lbl = i == 0 ? "Heute" : $"+{i} Tage";
                Console.WriteLine($"{lbl,-10} : {_state.Forecast[i]}");
            }
        }

        private static void ShowSupermarkt()
        {
            Console.WriteLine("SUPERMARKT – Preise (Einkauf)");
            Console.WriteLine("────────────────────────────");
            var priceList = _market.GetPriceList(_state)
            .OrderBy(k => k.Key)
            .ToList();
            foreach (var kvp in priceList)
            {
                Console.WriteLine($"{kvp.Key,-12} : {kvp.Value,5:0.00} €");
            }


            Console.WriteLine($"\n{StatusText()}");
            Console.WriteLine("Gib den Artikelnamen ein (oder leer für Abbruch):");
            Console.Write("> ");
            var name = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(name)) return;
            if (!priceList.Any(p => p.Key.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine("Unbekannter Artikel.");
                return;
            }
            var canonical = priceList.First(p => p.Key.Equals(name, StringComparison.OrdinalIgnoreCase)).Key;


            Console.WriteLine("Menge (ganze Zahl):");
            Console.Write("> ");
            if (!int.TryParse(Console.ReadLine(), out var qty) || qty <= 0)
            {
                Console.WriteLine("Ungültige Menge.");
                return;
            }


            var ok = _market.TryBuy(_state, canonical, qty);
            if (ok)
            {
                Console.WriteLine($"Gekauft: {qty} × {canonical}. Neuer Kontostand: {_state.Balance:0.00} €");
            }
            else
            {
                Console.WriteLine("Kauf fehlgeschlagen (zu wenig Geld?).");
            }
        }

        private static void EndDay()
        {
            Console.WriteLine("TAG BEENDEN – Verkauf wird simuliert…\n");
            var (sold, revenue, weather) = _weather.SimulateDay(_state);
            Console.WriteLine($"Wetter heute: {weather}");
            Console.WriteLine("Verkäufe:");
            foreach (var kvp in sold.OrderBy(k => k.Key))
            {
                Console.WriteLine($" {kvp.Key,-12} : {kvp.Value,4}");
            }
            Console.WriteLine($"\nEinnahmen: {revenue:0.00} €");
            Console.WriteLine($"Neuer Kontostand: {_state.Balance:0.00} €");
        }


        private static void SaveGame()
        {
            try
            {
                _persistence.Save(_state, SavePath);
                Console.WriteLine($"Spiel gespeichert unter '{SavePath}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Speichern: " + ex.Message);
            }
        }

        private static void LoadGame()
        {
            try
            {
                _state = _persistence.Load(SavePath);
                // Rewire services if necessary (state carries data only)
                Console.WriteLine($"Spielstand geladen aus '{SavePath}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Laden: " + ex.Message);
            }
        }
    }
}