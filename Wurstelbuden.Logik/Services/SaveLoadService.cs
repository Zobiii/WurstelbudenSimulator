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
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public void Save(GameState state, string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
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
    }
}