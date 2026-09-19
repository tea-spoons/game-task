namespace TeaSpoons.GameTask
{
    using System;
    using UnityEngine;

    /// <summary>
    /// A name and value a report has to carry for a task to count it: the <c>enemy</c> must be <c>goblin</c>.
    /// </summary>
    [Serializable]
    public struct TaskArgument
    {
        [SerializeField]
        private string key;

        [SerializeField]
        private string value;

        public TaskArgument(string key, string value)
        {
            this.key = key;
            this.value = value;
        }

        public string Key => key ?? string.Empty;

        public string Value => value ?? string.Empty;
    }
}
