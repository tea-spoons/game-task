namespace TeaSpoons.GameTask
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    /// <summary>
    /// What a task asks for. Progress is reported by <see cref="ActionKey"/>: a task with the action
    /// <c>kill-enemy</c> and a target of 10 completes after <c>tracker.Report("kill-enemy", 10)</c> in any split.
    /// </summary>
    [Serializable]
    public class TaskDefinition
    {
        [SerializeField]
        private string id;

        [SerializeField]
        private string actionKey;

        [SerializeField]
        private long target = 1;

        [SerializeField]
        private ProgressMode progressMode;

        [SerializeField]
        private List<string> prerequisites = new();

        [SerializeField]
        private UnlockCondition unlockOn = UnlockCondition.PrerequisiteClaimed;

        [SerializeField]
        private List<TaskArgument> requiredArguments = new();

        [SerializeField]
        private long durationSeconds;

        [SerializeField]
        private string titleKey;

        [SerializeField]
        private string descriptionKey;

        [SerializeField]
        private string rewardKey;

        /// <summary>
        /// For serialization. Use the other constructor in code.
        /// </summary>
        public TaskDefinition()
        {
        }

        /// <param name="id">Unique among the tasks of a tracker.</param>
        /// <param name="actionKey">The action whose reports count for this task.</param>
        /// <param name="target">The progress at which the task is complete.</param>
        /// <param name="prerequisites">Ids of tasks that have to be done first.</param>
        /// <param name="requiredArguments">Only reports that carry all of these arguments count.</param>
        /// <param name="durationSeconds">How long the task stays active. 0 means forever.</param>
        /// <param name="titleKey">Defaults to <c>task.{id}.title</c>.</param>
        /// <param name="descriptionKey">Defaults to <c>task.{id}.description</c>.</param>
        /// <param name="rewardKey">Names the reward of the task, see the Game Reward integration.</param>
        public TaskDefinition(
            string id,
            string actionKey,
            long target = 1,
            ProgressMode progressMode = ProgressMode.Accumulate,
            IEnumerable<string> prerequisites = null,
            UnlockCondition unlockOn = UnlockCondition.PrerequisiteClaimed,
            IEnumerable<KeyValuePair<string, string>> requiredArguments = null,
            long durationSeconds = 0,
            string titleKey = null,
            string descriptionKey = null,
            string rewardKey = null)
        {
            this.id = id;
            this.actionKey = actionKey;
            this.target = target;
            this.progressMode = progressMode;
            this.prerequisites = prerequisites != null ? new List<string>(prerequisites) : new List<string>();
            this.unlockOn = unlockOn;
            this.requiredArguments = requiredArguments != null
                ? requiredArguments.Select(pair => new TaskArgument(pair.Key, pair.Value)).ToList()
                : new List<TaskArgument>();
            this.durationSeconds = durationSeconds;
            this.titleKey = titleKey;
            this.descriptionKey = descriptionKey;
            this.rewardKey = rewardKey;
        }

        public string Id => id ?? string.Empty;

        public string ActionKey => actionKey ?? string.Empty;

        public long Target => target;

        public ProgressMode ProgressMode => progressMode;

        public IReadOnlyList<string> Prerequisites => prerequisites;

        public UnlockCondition UnlockOn => unlockOn;

        public IReadOnlyList<TaskArgument> RequiredArguments => requiredArguments;

        /// <summary>
        /// How long the task stays active after it became active. <c>null</c> means it never expires.
        /// </summary>
        public TimeSpan? Duration => durationSeconds > 0 ? TimeSpan.FromSeconds(durationSeconds) : null;

        public string TitleKey => string.IsNullOrEmpty(titleKey) ? "task." + Id + ".title" : titleKey;

        public string DescriptionKey => string.IsNullOrEmpty(descriptionKey) ? "task." + Id + ".description" : descriptionKey;

        public string RewardKey => rewardKey ?? string.Empty;

        public bool HasReward => !string.IsNullOrEmpty(rewardKey);

        /// <summary>
        /// Whether a report with these arguments counts for the task: it has to carry every required argument.
        /// </summary>
        public bool Matches(IReadOnlyDictionary<string, string> arguments)
        {
            foreach (var required in requiredArguments)
            {
                if (arguments == null ||
                    !arguments.TryGetValue(required.Key, out var value) ||
                    !string.Equals(value, required.Value, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Describes what is wrong with the definition, or returns <c>null</c> if it is fine.
        /// </summary>
        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(id)) return "it has no id";
            if (string.IsNullOrWhiteSpace(actionKey)) return "it has no action key";
            if (target < 1) return "the target must be at least 1";
            if (durationSeconds < 0) return "the duration cannot be negative";

            foreach (var prerequisite in prerequisites)
            {
                if (string.IsNullOrWhiteSpace(prerequisite)) return "a prerequisite is empty";
                if (prerequisite == id) return "it lists itself as a prerequisite";
            }

            if (prerequisites.Distinct().Count() != prerequisites.Count) return "a prerequisite is listed twice";

            foreach (var argument in requiredArguments)
            {
                if (string.IsNullOrEmpty(argument.Key)) return "a required argument has no name";
            }

            return null;
        }
    }
}
