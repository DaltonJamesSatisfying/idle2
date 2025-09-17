using System;
using System.IO;
using System.Text;
using IdleFramework.Core;
using IdleFramework.Data;
using IdleFramework.Economy;
using IdleFramework.Save;
using NUnit.Framework;

namespace IdleFramework.Tests.Runtime
{
    public class SaveServiceTests
    {
        [Test]
        public void SaveService_RoundTripPersistsState()
        {
            var tempDir = CreateTempDirectory();
            var content = LoadContent("Classic");
            var economy = new EconomyService(content);
            var prestige = new PrestigeService(economy, content, new SystemTimeProvider());
            var saveService = new SaveService(content, economy, prestige, new SystemTimeProvider(), saveDirectory: tempDir);

            economy.Generators["gen_oven"].Level = 10;
            economy.AddCurrency("soft", 1234);
            economy.AddCurrency("prestige", 5);
            economy.AddCurrency("soft", 1000);
            Assert.IsTrue(economy.TryBuyUpgrade("upg_hotter_ovens", out _));

            saveService.Save();

            economy.ResetProgress();
            prestige.LoadState(0, DateTimeOffset.UnixEpoch);

            saveService.Load();

            Assert.AreEqual(10, economy.Generators["gen_oven"].Level);
            Assert.IsTrue(economy.PurchasedUpgrades.Contains("upg_hotter_ovens"));
            Assert.Greater(economy.Balances["soft"], 0);
        }

        [Test]
        public void SaveService_MigrationInvoked()
        {
            var tempDir = CreateTempDirectory();
            var content = LoadContent("Classic");
            var economy = new EconomyService(content);
            var prestige = new PrestigeService(economy, content, new SystemTimeProvider());
            var saveService = new SaveService(content, economy, prestige, new SystemTimeProvider(), saveDirectory: tempDir);

            var cipher = new XorSaveCipher("idle-template");
            var legacy = new SaveModel { Version = 0 };
            var json = IdleFramework.Utils.JsonUtils.Serialize(legacy);
            var bytes = cipher.Encode(Encoding.UTF8.GetBytes(json));
            File.WriteAllBytes(Path.Combine(tempDir, "idle_save.dat"), bytes);

            var invoked = false;
            saveService.RegisterMigration(0, model =>
            {
                invoked = true;
                model.Version = 1;
                return model;
            });

            saveService.Load();
            Assert.IsTrue(invoked);
        }

        private static string CreateTempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(path);
            return path;
        }

        private static ContentDB LoadContent(string skin)
        {
            var db = new ContentDB(Path.Combine(FindProjectRoot(), "Assets", "Skins"));
            db.LoadSkin(skin);
            return db;
        }

        private static string FindProjectRoot()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets")))
            {
                dir = dir.Parent;
            }

            return dir?.FullName ?? Directory.GetCurrentDirectory();
        }
    }
}
