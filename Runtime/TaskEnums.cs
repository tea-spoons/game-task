namespace TeaSpoons.GameTask
{
    /// <summary>
    /// Where a task is in its life.
    /// </summary>
    public enum TaskState
    {
        /// <summary>Waiting for its prerequisites. Progress is not counted yet.</summary>
        Locked,

        /// <summary>Counting progress.</summary>
        Active,

        /// <summary>The target was reached and the reward can be claimed.</summary>
        Completed,

        /// <summary>The reward was claimed. The end of the task.</summary>
        Claimed,

        /// <summary>The time ran out before the target was reached. The end of the task.</summary>
        Expired,
    }

    /// <summary>
    /// How a report adds to the progress of a task.
    /// </summary>
    public enum ProgressMode
    {
        /// <summary>The amounts are added up: "kill 10 enemies".</summary>
        Accumulate,

        /// <summary>The highest amount reported counts: "reach level 10".</summary>
        Highest,
    }

    /// <summary>
    /// What has to happen to a prerequisite before the task it unlocks becomes active.
    /// </summary>
    public enum UnlockCondition
    {
        /// <summary>The prerequisite reached its target.</summary>
        PrerequisiteCompleted,

        /// <summary>The prerequisite was claimed.</summary>
        PrerequisiteClaimed,
    }
}
