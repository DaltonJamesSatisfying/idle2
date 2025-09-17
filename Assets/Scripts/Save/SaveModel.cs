using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IdleFramework.Save
{
    /// <summary>
    /// Serializable save data container.
    /// </summary>
    public sealed class SaveModel
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        [JsonPropertyName("lastSaveUtc")]
        public DateTimeOffset LastSaveUtc { get; set; }

        [JsonPropertyName("currencies")]
        public Dictionary<string, double> Currencies { get; set; } = new Dictionary<string, double>();

        [JsonPropertyName("generators")]
        public Dictionary<string, int> Generators { get; set; } = new Dictionary<string, int>();

        [JsonPropertyName("purchasedUpgrades")]
        public List<string> PurchasedUpgrades { get; set; } = new List<string>();

        [JsonPropertyName("lifetimePerGenerator")]
        public Dictionary<string, double> LifetimePerGenerator { get; set; } = new Dictionary<string, double>();

        [JsonPropertyName("totalLifetimeProduced")]
        public double TotalLifetimeProduced { get; set; }

        [JsonPropertyName("prestigeCurrency")]
        public double PrestigeCurrency { get; set; }

        [JsonPropertyName("lastPrestigeUtc")]
        public DateTimeOffset LastPrestigeUtc { get; set; }
    }
}
