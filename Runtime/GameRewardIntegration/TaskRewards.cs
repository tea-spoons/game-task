namespace TeaSpoons.GameTask
{
    using System;
    using System.Collections.Generic;
    using TeaSpoons.GameReward;

    /// <summary>
    /// Claims tasks and grants their rewards in one step. Only compiled when the Game Reward package is in the project.
    /// </summary>
    /// <remarks>
    /// A task names its reward with <see cref="TaskDefinition.RewardKey"/>. The lookup you give turns that name into a
    /// <see cref="Reward"/>, for example <see cref="LookupByName"/> for <see cref="RewardDefinition"/> assets.
    /// </remarks>
    public sealed class TaskRewards
    {
        private readonly TaskTracker tracker;
        private readonly RewardService rewards;
        private readonly Func<string, Reward> lookup;

        /// <param name="lookup">Gives the reward for a reward key, or <c>null</c> if there is none.</param>
        public TaskRewards(TaskTracker tracker, RewardService rewards, Func<string, Reward> lookup)
        {
            this.tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
            this.rewards = rewards ?? throw new ArgumentNullException(nameof(rewards));
            this.lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
        }

        /// <summary>
        /// A lookup for <see cref="TaskRewards"/> that finds reward assets by their name.
        /// </summary>
        public static Func<string, Reward> LookupByName(IEnumerable<RewardDefinition> definitions)
        {
            var byName = new Dictionary<string, RewardDefinition>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                byName[definition.name] = definition;
            }

            return key => byName.TryGetValue(key, out var definition) ? definition.ToReward() : null;
        }

        /// <summary>
        /// Grants the reward of a completed task and claims the task.
        /// </summary>
        /// <param name="result">What the reward service did. <c>null</c> when the task has no reward, or is not completed.</param>
        /// <returns>
        /// <c>true</c> if the task was claimed. <c>false</c> if it is not completed, or the reward could not be granted
        /// (see <paramref name="result"/>), in which case the task stays completed and can be claimed again.
        /// </returns>
        /// <exception cref="InvalidOperationException">The lookup does not know the reward key of the task.</exception>
        public bool TryClaim(string taskId, out RewardResult result)
        {
            result = null;

            if (!tracker.TryGet(taskId, out var task) || task.State != TaskState.Completed)
            {
                return false;
            }

            if (task.Definition.HasReward)
            {
                var reward = lookup(task.Definition.RewardKey)
                             ?? throw new InvalidOperationException(
                                 $"Task '{taskId}' has the reward '{task.Definition.RewardKey}', which the lookup does not know.");

                if (!rewards.TryGrant(reward, out result))
                {
                    return false;
                }
            }

            return tracker.TryClaim(taskId);
        }
    }
}
