using System.Text.Json.Serialization;

namespace IdleFramework.Data.Models
{
    /// <summary>
    /// Achievement metadata for skins.
    /// </summary>
    public sealed class AchievementDef
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("desc")]
        public string Description { get; set; } = string.Empty;
    }
}
