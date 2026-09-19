namespace TeaSpoons.GameTask
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// The progress of a <see cref="TaskTracker"/> in a form you can save: <see cref="ToJson"/> and <see cref="FromJson"/>.
    /// </summary>
    [Serializable]
    public class TaskTrackerSnapshot
    {
        [SerializeField]
        private List<TaskSnapshot> tasks = new();

        public TaskTrackerSnapshot()
        {
        }

        internal TaskTrackerSnapshot(List<TaskSnapshot> tasks)
        {
            this.tasks = tasks;
        }

        public IReadOnlyList<TaskSnapshot> Tasks => tasks;

        /// <exception cref="FormatException">The text is not a snapshot.</exception>
        public static TaskTrackerSnapshot FromJson(string json)
        {
            try
            {
                return JsonUtility.FromJson<TaskTrackerSnapshot>(json) ?? throw new FormatException("The text is not a task snapshot.");
            }
            catch (ArgumentException exception)
            {
                throw new FormatException("The text is not a task snapshot.", exception);
            }
        }

        public string ToJson(bool prettyPrint = false)
        {
            return JsonUtility.ToJson(this, prettyPrint);
        }
    }

    /// <summary>
    /// The saved state of one task.
    /// </summary>
    [Serializable]
    public struct TaskSnapshot
    {
        [SerializeField]
        private string id;

        [SerializeField]
        private string state;

        [SerializeField]
        private long progress;

        [SerializeField]
        private long activatedAtTicks;

        public TaskSnapshot(string id, TaskState state, long progress, DateTime? activatedAtUtc)
        {
            this.id = id;
            this.state = state.ToString();
            this.progress = progress;
            activatedAtTicks = activatedAtUtc.HasValue ? activatedAtUtc.Value.ToUniversalTime().Ticks : 0;
        }

        public string Id => id ?? string.Empty;

        /// <summary>
        /// <c>false</c> if the saved state is not one this version knows.
        /// </summary>
        public bool TryGetState(out TaskState result)
        {
            return Enum.TryParse(state, out result) && Enum.IsDefined(typeof(TaskState), result);
        }

        public long Progress => progress;

        public DateTime? ActivatedAtUtc => activatedAtTicks > 0 ? new DateTime(activatedAtTicks, DateTimeKind.Utc) : null;
    }
}
