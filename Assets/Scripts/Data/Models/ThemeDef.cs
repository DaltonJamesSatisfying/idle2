using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IdleFramework.Data.Models
{
    /// <summary>
    /// Theme configuration describing presentation and meta tuning.
    /// </summary>
    public sealed class ThemeDef
    {
        [JsonPropertyName("primaryColor")]
        public string PrimaryColor { get; set; } = "#FFFFFF";

        [JsonPropertyName("fontId")]
        public string FontId { get; set; } = "default";

        [JsonPropertyName("sfxPackId")]
        public string SfxPackId { get; set; } = string.Empty;

        [JsonPropertyName("artAtlasId")]
        public string ArtAtlasId { get; set; } = string.Empty;

        [JsonPropertyName("prestigeFormula")]
        public PrestigeFormulaDef PrestigeFormula { get; set; } = new PrestigeFormulaDef();

        [JsonPropertyName("offlineCapHours")]
        public double OfflineCapHours { get; set; } = 12d;

        [JsonPropertyName("startingBalances")]
        public Dictionary<string, double> StartingBalances { get; set; } = new Dictionary<string, double>();
    }

    /// <summary>
    /// Parameters for the prestige conversion formula.
    /// </summary>
    public sealed class PrestigeFormulaDef
    {
        [JsonPropertyName("A")]
        public double A { get; set; } = 1d;

        [JsonPropertyName("B")]
        public double B { get; set; } = 1d;
    }
}
