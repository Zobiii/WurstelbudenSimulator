namespace Wurstelbuden.Konsole.UI
{
    /// <summary>
    /// Simple arrow-key driven vertical menu with status bar.
    /// Left/Right are accepted but currently have no special meaning and will beep.
    /// </summary>

    public sealed class Menu
    {
        private readonly string _title;
        private readonly List<string> _options;
        private int _index = 0;

        public Menu(string title, IEnumerable<string> options)
        {
            _title = title;
            _options = options.ToList();
        }

        public int Show(Func<string> statusProvider)
        {
            ConsoleKey key;
            Console.CursorVisible = false;
            do
            {
                Console.Clear();
                Console.WriteLine(_title);
                Console.WriteLine(new string('─', Math.Max(10, _title.Length)));
                for (int i = 0; i < _options.Count; i++)
                {
                    var prefix = (i == _index) ? "> " : "  ";
                    var line = (i == _index) ? Highlight(_options[i]) : _options[i];
                    Console.WriteLine(prefix + line);
                }

                Console.WriteLine();
                Console.WriteLine("Status: " + statusProvider());
                Console.WriteLine("\nVerwende ↑/↓ um zu navigieren, Enter für Bestätigen. (←/→ nicht in Verwendung)");

                var info = Console.ReadKey(true);
                key = info.Key;
                switch (key)
                {
                    case ConsoleKey.UpArrow:
                        _index = (_index - 1 + _options.Count) % _options.Count;
                        break;
                    case ConsoleKey.DownArrow:
                        _index = (_index + 1) % _options.Count;
                        break;
                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.RightArrow:
                        Console.Beep();
                        break;
                }
            } while (key != ConsoleKey.Enter);

            Console.CursorVisible = true;
            return _index;
        }

        private static string Highlight(string text)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("\r\x1b[2K");
            Console.ForegroundColor = prev;
            return text;
        }
    }
}