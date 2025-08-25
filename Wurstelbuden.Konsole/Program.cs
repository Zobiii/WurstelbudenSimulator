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
            Console.Title = "Würstelbuden-Simulator";

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
            => $"Tag {_state.Day} | Kontostand: {_state.Balance:0.00} € | Sparbuch: {_state.SavingsBalance} € | Kredit: {_state.LoanBalance} €";

        private static void ShowBank()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("BANK");
                Console.WriteLine("────");
                Console.WriteLine(StatusText());
                Console.WriteLine($"Sparkonto: {_state.SavingsBalance:0.00} €");
                Console.WriteLine($"Kredit    : {_state.LoanBalance:0.00} €");
                Console.WriteLine($"Zinsen p.a.: Sparen {_state.SavingsRateAnnual:P2} | Kredit {_state.LoanRateAnnual:P2}\n");

                var bankMenu = new Menu("Bank – Aktion wählen", new[]
                {
            "Auf Sparkonto einzahlen",
            "Vom Sparkonto abheben",
            "Kredit aufnehmen",
            "Kredit zurückzahlen",
            "Zurück"
        });

                var choice = bankMenu.Show(StatusText);
                Console.Clear();

                switch (choice)
                {
                    case 0:
                        {
                            var amount = ChooseAmount("Einzahlen aufs Sparkonto", maxSuggestion: _state.Balance);
                            if (amount < 0) break;
                            if (_bank.TransferToSavings(_state, amount))
                                Console.WriteLine($"Eingezahlt: {amount:0.00} € → Sparkonto: {_state.SavingsBalance:0.00} €");
                            else
                                Console.WriteLine("Nicht genug Guthaben auf dem Hauptkonto.");
                            Pause();
                            break;
                        }
                    case 1:
                        {
                            var amount = ChooseAmount("Abheben vom Sparkonto", maxSuggestion: _state.SavingsBalance);
                            if (amount < 0) break;
                            if (_bank.TransferFromSavings(_state, amount))
                                Console.WriteLine($"Abgehoben: {amount:0.00} € → Sparkonto: {_state.SavingsBalance:0.00} €");
                            else
                                Console.WriteLine("Nicht genug Guthaben auf dem Sparkonto.");
                            Pause();
                            break;
                        }
                    case 2:
                        {
                            var amount = ChooseAmount("Kredit aufnehmen", maxSuggestion: 1000m);
                            if (amount < 0) break;
                            _bank.TakeLoan(_state, amount);
                            Console.WriteLine($"Kredit aufgenommen: {amount:0.00} € → Kreditstand: {_state.LoanBalance:0.00} €");
                            Pause();
                            break;
                        }
                    case 3:
                        {
                            var maxPay = Math.Min(_state.Balance, _state.LoanBalance);
                            var amount = ChooseAmount("Kredit zurückzahlen", maxSuggestion: maxPay);
                            if (amount < 0) break;
                            if (_bank.RepayLoan(_state, amount))
                                Console.WriteLine($"Zurückgezahlt: {amount:0.00} € → Kreditstand: {_state.LoanBalance:0.00} €");
                            else
                                Console.WriteLine("Zahlung nicht möglich (zu wenig Guthaben?).");
                            Pause();
                            break;
                        }
                    case 4:
                        return;
                }
            }

            static void Pause()
            {
                Console.WriteLine("\nTaste drücken…");
                Console.ReadKey(true);
            }
        }

        private static void ShowLager()
        {
            Console.WriteLine("LAGER (mit Haltbarkeit)");
            Console.WriteLine("────────────────────────");

            var groups = _state.InventoryBatches.GroupBy(b => b.ItemName).OrderBy(g => g.Key);

            foreach (var g in groups)
            {
                var total = g.Sum(b => b.Quantity);
                var batches = string.Join(", ", g.OrderBy(b => b.ExpiryDay).Select(b => $"{b.Quantity} (bis Tag {b.ExpiryDay})"));
                Console.WriteLine($"{g.Key,-12} : {total,4} | {batches}");
            }

            if (!_state.InventoryBatches.Any())
                Console.WriteLine("(leer)");
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
            var priceList = _market.GetPriceList(_state)
                                    .OrderBy(k => k.Key)
                                    .ToList();

            // Artikel-Auswahl
            var menu = new Menu("Supermarkt – Artikel wählen",
                                priceList.Select(kvp => $"{kvp.Key} – {kvp.Value:0.00} €"));
            var selectedIndex = menu.Show(StatusText);
            var selectedItem = priceList[selectedIndex];
            var canonical = selectedItem.Key;
            var unitPrice = selectedItem.Value;

            // Mengen-Auswahl mit Pfeiltasten
            int qty = 1;
            ConsoleKey key;
            do
            {
                Console.Clear();
                Console.WriteLine("SUPERMARKT – Kauf bestätigen");
                Console.WriteLine("───────────────────────────");
                Console.WriteLine($"Artikel   : {canonical}");
                Console.WriteLine($"Preis     : {unitPrice:0.00} € pro Stück");
                Console.WriteLine($"Menge     : {qty}");
                Console.WriteLine($"Kosten    : {qty * unitPrice:0.00} €");
                Console.WriteLine($"Nach Kauf : {_state.Balance - qty * unitPrice:0.00} €");
                Console.WriteLine($"\n{StatusText()}");

                Console.WriteLine("\nLinks/Rechts = Menge ändern | Enter = Kaufen | Esc = Abbrechen");

                key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.LeftArrow:
                        if (qty > 1) qty--;
                        break;
                    case ConsoleKey.RightArrow:
                        qty++;
                        break;
                }
            } while (key != ConsoleKey.Enter && key != ConsoleKey.Escape);

            if (key == ConsoleKey.Enter)
            {
                var ok = _market.TryBuy(_state, canonical, qty);
                Console.Clear();
                if (ok)
                {
                    Console.WriteLine($"Gekauft: {qty} × {canonical}");
                    Console.WriteLine($"Neuer Kontostand: {_state.Balance:0.00} €");
                }
                else
                {
                    Console.WriteLine("Kauf fehlgeschlagen (zu wenig Geld?).");
                }
            }
            else
            {
                Console.Clear();
                Console.WriteLine("Kauf abgebrochen.");
            }
        }

        private static void EndDay()
        {
            try
            {
                _persistence.AutoSave(_state);
                Console.WriteLine($"\nAutosave gespeichert: autosave_day_{_state.Day}.json (Vortag gelöscht)");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Autosave fehlgeschlagen: " + ex.Message);
            }

            Console.WriteLine("TAG BEENDEN – Verkauf wird simuliert…\n");
            var (sold, revenue, weather) = _weather.SimulateDay(_state);

            _bank.ApplyDailyInterest(_state);
            Console.WriteLine($"\nZinsen verbucht: Sparen p.a. {_state.SavingsRateAnnual:P2} | Kredit p.a. {_state.LoanRateAnnual:P2}\n");


            Console.WriteLine($"Wetter heute: {weather}");
            Console.WriteLine("Verkäufe:");
            foreach (var kvp in sold.OrderBy(k => k.Key))
            {
                Console.WriteLine($" {kvp.Key,-12} : {kvp.Value,4}");
            }
            Console.WriteLine($"\nEinnahmen: {revenue:0.00} €");
            Console.WriteLine($"Neuer Kontostand: {_state.Balance:0.00} €");

            var expired = _inv.RemoveExpired(_state);
            if (expired.Count > 0)
            {
                Console.WriteLine("\nFolgende Waren sind verdorben und wurden entsorgt:");
                foreach (var kvp in expired)
                {
                    Console.WriteLine($"  {kvp.Key,-12} : {kvp.Value,4}");
                }
            }
        }

        private static void SaveGame()
        {
            Console.WriteLine("SPIEL SPEICHERN");
            Console.WriteLine("────────────────");
            Console.Write("Name für den Spielstand eingeben: ");

            var name = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                Console.WriteLine("Abgebrochen - kein Name eingegeben.");
                return;
            }

            try
            {
                var path = Path.Combine("saves", $"{name}.json");

                if (File.Exists(path))
                {
                    Console.WriteLine($"\n'{name}' existiert bereits. Überschreiben? (y/n) ");
                    var k = Console.ReadKey(true).Key;
                    if (k != ConsoleKey.Y)
                    {
                        Console.WriteLine("\nAbgebrochen.");
                        return;
                    }
                }

                _persistence.Save(_state, name);
                Console.WriteLine($"Spiel gespeichert als '{name}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Speichern: " + ex.Message);
            }
        }

        private static void LoadGame()
        {
            ConsoleKey key;
            while (true)
            {
                var saves = _persistence.GetAllSaveNames();
                if (saves.Count == 0)
                {
                    Console.Clear();
                    Console.WriteLine("SPIEL LADEN");
                    Console.WriteLine("────────────");
                    Console.WriteLine("Keine Spielstände gefunden.");
                    return;
                }

                int index = 0;
                do
                {
                    Console.Clear();
                    Console.WriteLine("SPIEL LADEN – Wähle einen Spielstand");
                    Console.WriteLine("────────────────────────────────────");
                    for (int i = 0; i < saves.Count; i++)
                    {
                        var prefix = (i == index) ? "> " : "  ";
                        var name = saves[i];
                        if (name.StartsWith("autosave_", StringComparison.OrdinalIgnoreCase))
                            name += "  (Auto)";
                        Console.WriteLine(prefix + name);
                    }

                    Console.WriteLine("\n↑/↓: Auswahl  |  Enter: Laden  |  Entf: Löschen  |  Esc: Abbrechen");
                    Console.WriteLine($"\n{StatusText()}");

                    key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.UpArrow:
                            index = (index - 1 + saves.Count) % saves.Count;
                            break;
                        case ConsoleKey.DownArrow:
                            index = (index + 1) % saves.Count;
                            break;

                        case ConsoleKey.Delete:
                            {
                                var toDelete = saves[index];
                                Console.WriteLine($"\nSpielstand '{toDelete}' wirklich löschen? (y/n)");
                                var confirm = Console.ReadKey(true).Key;
                                if (confirm == ConsoleKey.Y)
                                {
                                    var ok = _persistence.DeleteSaveByName(toDelete);
                                    if (!ok)
                                    {
                                        Console.WriteLine("Löschen fehlgeschlagen (Datei nicht gefunden?).");
                                        Console.ReadKey(true);
                                    }
                                    else
                                    {
                                        saves = _persistence.GetAllSaveNames();
                                        if (saves.Count == 0)
                                        {
                                            Console.WriteLine("\nAlle Spielstände gelöscht.");
                                            Console.ReadKey(true);
                                            return;
                                        }
                                        index = Math.Min(index, saves.Count - 1);
                                    }
                                }
                            }
                            break;

                        case ConsoleKey.Enter:
                            {
                                var chosenName = saves[index];
                                var path = Path.Combine("saves", $"{chosenName}.json");
                                try
                                {
                                    _state = _persistence.Load(path);
                                    _inv.EnsureCatalogDefaults(_state);
                                    _weather.EnsureForecast(_state);
                                    Console.WriteLine($"\nSpielstand '{chosenName}' geladen.");
                                    Console.WriteLine(StatusText());
                                    return;
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("Fehler beim Laden: " + ex.Message);
                                    Console.ReadKey(true);
                                }
                            }
                            break;

                        case ConsoleKey.Escape:
                            return;
                    }

                } while (true);
            }
        }

        private static decimal ChooseAmount(string title, decimal maxSuggestion)
        {
            ConsoleKey key;
            decimal stepSmall = 1.00m;
            decimal stepBig = 10.00m;
            decimal amount = stepSmall;

            if (maxSuggestion <= 0) maxSuggestion = stepSmall * 10;

            do
            {
                Console.Clear();
                Console.WriteLine(title);
                Console.WriteLine(new string('─', Math.Max(10, title.Length)));
                Console.WriteLine($"Betrag : {amount:0.00} €");
                Console.WriteLine($"Hinweis: Vorschlag max. ≈ {maxSuggestion:0.00} €");
                Console.WriteLine($"\n{StatusText()}");
                Console.WriteLine("\n←/→ = ±1€ | PgUp/PgDn = ±10€ | Home = 0€ | End = max | Enter = OK | Esc = Abbrechen");

                key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.LeftArrow:
                        amount = Math.Max(0m, amount - stepSmall);
                        break;
                    case ConsoleKey.RightArrow:
                        amount += stepSmall;
                        break;
                    case ConsoleKey.PageUp:
                        amount += stepBig;
                        break;
                    case ConsoleKey.PageDown:
                        amount = Math.Max(0m, amount - stepBig);
                        break;
                    case ConsoleKey.Home:
                        amount = 0m;
                        break;
                    case ConsoleKey.End:
                        amount = maxSuggestion > 0 ? maxSuggestion : amount;
                        break;
                }

                // Clamp and round
                if (amount > 999999m) amount = 999999m;
                amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

            } while (key != ConsoleKey.Enter && key != ConsoleKey.Escape);

            return key == ConsoleKey.Enter ? amount : -1m;
        }

    }
}