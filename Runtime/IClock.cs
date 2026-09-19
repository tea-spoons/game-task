namespace TeaSpoons.GameTask
{
    using System;

    /// <summary>
    /// The time source of a <see cref="TaskTracker"/>. Replace it to test expiry, or to use server time.
    /// </summary>
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    /// <summary>
    /// The system clock.
    /// </summary>
    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
