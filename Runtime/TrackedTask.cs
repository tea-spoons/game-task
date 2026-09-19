namespace TeaSpoons.GameTask
{
    using System;

    /// <summary>
    /// A task of a <see cref="TaskTracker"/> with its progress. The tracker changes it, everyone else reads it.
    /// </summary>
    public sealed class TrackedTask
    {
        internal TrackedTask(TaskDefinition definition)
        {
            Definition = definition;
        }

        public TaskDefinition Definition { get; }

        public string Id => Definition.Id;

        public TaskState State { get; internal set; }

        public long Progress { get; internal set; }

        public long Target => Definition.Target;

        /// <summary>
        /// How much of the target is done, from 0 to 1.
        /// </summary>
        public double Fraction => (double)Progress / Definition.Target;

        /// <summary>
        /// When the task became active, or <c>null</c> while it is locked.
        /// </summary>
        public DateTime? ActivatedAtUtc { get; internal set; }

        /// <summary>
        /// When an active task runs out of time, or <c>null</c> if it does not expire or is not active.
        /// </summary>
        public DateTime? ExpiresAtUtc
        {
            get
            {
                var duration = Definition.Duration;
                return State == TaskState.Active && ActivatedAtUtc.HasValue && duration.HasValue
                    ? ActivatedAtUtc.Value + duration.Value
                    : null;
            }
        }

        public override string ToString()
        {
            return $"{Id} {State} {Progress}/{Target}";
        }
    }
}
