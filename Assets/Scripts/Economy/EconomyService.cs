using System;
using System.Collections.Generic;
using IdleFramework.Data;
using IdleFramework.Data.Models;
using IdleFramework.Save;

namespace IdleFramework.Economy
{
    /// <summary>
    /// Central service managing generator production, upgrades, and balances.
    /// </summary>
    public sealed class EconomyService
    {
        internal const string AllGeneratorsTarget = "all";

        private readonly ContentDB _content;
        private readonly Dictionary<string, GeneratorState> _generatorStates = new Dictionary<string, GeneratorState>();
        private readonly Dictionary<string, double> _balances = new Dictionary<string, double>();
        private readonly HashSet<string> _purchasedUpgrades = new HashSet<string>();
        private readonly Dictionary<string, double> _multipliers = new Dictionary<string, double>();
        private readonly Dictionary<string, double> _additives = new Dictionary<string, double>();
        private readonly Dictionary<string, double> _costReductions = new Dictionary<string, double>();
        private double _externalMultiplier = 1d;

        /// <summary>
        /// Occurs when any economy value changes.
        /// </summary>
        public event Action? OnStateChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="EconomyService"/> class.
        /// </summary>
        /// <param name="content">Content database.</param>
        public EconomyService(ContentDB content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            foreach (var generator in content.Generators.Values)
            {
                _generatorStates[generator.Id] = new GeneratorState(generator);
            }

            foreach (var currency in content.Currencies.Values)
            {
                _balances[currency.Id] = content.Theme.StartingBalances.TryGetValue(currency.Id, out var start) ? start : currency.Start;
            }
        }

        /// <summary>
        /// Gets the total lifetime production across all generators.
        /// </summary>
        public double TotalLifetimeProduced { get; private set; }

        /// <summary>
        /// Gets current currency balances.
        /// </summary>
        public IReadOnlyDictionary<string, double> Balances => _balances;

        /// <summary>
        /// Gets generator runtime states.
        /// </summary>
        public IReadOnlyDictionary<string, GeneratorState> Generators => _generatorStates;

        /// <summary>
        /// Gets purchased upgrade identifiers.
        /// </summary>
        public IReadOnlyCollection<string> PurchasedUpgrades => _purchasedUpgrades;

        /// <summary>
        /// Sets a multiplier applied globally (e.g., from prestige).
        /// </summary>
        /// <param name="multiplier">Multiplier value.</param>
        public void SetExternalGlobalMultiplier(double multiplier)
        {
            _externalMultiplier = Math.Max(0d, multiplier);
        }

        /// <summary>
        /// Advances the economy simulation.
        /// </summary>
        /// <param name="deltaTime">Delta time in seconds.</param>
        public void Tick(double deltaTime)
        {
            if (deltaTime <= 0)
            {
                return;
            }

            foreach (var state in _generatorStates.Values)
            {
                if (state.Level <= 0)
                {
                    continue;
                }

                var production = GetGeneratorProductionPerSec(state.Definition.Id) * deltaTime;
                if (production <= 0)
                {
                    continue;
                }

                AddCurrency(state.Definition.CurrencyId, production);
                state.LifetimeProduced += production;
                TotalLifetimeProduced += production;
            }

            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// Adds currency without triggering validation.
        /// </summary>
        public void AddCurrency(string currencyId, double amount)
        {
            if (!_balances.ContainsKey(currencyId))
            {
                _balances[currencyId] = 0;
            }

            _balances[currencyId] += amount;
        }

        /// <summary>
        /// Determines whether the next generator level can be purchased.
        /// </summary>
        public bool CanBuyGeneratorLevel(string generatorId, out string reason)
        {
            reason = string.Empty;
            if (!_generatorStates.TryGetValue(generatorId, out var state))
            {
                reason = "Generator not found";
                return false;
            }

            if (!IsGeneratorUnlocked(state.Definition))
            {
                reason = "Generator locked";
                return false;
            }

            if (state.Definition.MaxLevel.HasValue && state.Level >= state.Definition.MaxLevel.Value)
            {
                reason = "Max level reached";
                return false;
            }

            var cost = GetNextGeneratorCost(generatorId);
            if (!HasCurrency(state.Definition.CurrencyId, cost))
            {
                reason = "Insufficient currency";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Attempts to buy the next level of a generator.
        /// </summary>
        public bool TryBuyGeneratorLevel(string generatorId, out string reason)
        {
            if (!CanBuyGeneratorLevel(generatorId, out reason))
            {
                return false;
            }

            var state = _generatorStates[generatorId];
            var cost = GetNextGeneratorCost(generatorId);
            SpendCurrency(state.Definition.CurrencyId, cost);
            state.Level++;
            OnStateChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Gets the cost for the next generator level after reductions.
        /// </summary>
        public double GetNextGeneratorCost(string generatorId)
        {
            var state = _generatorStates[generatorId];
            var baseCost = CostCurves.GetCost(state.Definition, state.Level);
            var reductionMultiplier = GetCostReductionMultiplier(generatorId);
            return baseCost * reductionMultiplier;
        }

        /// <summary>
        /// Attempts to purchase an upgrade.
        /// </summary>
        public bool CanPurchaseUpgrade(string upgradeId, out string reason)
        {
            reason = string.Empty;
            if (_purchasedUpgrades.Contains(upgradeId))
            {
                reason = "Already purchased";
                return false;
            }

            if (!_content.Upgrades.TryGetValue(upgradeId, out var def))
            {
                reason = "Upgrade not found";
                return false;
            }

            if (!MeetsConditions(def, out reason))
            {
                return false;
            }

            var currencyId = def.Conditions.CurrencyId ?? _content.PrimaryCurrencyId;
            if (!HasCurrency(currencyId, def.Price))
            {
                reason = "Insufficient currency";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Attempts to purchase an upgrade.
        /// </summary>
        public bool TryBuyUpgrade(string upgradeId, out string reason)
        {
            if (!CanPurchaseUpgrade(upgradeId, out reason))
            {
                return false;
            }

            var def = _content.Upgrades[upgradeId];
            var currencyId = def.Conditions.CurrencyId ?? _content.PrimaryCurrencyId;
            SpendCurrency(currencyId, def.Price);
            _purchasedUpgrades.Add(upgradeId);
            RebuildUpgradeModifiers();
            OnStateChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Computes the production per second for the given generator.
        /// </summary>
        public double GetGeneratorProductionPerSec(string generatorId)
        {
            var state = _generatorStates[generatorId];
            if (state.Level <= 0)
            {
                return 0;
            }

            var baseProduction = state.Definition.BaseRatePerSec * state.Level;
            var multiplier = GetMultiplier(generatorId);
            var additive = GetAdditive(generatorId);
            return Math.Max(0, baseProduction * multiplier + additive);
        }

        /// <summary>
        /// Computes total production per second across all generators.
        /// </summary>
        public double GetTotalProductionPerSec()
        {
            double total = 0;
            foreach (var generator in _generatorStates.Keys)
            {
                total += GetGeneratorProductionPerSec(generator);
            }

            return total;
        }

        /// <summary>
        /// Computes offline earnings for a time span.
        /// </summary>
        public OfflineEarningsResult ComputeOfflineEarnings(DateTimeOffset lastSaveUtc, DateTimeOffset nowUtc, double capHours)
        {
            var elapsedHours = (nowUtc - lastSaveUtc).TotalHours;
            var clampedHours = Math.Max(0, Math.Min(elapsedHours, capHours));
            var seconds = clampedHours * 3600d;
            var result = new OfflineEarningsResult(clampedHours, seconds);

            if (seconds <= 0)
            {
                return result;
            }

            foreach (var state in _generatorStates.Values)
            {
                var perSec = GetGeneratorProductionPerSec(state.Definition.Id);
                if (perSec <= 0)
                {
                    continue;
                }

                var amount = perSec * seconds;
                result.Add(state.Definition.CurrencyId, state.Definition.Id, amount);
            }

            return result;
        }

        /// <summary>
        /// Applies offline earnings to current balances.
        /// </summary>
        public void ApplyOfflineEarnings(OfflineEarningsResult result)
        {
            foreach (var currency in result.CurrencyEarnings)
            {
                AddCurrency(currency.Key, currency.Value);
            }
        }

        /// <summary>
        /// Resets runtime state while preserving upgrades.
        /// </summary>
        public void ResetProgress()
        {
            foreach (var state in _generatorStates.Values)
            {
                state.Level = 0;
                state.LifetimeProduced = 0;
            }

            foreach (var key in _balances.Keys)
            {
                _balances[key] = _content.Theme.StartingBalances.TryGetValue(key, out var start) ? start : 0;
            }

            _purchasedUpgrades.Clear();
            RebuildUpgradeModifiers();
            TotalLifetimeProduced = 0;
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// Restores economy values from a save model.
        /// </summary>
        public void LoadState(SaveModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var keys = new List<string>(_balances.Keys);
            foreach (var currency in keys)
            {
                _balances[currency] = model.Currencies.TryGetValue(currency, out var balance) ? balance : _balances[currency];
            }

            foreach (var extra in model.Currencies)
            {
                if (!_balances.ContainsKey(extra.Key))
                {
                    _balances[extra.Key] = extra.Value;
                }
            }

            foreach (var pair in _generatorStates)
            {
                pair.Value.Level = model.Generators.TryGetValue(pair.Key, out var level) ? level : 0;
                pair.Value.LifetimeProduced = model.LifetimePerGenerator.TryGetValue(pair.Key, out var lifetime) ? lifetime : 0;
            }

            _purchasedUpgrades.Clear();
            foreach (var id in model.PurchasedUpgrades)
            {
                _purchasedUpgrades.Add(id);
            }

            RebuildUpgradeModifiers();
            TotalLifetimeProduced = model.TotalLifetimeProduced;
        }

        internal void AddMultiplier(string target, double percent)
        {
            var key = NormalizeTarget(target);
            var current = _multipliers.ContainsKey(key) ? _multipliers[key] : 1d;
            current *= 1d + percent;
            _multipliers[key] = current;
        }

        internal void AddAdditive(string target, double amountPerSec)
        {
            var key = NormalizeTarget(target);
            var current = _additives.ContainsKey(key) ? _additives[key] : 0d;
            _additives[key] = current + amountPerSec;
        }

        internal void AddCostReduction(string target, double percent)
        {
            var key = NormalizeTarget(target);
            var current = _costReductions.ContainsKey(key) ? _costReductions[key] : 1d;
            current *= Math.Max(0.0, 1d - percent);
            _costReductions[key] = current;
        }

        private void RebuildUpgradeModifiers()
        {
            _multipliers.Clear();
            _additives.Clear();
            _costReductions.Clear();

            var context = new UpgradeEffectContext(this);
            foreach (var upgradeId in _purchasedUpgrades)
            {
                var effect = _content.GetUpgradeEffect(upgradeId);
                effect.Apply(context);
            }
        }

        private bool MeetsConditions(UpgradeDef def, out string reason)
        {
            reason = string.Empty;
            var conditions = def.Conditions;
            if (!string.IsNullOrEmpty(conditions.GeneratorId))
            {
                var state = _generatorStates[conditions.GeneratorId];
                if (state.Level < conditions.MinLevel)
                {
                    reason = "Generator level too low";
                    return false;
                }
            }

            if (conditions.MinTotal > 0)
            {
                var currency = conditions.CurrencyId ?? _content.PrimaryCurrencyId;
                if (GetCurrencyBalance(currency) < conditions.MinTotal)
                {
                    reason = "Insufficient total currency";
                    return false;
                }
            }

            return true;
        }

        private bool IsGeneratorUnlocked(GeneratorDef def)
        {
            var req = def.UnlockRequirement;
            if (!string.IsNullOrEmpty(req.CurrencyId) && GetCurrencyBalance(req.CurrencyId) < req.Amount)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(req.GeneratorId) && _generatorStates.TryGetValue(req.GeneratorId, out var state) && state.Level < req.MinLevel)
            {
                return false;
            }

            return true;
        }

        private bool HasCurrency(string currencyId, double amount)
        {
            return GetCurrencyBalance(currencyId) >= amount;
        }

        private double GetCurrencyBalance(string currencyId)
        {
            return _balances.TryGetValue(currencyId, out var value) ? value : 0d;
        }

        private void SpendCurrency(string currencyId, double amount)
        {
            if (!_balances.ContainsKey(currencyId))
            {
                _balances[currencyId] = 0;
            }

            _balances[currencyId] = Math.Max(0, _balances[currencyId] - amount);
        }

        private double GetMultiplier(string generatorId)
        {
            double value = 1d;
            if (_multipliers.TryGetValue(AllGeneratorsTarget, out var global))
            {
                value *= global;
            }

            if (_multipliers.TryGetValue(generatorId, out var specific))
            {
                value *= specific;
            }

            return value * _externalMultiplier;
        }

        private double GetAdditive(string generatorId)
        {
            double value = 0d;
            if (_additives.TryGetValue(AllGeneratorsTarget, out var global))
            {
                value += global;
            }

            if (_additives.TryGetValue(generatorId, out var specific))
            {
                value += specific;
            }

            return value;
        }

        private double GetCostReductionMultiplier(string generatorId)
        {
            double value = 1d;
            if (_costReductions.TryGetValue(AllGeneratorsTarget, out var global))
            {
                value *= global;
            }

            if (_costReductions.TryGetValue(generatorId, out var specific))
            {
                value *= specific;
            }

            return value;
        }

        private static string NormalizeTarget(string target)
        {
            return string.IsNullOrEmpty(target) ? AllGeneratorsTarget : target;
        }
    }

    /// <summary>
    /// Represents runtime state for a generator.
    /// </summary>
    public sealed class GeneratorState
    {
        internal GeneratorState(GeneratorDef def)
        {
            Definition = def;
        }

        /// <summary>
        /// Generator definition.
        /// </summary>
        public GeneratorDef Definition { get; }

        /// <summary>
        /// Current owned level.
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Total lifetime production from this generator.
        /// </summary>
        public double LifetimeProduced { get; set; }
    }

    /// <summary>
    /// Result information for offline earnings calculations.
    /// </summary>
    public sealed class OfflineEarningsResult
    {
        internal OfflineEarningsResult(double hours, double seconds)
        {
            Hours = hours;
            Seconds = seconds;
        }

        /// <summary>
        /// Total offline hours applied.
        /// </summary>
        public double Hours { get; }

        /// <summary>
        /// Total offline seconds applied.
        /// </summary>
        public double Seconds { get; }

        /// <summary>
        /// Gets total currency earnings by currency id.
        /// </summary>
        public Dictionary<string, double> CurrencyEarnings { get; } = new Dictionary<string, double>();

        /// <summary>
        /// Gets total earnings breakdown per generator.
        /// </summary>
        public Dictionary<string, double> GeneratorBreakdown { get; } = new Dictionary<string, double>();

        /// <summary>
        /// Total amount earned across all currencies.
        /// </summary>
        public double TotalEarned { get; private set; }

        internal void Add(string currencyId, string generatorId, double amount)
        {
            if (!CurrencyEarnings.ContainsKey(currencyId))
            {
                CurrencyEarnings[currencyId] = 0;
            }

            CurrencyEarnings[currencyId] += amount;
            if (!GeneratorBreakdown.ContainsKey(generatorId))
            {
                GeneratorBreakdown[generatorId] = 0;
            }

            GeneratorBreakdown[generatorId] += amount;
            TotalEarned += amount;
        }
    }
}
