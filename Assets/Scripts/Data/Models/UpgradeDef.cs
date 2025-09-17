using System.Text.Json.Serialization;

namespace IdleFramework.Data.Models
{
    /// <summary>
    /// Describes an upgrade purchasable by the player.
    /// </summary>
    public sealed class UpgradeDef
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("desc")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public double Price { get; set; }

        [JsonPropertyName("iconId")]
        public string IconId { get; set; } = string.Empty;

        [JsonPropertyName("conditions")]
        public UpgradeConditionDef Conditions { get; set; } = new UpgradeConditionDef();

        [JsonPropertyName("effect")]
        public UpgradeEffectDef Effect { get; set; } = new UpgradeEffectDef();
    }

    /// <summary>
    /// Conditions for unlocking an upgrade.
    /// </summary>
    public sealed class UpgradeConditionDef
    {
        [JsonPropertyName("generatorId")]
        public string? GeneratorId { get; set; }

        [JsonPropertyName("minLevel")]
        public int MinLevel { get; set; }

        [JsonPropertyName("currencyId")]
        public string? CurrencyId { get; set; }

        [JsonPropertyName("minTotal")]
        public double MinTotal { get; set; }
    }

    /// <summary>
    /// Raw definition of an upgrade effect.
    /// </summary>
    public sealed class UpgradeEffectDef
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("target")]
        public string Target { get; set; } = "all";

        [JsonPropertyName("percent")]
        public double Percent { get; set; }

        [JsonPropertyName("amountPerSec")]
        public double AmountPerSec { get; set; }

        [JsonPropertyName("amount")]
        public double Amount { get; set; }
    }
}
