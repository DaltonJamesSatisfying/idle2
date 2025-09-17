using System;
using System.IO;
using System.Text.Json;

namespace IdleFramework.Utils
{
    /// <summary>
    /// Helper methods for JSON serialization.
    /// </summary>
    public static class JsonUtils
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = true
        };

        /// <summary>
        /// Serializes a value to JSON using shared options.
        /// </summary>
        public static string Serialize<T>(T value)
        {
            return JsonSerializer.Serialize(value, Options);
        }

        /// <summary>
        /// Deserializes JSON text to the specified type.
        /// </summary>
        public static T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, Options) ?? throw new InvalidDataException("Unable to deserialize JSON content.");
        }

        /// <summary>
        /// Reads JSON content from disk and deserializes to the specified type.
        /// </summary>
        public static T DeserializeFromFile<T>(string path)
        {
            var text = File.ReadAllText(path);
            return Deserialize<T>(text);
        }
    }
}
