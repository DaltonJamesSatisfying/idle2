using System;
using UnityEngine;

namespace IdleFramework.Core
{
    /// <summary>
    /// Provides a fixed-step simulation loop for idle gameplay services.
    /// </summary>
    public sealed class TickManager : MonoBehaviour
    {
        private const float DefaultTickRate = 10f;
        private float _accumulator;
        private bool _isRunning;

        /// <summary>
        /// Occurs when the simulation advances by one fixed step.
        /// </summary>
        public event Action<float>? OnTick;

        /// <summary>
        /// Gets or sets the number of ticks executed per second.
        /// </summary>
        public float TickRate { get; private set; } = DefaultTickRate;

        private void Awake()
        {
            Resume();
        }

        private void Update()
        {
            if (!_isRunning)
            {
                return;
            }

            var delta = Time.unscaledDeltaTime;
            _accumulator += delta;
            var step = 1f / Mathf.Max(1f, TickRate);

            while (_accumulator >= step)
            {
                _accumulator -= step;
                OnTick?.Invoke(step);
            }
        }

        /// <summary>
        /// Adjusts the tick rate at runtime.
        /// </summary>
        /// <param name="ticksPerSecond">Desired ticks per second.</param>
        public void SetTickRate(float ticksPerSecond)
        {
            TickRate = Mathf.Max(0.1f, ticksPerSecond);
        }

        /// <summary>
        /// Pauses simulation ticks.
        /// </summary>
        public void Pause()
        {
            _isRunning = false;
        }

        /// <summary>
        /// Resumes simulation ticks.
        /// </summary>
        public void Resume()
        {
            _isRunning = true;
        }
    }
}
