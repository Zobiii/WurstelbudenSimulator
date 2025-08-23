using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wurstelbuden.Logik.Models;

namespace Wurstelbuden.Logik.Services
{
    /// <summary>
    /// Handles serialization and persistence of the GameState.
    /// </summary>

    public sealed class SaveLoadService
    {
        private readonly JsonSerializerOptions _opts = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        private const string SaveDir = "saves";

        private static string Sanitize(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            foreach (var ch in invalid)
                name = name.Replace(ch, '_');
            return name.Trim();
        }

        public void Save(GameState state, string saveName)
        {
            if (string.IsNullOrWhiteSpace(saveName))
                throw new ArgumentException("Save name cannot be empty.", nameof(saveName));

            if (!Directory.Exists(SaveDir))
                Directory.CreateDirectory(SaveDir);

            var clean = Sanitize(saveName);
            var path = Path.Combine(SaveDir, $"{clean}.json");
            var json = JsonSerializer.Serialize(state, _opts);
            File.WriteAllText(path, json);
        }

        public GameState Load(string path)
        {
            var json = File.ReadAllText(path);
            var state = JsonSerializer.Deserialize<GameState>(json, _opts)
                        ?? throw new InvalidOperationException("Failed to deserialize game state.");
            return state;
        }

        public List<string> GetAllSaveFiles()
        {
            if (!Directory.Exists(SaveDir))
                return new List<string>();

            return Directory.GetFiles(SaveDir, "*.json").OrderBy(f => f).ToList();
        }

        public List<string> GetAllSaveNames()
            => GetAllSaveFiles().Select(f => Path.GetFileNameWithoutExtension(f)).ToList();

        public bool DeleteSaveByName(string saveName)
        {
            var clean = Sanitize(saveName);
            var path = Path.Combine(SaveDir, $"{clean}.json");
            if (File.Exists(path))
            {
                File.Delete(path);
                return true;
            }
            return false;
        }

        private static string AutoSaveNameForDay(int day) => $"autosave_day_{day}";
        private static string AutoSavePathForDay(int day) => Path.Combine(SaveDir, AutoSaveNameForDay(day) + ".json");

        public void AutoSave(GameState state)
        {
            if (!Directory.Exists(SaveDir))
                Directory.CreateDirectory(SaveDir);

            if (state.Day > 1)
            {
                var prevPath = AutoSavePathForDay(state.Day - 3);
                if (File.Exists(prevPath))
                    File.Delete(prevPath);
            }

            var currentPath = AutoSavePathForDay(state.Day);
            var json = JsonSerializer.Serialize(state, _opts);
            File.WriteAllText(currentPath, json);
        }
    }
}