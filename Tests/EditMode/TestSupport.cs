namespace TeaSpoons.GameTask.Tests
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A clock that only moves when the test says so.
    /// </summary>
    public sealed class ManualClock : IClock
    {
        public DateTime UtcNow { get; set; } = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        public void Advance(TimeSpan time)
        {
            UtcNow += time;
        }
    }

    public static class TestTasks
    {
        public static TaskDefinition Task(
            string id,
            string action = "act",
            long target = 1,
            ProgressMode mode = ProgressMode.Accumulate,
            string[] prerequisites = null,
            UnlockCondition unlockOn = UnlockCondition.PrerequisiteClaimed,
            long durationSeconds = 0,
            string rewardKey = null,
            params (string Key, string Value)[] requiredArguments)
        {
            var arguments = new List<KeyValuePair<string, string>>();
            foreach (var (key, value) in requiredArguments)
            {
                arguments.Add(new KeyValuePair<string, string>(key, value));
            }

            return new TaskDefinition(id, action, target, mode, prerequisites, unlockOn, arguments, durationSeconds, rewardKey: rewardKey);
        }

        public static Dictionary<string, string> Args(params (string Key, string Value)[] pairs)
        {
            var dictionary = new Dictionary<string, string>();
            foreach (var (key, value) in pairs)
            {
                dictionary[key] = value;
            }

            return dictionary;
        }
    }
}
