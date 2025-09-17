using System;
using IdleFramework.Data.Models;

namespace IdleFramework.Economy
{
    /// <summary>
    /// Provides helpers for evaluating generator cost progression curves.
    /// </summary>
    public static class CostCurves
    {
        /// <summary>
        /// Computes the cost for purchasing the next level given the current level.
        /// </summary>
        /// <param name="generator">Generator definition.</param>
        /// <param name="currentLevel">Current level owned.</param>
        /// <returns>Cost of the next level.</returns>
        public static double GetCost(GeneratorDef generator, int currentLevel)
        {
            var n = Math.Max(0, currentLevel);
            return generator.CostCurve.Type.ToLowerInvariant() switch
            {
                "linear" => Linear(generator.BaseCost, generator.CostCurve.Step, n),
                "geometric" => Geometric(generator.BaseCost, generator.CostCurve.Growth, n),
                "polynomial" => Polynomial(generator.BaseCost, generator.CostCurve.A, generator.CostCurve.B, n),
                _ => throw new ArgumentOutOfRangeException(nameof(generator), $"Unknown cost curve {generator.CostCurve.Type}")
            };
        }

        /// <summary>
        /// Linear curve implementation <c>base + step * n</c>.
        /// </summary>
        public static double Linear(double baseCost, double step, int n)
        {
            return baseCost + step * n;
        }

        /// <summary>
        /// Geometric curve implementation <c>base * growth^n</c>.
        /// </summary>
        public static double Geometric(double baseCost, double growth, int n)
        {
            return baseCost * Math.Pow(growth, n);
        }

        /// <summary>
        /// Polynomial curve implementation <c>base * (1 + a*n + b*n^2)</c>.
        /// </summary>
        public static double Polynomial(double baseCost, double a, double b, int n)
        {
            return baseCost * (1d + a * n + b * n * n);
        }
    }
}
