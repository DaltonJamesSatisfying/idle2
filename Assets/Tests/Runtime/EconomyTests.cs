using System.IO;
using IdleFramework.Data;
using IdleFramework.Economy;
using NUnit.Framework;

namespace IdleFramework.Tests.Runtime
{
    public class EconomyTests
    {
        [Test]
        public void LinearCost_FollowsFormula()
        {
            Assert.AreEqual(25d, CostCurves.Linear(10d, 5d, 3));
        }

        [Test]
        public void GeometricCost_FollowsFormula()
        {
            Assert.AreEqual(80d, CostCurves.Geometric(10d, 2d, 3));
        }

        [Test]
        public void PolynomialCost_FollowsFormula()
        {
            var expected = 10d * (1d + 0.1d * 4 + 0.01d * 16);
            Assert.AreEqual(expected, CostCurves.Polynomial(10d, 0.1d, 0.01d, 4));
        }

        [Test]
        public void Upgrades_MultiplierStackingApplied()
        {
            var content = LoadContent("Classic");
            var economy = new EconomyService(content);
            var oven = economy.Generators["gen_oven"];
            oven.Level = 10;
            economy.AddCurrency("soft", 5000);

            Assert.IsTrue(economy.TryBuyUpgrade("upg_hotter_ovens", out _));
            Assert.IsTrue(economy.TryBuyUpgrade("upg_universal_fans", out _));

            var baseRate = content.Generators["gen_oven"].BaseRatePerSec * oven.Level;
            var expected = baseRate * (1 + 0.25d) * (1 + 0.15d);
            Assert.AreEqual(expected, economy.GetGeneratorProductionPerSec("gen_oven"), 1e-6);
        }

        [Test]
        public void Upgrades_AdditiveAndCostReduction()
        {
            var content = LoadContent("Neon");
            var economy = new EconomyService(content);
            economy.Generators["gen_drone"].Level = 12;
            economy.Generators["gen_reactor"].Level = 3;
            economy.AddCurrency("soft", 10000);

            Assert.IsTrue(economy.TryBuyUpgrade("upg_drone_swarm", out _));
            Assert.IsTrue(economy.TryBuyUpgrade("upg_energy_market", out _));

            var baseDrone = content.Generators["gen_drone"].BaseRatePerSec * 12;
            var production = economy.GetGeneratorProductionPerSec("gen_drone");
            Assert.AreEqual(baseDrone + 1.0d, production, 1e-6);

            var baseCost = CostCurves.GetCost(content.Generators["gen_drone"], 12);
            var discounted = economy.GetNextGeneratorCost("gen_drone");
            Assert.AreEqual(baseCost * 0.9d, discounted, 1e-6);
        }

        private static ContentDB LoadContent(string skin)
        {
            var root = FindProjectRoot();
            var db = new ContentDB(Path.Combine(root, "Assets", "Skins"));
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
