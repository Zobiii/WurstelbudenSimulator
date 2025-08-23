using System;
using System.IO;
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

        public void Save(GameState state, string saveName)
        {
            if (string.IsNullOrWhiteSpace(saveName))
                throw new ArgumentException("Save name cannot be empty.", nameof(saveName));

            if (!Directory.Exists(SaveDir))
                Directory.CreateDirectory(SaveDir);

            var path = Path.Combine(SaveDir, $"{saveName}.json");
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
    }
}