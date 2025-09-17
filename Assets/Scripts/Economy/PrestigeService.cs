using System;
using IdleFramework.Core;
using IdleFramework.Data;

namespace IdleFramework.Economy
{
    /// <summary>
    /// Handles prestige conversions and meta progression.
    /// </summary>
    public sealed class PrestigeService
    {
        private readonly EconomyService _economy;
        private readonly ContentDB _content;
        private readonly ITimeProvider _timeProvider;
        private double _prestigeCurrency;

        /// <summary>
        /// Occurs after a prestige reset.
        /// </summary>
        public event Action<int>? OnPrestige;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrestigeService"/> class.
        /// </summary>
        public PrestigeService(EconomyService economy, ContentDB content, ITimeProvider timeProvider)
        {
            _economy = economy;
            _content = content;
            UpdateMultiplier();
            LastPrestigeUtc = timeProvider.UtcNow;
            _timeProvider = timeProvider;
        }

        /// <summary>
        /// Gets the accumulated prestige currency.
        /// </summary>
        public double PrestigeCurrency => _prestigeCurrency;

        /// <summary>
        /// Gets the timestamp of the last prestige action.
        /// </summary>
        public DateTimeOffset LastPrestigeUtc { get; private set; }

        /// <summary>
        /// Calculates prestige payout for the current progress.
        /// </summary>
        public int PreviewPrestige()
        {
            return CalculatePrestige(_economy.TotalLifetimeProduced);
        }

        /// <summary>
        /// Executes prestige, resetting progress and awarding meta currency.
        /// </summary>
        public int PerformPrestige()
        {
            var earned = PreviewPrestige();
            if (earned <= 0)
            {
                return 0;
            }

            _prestigeCurrency += earned;
            _economy.ResetProgress();
            UpdateMultiplier();
            LastPrestigeUtc = _timeProvider.UtcNow;
            OnPrestige?.Invoke(earned);
            return earned;
        }

        /// <summary>
        /// Converts total production into prestige currency.
        /// </summary>
        /// <param name="totalLifetimeProduced">Lifetime production value.</param>
        /// <returns>Prestige payout.</returns>
        public int CalculatePrestige(double totalLifetimeProduced)
        {
            var formula = _content.Theme.PrestigeFormula;
            var safeB = Math.Max(1d, formula.B);
            var value = formula.A * Math.Sqrt(Math.Max(0d, totalLifetimeProduced) / safeB);
            return (int)Math.Floor(value);
        }

        private void UpdateMultiplier()
        {
            var multiplier = 1d + _prestigeCurrency * 0.05d;
            _economy.SetExternalGlobalMultiplier(multiplier);
        }

        /// <summary>
        /// Restores saved prestige state.
        /// </summary>
        public void LoadState(double prestigeCurrency, DateTimeOffset lastPrestigeUtc)
        {
            _prestigeCurrency = prestigeCurrency;
            LastPrestigeUtc = lastPrestigeUtc;
            UpdateMultiplier();
        }
    }
}
