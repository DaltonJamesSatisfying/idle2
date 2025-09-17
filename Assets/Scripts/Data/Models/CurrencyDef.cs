using System.Text.Json.Serialization;

namespace IdleFramework.Data.Models
{
    /// <summary>
    /// Currency definition loaded from JSON content.
    /// </summary>
    public sealed class CurrencyDef
    {
        /// <summary>
        /// Unique identifier of the currency.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Display name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Starting balance for new saves.
        /// </summary>
        [JsonPropertyName("start")]
        public double Start { get; set; }
    }
}
