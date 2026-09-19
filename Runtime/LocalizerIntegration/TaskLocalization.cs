namespace TeaSpoons.GameTask
{
    using System.Collections.Generic;
    using TeaSpoons.Localizer;

    /// <summary>
    /// Titles and descriptions of tasks in the current language. Only compiled when the Localizer package is in the project.
    /// </summary>
    public static class TaskLocalization
    {
        /// <summary>
        /// The title of the task, from the text under <see cref="TaskDefinition.TitleKey"/> (<c>task.{id}.title</c> by default).
        /// </summary>
        public static string GetTitle(this TaskDefinition definition, Translator translator = null)
        {
            return (translator ?? Translator.Default).Get(definition.TitleKey);
        }

        /// <summary>
        /// The description of the task, from the text under <see cref="TaskDefinition.DescriptionKey"/>
        /// (<c>task.{id}.description</c> by default).
        /// </summary>
        /// <remarks>
        /// The placeholders <c>{progress}</c>, <c>{target}</c> and <c>{remaining}</c> are filled in, and so is one
        /// for every required argument of the task, by its name. If the language has plural forms of the description
        /// (<c>...description.one</c>, <c>...description.other</c>) instead of one text, they are chosen by the target.
        /// </remarks>
        public static string GetDescription(this TrackedTask task, Translator translator = null)
        {
            translator ??= Translator.Default;
            var key = task.Definition.DescriptionKey;

            var arguments = new Dictionary<string, object>();
            foreach (var argument in task.Definition.RequiredArguments)
            {
                arguments[argument.Key] = argument.Value;
            }

            arguments["progress"] = task.Progress;
            arguments["target"] = task.Target;
            arguments["remaining"] = task.Target - task.Progress;

            return translator.HasKey(key)
                ? translator.Get(key, arguments)
                : translator.GetPlural(key, task.Target, arguments);
        }
    }
}
