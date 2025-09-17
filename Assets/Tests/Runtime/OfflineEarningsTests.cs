using System;
using System.IO;
using IdleFramework.Data;
using IdleFramework.Economy;
using NUnit.Framework;

namespace IdleFramework.Tests.Runtime
{
    public class OfflineEarningsTests
    {
        [Test]
        public void OfflineEarnings_NoElapsedTime()
        {
            var economy = CreateEconomy();
            var now = DateTimeOffset.UtcNow;
            var result = economy.ComputeOfflineEarnings(now, now, 12);
            Assert.AreEqual(0d, result.TotalEarned);
        }

        [Test]
        public void OfflineEarnings_PartialHour()
        {
            var economy = CreateEconomy();
            economy.Generators["gen_oven"].Level = 5;
            var start = DateTimeOffset.UnixEpoch;
            var end = start.AddMinutes(30);
            var result = economy.ComputeOfflineEarnings(start, end, 12);
            Assert.AreEqual(0.2d * 5 * 1800d, result.TotalEarned, 1e-6);
        }

        [Test]
        public void OfflineEarnings_Capped()
        {
            var economy = CreateEconomy();
            economy.Generators["gen_oven"].Level = 5;
            var start = DateTimeOffset.UnixEpoch;
            var end = start.AddHours(20);
            var result = economy.ComputeOfflineEarnings(start, end, 2);
            Assert.AreEqual(0.2d * 5 * 7200d, result.TotalEarned, 1e-6);
        }

        [Test]
        public void OfflineEarnings_MultipleGenerators()
        {
            var economy = CreateEconomy();
            economy.Generators["gen_oven"].Level = 5;
            economy.Generators["gen_factory"].Level = 2;
            var start = DateTimeOffset.UnixEpoch;
            var end = start.AddHours(1);
            var result = economy.ComputeOfflineEarnings(start, end, 12);

            var oven = 0.2d * 5 * 3600d;
            var factory = 2.0d * 2 * 3600d;
            Assert.AreEqual(oven + factory, result.TotalEarned, 1e-6);
            Assert.AreEqual(oven, result.GeneratorBreakdown["gen_oven"], 1e-6);
            Assert.AreEqual(factory, result.GeneratorBreakdown["gen_factory"], 1e-6);
        }

        private static EconomyService CreateEconomy()
        {
            var db = new ContentDB(Path.Combine(FindProjectRoot(), "Assets", "Skins"));
            db.LoadSkin("Classic");
            return new EconomyService(db);
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
