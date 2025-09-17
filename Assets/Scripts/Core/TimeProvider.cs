using System;

namespace IdleFramework.Core
{
    /// <summary>
    /// Abstraction for retrieving the current UTC time.
    /// </summary>
    public interface ITimeProvider
    {
        /// <summary>
        /// Gets the current UTC timestamp.
        /// </summary>
        DateTimeOffset UtcNow { get; }
    }

    /// <summary>
    /// Default implementation that reads from <see cref="DateTimeOffset.UtcNow"/>.
    /// </summary>
    public sealed class SystemTimeProvider : ITimeProvider
    {
        /// <inheritdoc />
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
