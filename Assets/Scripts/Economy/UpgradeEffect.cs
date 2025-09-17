using System;
using IdleFramework.Data.Models;

namespace IdleFramework.Economy
{
    /// <summary>
    /// Applies upgrade bonuses to the aggregated economy state.
    /// </summary>
    public interface IUpgradeEffect
    {
        /// <summary>
        /// Applies the effect to the provided context.
        /// </summary>
        /// <param name="context">Mutable context that accumulates modifiers.</param>
        void Apply(UpgradeEffectContext context);
    }

    /// <summary>
    /// Aggregates upgrade modifiers.
    /// </summary>
    public sealed class UpgradeEffectContext
    {
        internal UpgradeEffectContext(EconomyService service)
        {
            Service = service;
        }

        internal EconomyService Service { get; }

        /// <summary>
        /// Adds a multiplicative bonus for the supplied target.
        /// </summary>
        public void AddMultiplier(string target, double percent)
        {
            Service.AddMultiplier(target, percent);
        }

        /// <summary>
        /// Adds an additive bonus for the supplied target.
        /// </summary>
        public void AddAdditive(string target, double amountPerSec)
        {
            Service.AddAdditive(target, amountPerSec);
        }

        /// <summary>
        /// Adds a cost reduction modifier for the supplied target.
        /// </summary>
        public void AddCostReduction(string target, double percent)
        {
            Service.AddCostReduction(target, percent);
        }
    }

    /// <summary>
    /// Factory producing upgrade effect instances from definitions.
    /// </summary>
    public static class UpgradeEffectFactory
    {
        /// <summary>
        /// Creates a runtime effect from the provided definition.
        /// </summary>
        public static IUpgradeEffect Create(UpgradeEffectDef def)
        {
            if (def == null)
            {
                throw new ArgumentNullException(nameof(def));
            }

            var type = def.Type?.ToLowerInvariant();
            return type switch
            {
                "multiplier" => new MultiplierUpgradeEffect(def.Target, def.Percent),
                "additive" => new AdditiveUpgradeEffect(def.Target, def.AmountPerSec != 0 ? def.AmountPerSec : def.Amount),
                "costreduction" => new CostReductionUpgradeEffect(def.Target, def.Percent),
                _ => throw new ArgumentOutOfRangeException(nameof(def), $"Unknown upgrade effect type '{def.Type}'.")
            };
        }
    }

    internal sealed class MultiplierUpgradeEffect : IUpgradeEffect
    {
        private readonly string _target;
        private readonly double _percent;

        public MultiplierUpgradeEffect(string target, double percent)
        {
            _target = string.IsNullOrEmpty(target) ? EconomyService.AllGeneratorsTarget : target;
            _percent = percent;
        }

        public void Apply(UpgradeEffectContext context)
        {
            context.AddMultiplier(_target, _percent);
        }
    }

    internal sealed class AdditiveUpgradeEffect : IUpgradeEffect
    {
        private readonly string _target;
        private readonly double _amountPerSec;

        public AdditiveUpgradeEffect(string target, double amountPerSec)
        {
            _target = string.IsNullOrEmpty(target) ? EconomyService.AllGeneratorsTarget : target;
            _amountPerSec = amountPerSec;
        }

        public void Apply(UpgradeEffectContext context)
        {
            context.AddAdditive(_target, _amountPerSec);
        }
    }

    internal sealed class CostReductionUpgradeEffect : IUpgradeEffect
    {
        private readonly string _target;
        private readonly double _percent;

        public CostReductionUpgradeEffect(string target, double percent)
        {
            _target = string.IsNullOrEmpty(target) ? EconomyService.AllGeneratorsTarget : target;
            _percent = percent;
        }

        public void Apply(UpgradeEffectContext context)
        {
            context.AddCostReduction(_target, _percent);
        }
    }
}
