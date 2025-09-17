using System;
using System.Collections.Generic;
using System.IO;
using IdleFramework.Core;
using IdleFramework.Data;
using IdleFramework.Economy;
using IdleFramework.Utils;

namespace IdleFramework.Save
{
    /// <summary>
    /// Handles persistence of economy state to disk.
    /// </summary>
    public sealed class SaveService
    {
        private const string FileName = "idle_save.dat";
        private readonly EconomyService _economy;
        private readonly PrestigeService _prestige;
        private readonly ITimeProvider _timeProvider;
        private readonly ISaveCipher _cipher;
        private readonly Dictionary<int, Func<SaveModel, SaveModel>> _migrations = new Dictionary<int, Func<SaveModel, SaveModel>>();
        private readonly string _saveDirectory;
        private readonly double _autoSaveInterval;
        private double _accumulator;

        /// <summary>
        /// Gets the current save file path.
        /// </summary>
        public string SavePath => Path.Combine(_saveDirectory, FileName);

        /// <summary>
        /// Gets the current save version.
        /// </summary>
        public int CurrentVersion { get; } = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveService"/> class.
        /// </summary>
        public SaveService(ContentDB content, EconomyService economy, PrestigeService prestige, ITimeProvider timeProvider, ISaveCipher? cipher = null, string? saveDirectory = null, double autoSaveIntervalSeconds = 30d)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            _economy = economy;
            _prestige = prestige;
            _timeProvider = timeProvider;
            _cipher = cipher ?? new XorSaveCipher("idle-template");
            _saveDirectory = saveDirectory ?? GetDefaultDirectory();
            _autoSaveInterval = autoSaveIntervalSeconds;
            Directory.CreateDirectory(_saveDirectory);
        }

        /// <summary>
        /// Registers a migration invoked when loading older saves.
        /// </summary>
        public void RegisterMigration(int version, Func<SaveModel, SaveModel> migration)
        {
            _migrations[version] = migration;
        }

        /// <summary>
        /// Should be called regularly to handle autosaving.
        /// </summary>
        public void Update(double deltaTime)
        {
            _accumulator += deltaTime;
            if (_accumulator < _autoSaveInterval)
            {
                return;
            }

            _accumulator = 0;
            Save();
        }

        /// <summary>
        /// Saves the current state to disk.
        /// </summary>
        public void Save()
        {
            var model = BuildModel();
            var json = JsonUtils.Serialize(model);
            var encoded = _cipher.Encode(System.Text.Encoding.UTF8.GetBytes(json));
            File.WriteAllBytes(SavePath, encoded);
        }

        /// <summary>
        /// Loads the current save file if present.
        /// </summary>
        public SaveModel Load()
        {
            if (!File.Exists(SavePath))
            {
                return new SaveModel { Version = CurrentVersion };
            }

            var encoded = File.ReadAllBytes(SavePath);
            var json = System.Text.Encoding.UTF8.GetString(_cipher.Decode(encoded));
            var model = JsonUtils.Deserialize<SaveModel>(json);
            model = RunMigrations(model);
            Apply(model);
            return model;
        }

        /// <summary>
        /// Applies a save model to runtime services.
        /// </summary>
        public void Apply(SaveModel model)
        {
            _economy.LoadState(model);
            _prestige.LoadState(model.PrestigeCurrency, model.LastPrestigeUtc);
        }

        private SaveModel BuildModel()
        {
            var model = new SaveModel
            {
                Version = CurrentVersion,
                LastSaveUtc = _timeProvider.UtcNow,
                TotalLifetimeProduced = _economy.TotalLifetimeProduced,
                PrestigeCurrency = _prestige.PrestigeCurrency,
                LastPrestigeUtc = _prestige.LastPrestigeUtc
            };

            foreach (var currency in _economy.Balances)
            {
                model.Currencies[currency.Key] = currency.Value;
            }

            foreach (var pair in _economy.Generators)
            {
                model.Generators[pair.Key] = pair.Value.Level;
                model.LifetimePerGenerator[pair.Key] = pair.Value.LifetimeProduced;
            }

            foreach (var upgrade in _economy.PurchasedUpgrades)
            {
                model.PurchasedUpgrades.Add(upgrade);
            }

            return model;
        }

        private SaveModel RunMigrations(SaveModel model)
        {
            var current = model;
            while (current.Version < CurrentVersion)
            {
                if (_migrations.TryGetValue(current.Version, out var migration))
                {
                    current = migration(current);
                }

                current.Version++;
            }

            return current;
        }

        private static string GetDefaultDirectory()
        {
#if UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE || UNITY_EDITOR
            return UnityEngine.Application.persistentDataPath;
#else
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IdleTemplate");
#endif
        }
    }
}
