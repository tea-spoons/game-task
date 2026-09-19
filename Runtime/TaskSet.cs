namespace TeaSpoons.GameTask
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// A list of task definitions as an asset, so tasks can be authored in the Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "TeaSpoons/Game Task/Task Set", fileName = "Task Set")]
    public class TaskSet : ScriptableObject
    {
        [SerializeField]
        private List<TaskDefinition> tasks = new();

        public IReadOnlyList<TaskDefinition> Tasks => tasks;

        /// <summary>
        /// Replaces the definitions. Useful when a set is built in code.
        /// </summary>
        public void SetTasks(IEnumerable<TaskDefinition> newTasks)
        {
            tasks = new List<TaskDefinition>(newTasks);
        }

        /// <summary>
        /// A tracker for the tasks of the set.
        /// </summary>
        /// <exception cref="System.ArgumentException">A definition is invalid, see <see cref="TaskTracker"/>.</exception>
        public TaskTracker CreateTracker(IClock clock = null)
        {
            return new TaskTracker(tasks, clock);
        }
    }
}
