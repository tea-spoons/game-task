namespace TeaSpoons.GameTask
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Keeps the progress of a set of tasks. Report what the player does with <see cref="Report"/>, and the tracker
    /// counts it for the tasks that ask for it.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item>Tasks without prerequisites start <see cref="TaskState.Active"/>, the others <see cref="TaskState.Locked"/>.</item>
    ///   <item>A task that reaches its target becomes <see cref="TaskState.Completed"/>; <see cref="TryClaim"/> makes it <see cref="TaskState.Claimed"/>.</item>
    ///   <item>A task with a duration <see cref="TaskState.Expired"/> when the time runs out. Expiry is checked by <see cref="Tick"/>, <see cref="Report"/> and <see cref="TryClaim"/>.</item>
    ///   <item>Nothing here is thread safe or asynchronous. Call it from the main thread.</item>
    /// </list>
    /// </remarks>
    public sealed class TaskTracker
    {
        private readonly TrackedTask[] tasks;
        private readonly Dictionary<string, TrackedTask> tasksById;
        private readonly IClock clock;

        /// <exception cref="ArgumentException">A definition is invalid, an id is used twice, a prerequisite does not exist or the prerequisites form a cycle.</exception>
        public TaskTracker(IEnumerable<TaskDefinition> definitions, IClock clock = null)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));

            this.clock = clock ?? new SystemClock();

            var list = new List<TrackedTask>();
            tasksById = new Dictionary<string, TrackedTask>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                if (definition == null) throw new ArgumentException("A task definition is null.", nameof(definitions));

                var problem = definition.Validate();
                if (problem != null) throw new ArgumentException($"Task '{definition.Id}' is invalid: {problem}.", nameof(definitions));

                var task = new TrackedTask(definition);
                if (!tasksById.TryAdd(definition.Id, task))
                {
                    throw new ArgumentException($"The task id '{definition.Id}' is used twice.", nameof(definitions));
                }

                list.Add(task);
            }

            tasks = list.ToArray();

            foreach (var task in tasks)
            {
                foreach (var prerequisite in task.Definition.Prerequisites)
                {
                    if (!tasksById.ContainsKey(prerequisite))
                    {
                        throw new ArgumentException($"Task '{task.Id}' needs '{prerequisite}', which does not exist.", nameof(definitions));
                    }
                }
            }

            ThrowOnCycle();

            var now = this.clock.UtcNow;
            foreach (var task in tasks)
            {
                if (task.Definition.Prerequisites.Count == 0)
                {
                    task.State = TaskState.Active;
                    task.ActivatedAtUtc = now;
                }
            }
        }

        /// <summary>A task became active because its prerequisites are done.</summary>
        public event Action<TrackedTask> TaskActivated;

        /// <summary>The progress of a task changed.</summary>
        public event Action<TrackedTask> ProgressChanged;

        /// <summary>A task reached its target. Raised after <see cref="ProgressChanged"/>.</summary>
        public event Action<TrackedTask> TaskCompleted;

        /// <summary>A completed task was claimed.</summary>
        public event Action<TrackedTask> TaskClaimed;

        /// <summary>An active task ran out of time.</summary>
        public event Action<TrackedTask> TaskExpired;

        /// <summary>All tasks, in the order of the definitions.</summary>
        public IReadOnlyList<TrackedTask> Tasks => tasks;

        public bool TryGet(string id, out TrackedTask task)
        {
            if (id == null)
            {
                task = null;
                return false;
            }

            return tasksById.TryGetValue(id, out task);
        }

        /// <exception cref="KeyNotFoundException">There is no task with the id.</exception>
        public TrackedTask Get(string id)
        {
            return TryGet(id, out var task) ? task : throw new KeyNotFoundException($"There is no task '{id}'.");
        }

        /// <summary>
        /// Tells the tracker that the player did something: killed 3 enemies, won a match, reached level 12.
        /// </summary>
        /// <param name="action">What happened. Tasks with this action key count it.</param>
        /// <param name="amount">How much. For <see cref="ProgressMode.Highest"/> tasks this is the value reached.</param>
        /// <param name="arguments">Details of what happened, matched against the arguments the tasks require.</param>
        public void Report(string action, long amount = 1, IReadOnlyDictionary<string, string> arguments = null)
        {
            if (string.IsNullOrEmpty(action)) throw new ArgumentException("An action is required.", nameof(action));
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), amount, "The amount cannot be negative.");

            ExpireOverdue();

            if (amount == 0)
            {
                return;
            }

            // Decide first which tasks count this report. A task that a completion unlocks during the report does not.
            List<TrackedTask> counting = null;
            foreach (var task in tasks)
            {
                var definition = task.Definition;
                if (task.State == TaskState.Active &&
                    string.Equals(definition.ActionKey, action, StringComparison.Ordinal) &&
                    definition.Matches(arguments))
                {
                    (counting ??= new List<TrackedTask>()).Add(task);
                }
            }

            if (counting == null)
            {
                return;
            }

            foreach (var task in counting)
            {
                if (task.State != TaskState.Active)
                {
                    continue;   // changed by a handler of an earlier task in this report
                }

                var definition = task.Definition;
                var progress = definition.ProgressMode == ProgressMode.Accumulate
                    ? (amount > definition.Target - task.Progress ? definition.Target : task.Progress + amount)
                    : Math.Min(Math.Max(task.Progress, amount), definition.Target);

                if (progress == task.Progress)
                {
                    continue;
                }

                task.Progress = progress;
                ProgressChanged?.Invoke(task);

                if (progress >= definition.Target)
                {
                    task.State = TaskState.Completed;
                    TaskCompleted?.Invoke(task);
                    ActivateUnlocked(silent: false);
                }
            }
        }

        /// <summary>
        /// Claims a completed task.
        /// </summary>
        /// <returns><c>false</c> if there is no such task or it is not completed.</returns>
        public bool TryClaim(string id)
        {
            ExpireOverdue();

            if (!TryGet(id, out var task) || task.State != TaskState.Completed)
            {
                return false;
            }

            task.State = TaskState.Claimed;
            TaskClaimed?.Invoke(task);
            ActivateUnlocked(silent: false);
            return true;
        }

        /// <summary>
        /// Expires the active tasks that ran out of time. Call it now and then when tasks have durations.
        /// </summary>
        public void Tick()
        {
            ExpireOverdue();
        }

        /// <summary>
        /// The progress of every task, to save.
        /// </summary>
        public TaskTrackerSnapshot Save()
        {
            return new TaskTrackerSnapshot(tasks
                .Select(task => new TaskSnapshot(task.Id, task.State, task.Progress, task.ActivatedAtUtc))
                .ToList());
        }

        /// <summary>
        /// Puts saved progress back. Tasks that are not in the snapshot keep the state they have, and entries for
        /// tasks that no longer exist are ignored. No events are raised.
        /// </summary>
        public void Restore(TaskTrackerSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            var now = clock.UtcNow;
            foreach (var saved in snapshot.Tasks)
            {
                if (!tasksById.TryGetValue(saved.Id, out var task) || !saved.TryGetState(out var state))
                {
                    continue;
                }

                task.State = state;
                task.Progress = state == TaskState.Completed || state == TaskState.Claimed
                    ? task.Target
                    : Math.Max(0, Math.Min(saved.Progress, task.Target));
                task.ActivatedAtUtc = saved.ActivatedAtUtc ?? (state == TaskState.Locked ? (DateTime?)null : now);
            }

            // Tasks the snapshot did not know (for example added by an update) may be unlocked by the restored ones.
            ActivateUnlocked(silent: true);
        }

        private void ExpireOverdue()
        {
            var now = clock.UtcNow;
            foreach (var task in tasks)
            {
                var expiresAt = task.ExpiresAtUtc;
                if (expiresAt.HasValue && now >= expiresAt.Value)
                {
                    task.State = TaskState.Expired;
                    TaskExpired?.Invoke(task);
                }
            }
        }

        private void ActivateUnlocked(bool silent)
        {
            var now = clock.UtcNow;
            foreach (var task in tasks)
            {
                if (task.State != TaskState.Locked || !PrerequisitesAreDone(task))
                {
                    continue;
                }

                task.State = TaskState.Active;
                task.ActivatedAtUtc = now;
                if (!silent)
                {
                    TaskActivated?.Invoke(task);
                }
            }
        }

        private bool PrerequisitesAreDone(TrackedTask task)
        {
            var needsClaim = task.Definition.UnlockOn == UnlockCondition.PrerequisiteClaimed;
            foreach (var id in task.Definition.Prerequisites)
            {
                var state = tasksById[id].State;
                var done = needsClaim
                    ? state == TaskState.Claimed
                    : state == TaskState.Completed || state == TaskState.Claimed;

                if (!done)
                {
                    return false;
                }
            }

            return true;
        }

        private void ThrowOnCycle()
        {
            // 0 = not visited, 1 = on the current path, 2 = done
            var marks = new Dictionary<string, int>(StringComparer.Ordinal);

            void Visit(TrackedTask task)
            {
                marks.TryGetValue(task.Id, out var mark);
                if (mark == 2) return;
                if (mark == 1) throw new ArgumentException($"The prerequisites of task '{task.Id}' lead back to itself.");

                marks[task.Id] = 1;
                foreach (var prerequisite in task.Definition.Prerequisites)
                {
                    Visit(tasksById[prerequisite]);
                }

                marks[task.Id] = 2;
            }

            foreach (var task in tasks)
            {
                Visit(task);
            }
        }
    }
}
