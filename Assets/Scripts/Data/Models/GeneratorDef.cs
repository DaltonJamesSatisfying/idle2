using System.Text.Json.Serialization;

namespace IdleFramework.Data.Models
{
    /// <summary>
    /// Describes a generator in the idle economy.
    /// </summary>
    public sealed class GeneratorDef
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("iconId")]
        public string IconId { get; set; } = string.Empty;

        [JsonPropertyName("currencyId")]
        public string CurrencyId { get; set; } = string.Empty;

        [JsonPropertyName("baseCost")]
        public double BaseCost { get; set; }

        [JsonPropertyName("costCurve")]
        public CostCurveDef CostCurve { get; set; } = new CostCurveDef();

        [JsonPropertyName("baseRatePerSec")]
        public double BaseRatePerSec { get; set; }

        [JsonPropertyName("unlockReq")]
        public UnlockRequirementDef UnlockRequirement { get; set; } = new UnlockRequirementDef();

        [JsonPropertyName("maxLevel")]
        public int? MaxLevel { get; set; }
    }

    /// <summary>
    /// Configuration for cost scaling.
    /// </summary>
    public sealed class CostCurveDef
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "linear";

        [JsonPropertyName("step")]
        public double Step { get; set; } = 1.0;

        [JsonPropertyName("growth")]
        public double Growth { get; set; } = 1.07;

        [JsonPropertyName("a")]
        public double A { get; set; }

        [JsonPropertyName("b")]
        public double B { get; set; }
    }

    /// <summary>
    /// Unlock conditions for a generator.
    /// </summary>
    public sealed class UnlockRequirementDef
    {
        [JsonPropertyName("currencyId")]
        public string? CurrencyId { get; set; }

        [JsonPropertyName("amount")]
        public double Amount { get; set; }

        [JsonPropertyName("generatorId")]
        public string? GeneratorId { get; set; }

        [JsonPropertyName("minLevel")]
        public int MinLevel { get; set; }
    }
}
