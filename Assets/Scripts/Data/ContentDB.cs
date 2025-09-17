using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IdleFramework.Data.Models;
using IdleFramework.Economy;
using IdleFramework.Utils;

namespace IdleFramework.Data
{
    /// <summary>
    /// Loads and validates content definitions for a selected skin.
    /// </summary>
    public sealed class ContentDB
    {
        private readonly Dictionary<string, CurrencyDef> _currencies = new Dictionary<string, CurrencyDef>();
        private readonly Dictionary<string, GeneratorDef> _generators = new Dictionary<string, GeneratorDef>();
        private readonly Dictionary<string, UpgradeDef> _upgrades = new Dictionary<string, UpgradeDef>();
        private readonly Dictionary<string, AchievementDef> _achievements = new Dictionary<string, AchievementDef>();
        private readonly Dictionary<string, IUpgradeEffect> _upgradeEffects = new Dictionary<string, IUpgradeEffect>();
        private readonly string _skinsRoot;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentDB"/> class.
        /// </summary>
        /// <param name="skinsRoot">Optional root path for skin JSON files.</param>
        public ContentDB(string? skinsRoot = null)
        {
            _skinsRoot = skinsRoot ?? GetDefaultSkinRoot();
        }

        /// <summary>
        /// Gets the active theme definition.
        /// </summary>
        public ThemeDef Theme { get; private set; } = new ThemeDef();

        /// <summary>
        /// Gets the current skin name.
        /// </summary>
        public string SkinName { get; private set; } = string.Empty;

        /// <summary>
        /// Gets all currencies keyed by identifier.
        /// </summary>
        public IReadOnlyDictionary<string, CurrencyDef> Currencies => _currencies;

        /// <summary>
        /// Gets all generator definitions keyed by identifier.
        /// </summary>
        public IReadOnlyDictionary<string, GeneratorDef> Generators => _generators;

        /// <summary>
        /// Gets all upgrade definitions keyed by identifier.
        /// </summary>
        public IReadOnlyDictionary<string, UpgradeDef> Upgrades => _upgrades;

        /// <summary>
        /// Gets all achievements keyed by identifier.
        /// </summary>
        public IReadOnlyDictionary<string, AchievementDef> Achievements => _achievements;

        /// <summary>
        /// Gets the default soft currency identifier.
        /// </summary>
        public string PrimaryCurrencyId => _currencies.Count > 0 ? _currencies.Values.First().Id : string.Empty;

        /// <summary>
        /// Retrieves the runtime effect associated with an upgrade.
        /// </summary>
        public IUpgradeEffect GetUpgradeEffect(string upgradeId)
        {
            if (_upgradeEffects.TryGetValue(upgradeId, out var effect))
            {
                return effect;
            }

            throw new KeyNotFoundException($"Upgrade effect not found for id {upgradeId}.");
        }

        /// <summary>
        /// Loads a skin and validates its definitions.
        /// </summary>
        public void LoadSkin(string skinName)
        {
            if (string.IsNullOrWhiteSpace(skinName))
            {
                throw new ArgumentException("Skin name is required", nameof(skinName));
            }

            SkinName = skinName;
            _currencies.Clear();
            _generators.Clear();
            _upgrades.Clear();
            _achievements.Clear();
            _upgradeEffects.Clear();

            var skinFolder = Path.Combine(_skinsRoot, skinName);
            if (!Directory.Exists(skinFolder))
            {
                throw new DirectoryNotFoundException($"Skin folder not found: {skinFolder}");
            }

            LoadCurrencies(Path.Combine(skinFolder, "currencies.json"));
            LoadGenerators(Path.Combine(skinFolder, "generators.json"));
            LoadUpgrades(Path.Combine(skinFolder, "upgrades.json"));
            LoadAchievements(Path.Combine(skinFolder, "achievements.json"));
            Theme = JsonUtils.DeserializeFromFile<ThemeDef>(Path.Combine(skinFolder, "theme.json"));

            ApplyDefaults();
            ValidateReferences();
        }

        private void LoadCurrencies(string path)
        {
            var items = JsonUtils.DeserializeFromFile<List<CurrencyDef>>(path);
            foreach (var item in items)
            {
                _currencies[item.Id] = item;
            }
        }

        private void LoadGenerators(string path)
        {
            var items = JsonUtils.DeserializeFromFile<List<GeneratorDef>>(path);
            foreach (var item in items)
            {
                _generators[item.Id] = item;
            }
        }

        private void LoadUpgrades(string path)
        {
            var items = JsonUtils.DeserializeFromFile<List<UpgradeDef>>(path);
            foreach (var item in items)
            {
                _upgrades[item.Id] = item;
                _upgradeEffects[item.Id] = UpgradeEffectFactory.Create(item.Effect);
            }
        }

        private void LoadAchievements(string path)
        {
            var items = JsonUtils.DeserializeFromFile<List<AchievementDef>>(path);
            foreach (var item in items)
            {
                _achievements[item.Id] = item;
            }
        }

        private void ApplyDefaults()
        {
            foreach (var currency in _currencies.Values)
            {
                if (!Theme.StartingBalances.ContainsKey(currency.Id))
                {
                    Theme.StartingBalances[currency.Id] = currency.Start;
                }
            }
        }

        private void ValidateReferences()
        {
            foreach (var generator in _generators.Values)
            {
                if (!_currencies.ContainsKey(generator.CurrencyId))
                {
                    throw new InvalidDataException($"Generator {generator.Id} references missing currency {generator.CurrencyId}.");
                }

                if (!string.IsNullOrEmpty(generator.UnlockRequirement.CurrencyId) && !_currencies.ContainsKey(generator.UnlockRequirement.CurrencyId))
                {
                    throw new InvalidDataException($"Generator {generator.Id} unlock requires missing currency {generator.UnlockRequirement.CurrencyId}.");
                }

                if (!string.IsNullOrEmpty(generator.UnlockRequirement.GeneratorId) && !_generators.ContainsKey(generator.UnlockRequirement.GeneratorId))
                {
                    throw new InvalidDataException($"Generator {generator.Id} unlock requires missing generator {generator.UnlockRequirement.GeneratorId}.");
                }
            }

            foreach (var upgrade in _upgrades.Values)
            {
                if (!string.IsNullOrEmpty(upgrade.Conditions.GeneratorId) && !_generators.ContainsKey(upgrade.Conditions.GeneratorId))
                {
                    throw new InvalidDataException($"Upgrade {upgrade.Id} references missing generator {upgrade.Conditions.GeneratorId}.");
                }

                if (!string.IsNullOrEmpty(upgrade.Conditions.CurrencyId) && !_currencies.ContainsKey(upgrade.Conditions.CurrencyId))
                {
                    throw new InvalidDataException($"Upgrade {upgrade.Id} references missing currency {upgrade.Conditions.CurrencyId}.");
                }

                if (!string.Equals(upgrade.Effect.Target, "all", StringComparison.OrdinalIgnoreCase) && !_generators.ContainsKey(upgrade.Effect.Target))
                {
                    throw new InvalidDataException($"Upgrade {upgrade.Id} effect targets missing generator {upgrade.Effect.Target}.");
                }
            }
        }

        private static string GetDefaultSkinRoot()
        {
#if UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || UNITY_EDITOR
            return Path.Combine(UnityEngine.Application.dataPath, "Skins");
#else
            return Path.Combine("Assets", "Skins");
#endif
        }
    }
}
