namespace TeaSpoons.GameTask.Tests
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using TeaSpoons.Localizer;
    using static TestTasks;

    public class TaskLocalizationTests
    {
        private Translator translator;

        [SetUp]
        public void SetUp()
        {
            translator = new Translator("en");
            translator.AddTable("en", new Dictionary<string, string>
            {
                ["task.kill.title"] = "Goblin hunter",
                ["task.kill.description"] = "Defeat {enemy}s: {progress}/{target} ({remaining} to go)",
                ["task.collect.description.one"] = "Collect {count} coin",
                ["task.collect.description.other"] = "Collect {count} coins ({progress} so far)",
                ["custom.title"] = "Custom title",
            });
            translator.AddTable("de", new Dictionary<string, string>
            {
                ["task.kill.title"] = "Kobold-Jäger",
            });
        }

        [Test]
        public void TheTitleComesFromTheDefaultKey()
        {
            Assert.AreEqual("Goblin hunter", Task("kill").GetTitle(translator));
        }

        [Test]
        public void TheTitleFollowsTheLanguage()
        {
            translator.SetLanguage("de");

            Assert.AreEqual("Kobold-Jäger", Task("kill").GetTitle(translator));
        }

        [Test]
        public void ATitleKeyCanBeChosen()
        {
            var definition = new TaskDefinition("kill", "act", titleKey: "custom.title");

            Assert.AreEqual("Custom title", definition.GetTitle(translator));
        }

        [Test]
        public void AMissingTitleShowsTheKey()
        {
            Assert.AreEqual("[task.nope.title]", Task("nope").GetTitle(translator));
        }

        [Test]
        public void TheDescriptionShowsProgressTargetAndRequiredArguments()
        {
            var tracker = new TaskTracker(new[] { Task("kill", "kill", 10, requiredArguments: new[] { ("enemy", "goblin") }) }, new ManualClock());
            tracker.Report("kill", 4, Args(("enemy", "goblin")));

            Assert.AreEqual("Defeat goblins: 4/10 (6 to go)", tracker.Get("kill").GetDescription(translator));
        }

        private string DescribeCollect(long target)
        {
            var tracker = new TaskTracker(new[] { new TaskDefinition("collect", "coin", target) }, new ManualClock());
            return tracker.Get("collect").GetDescription(translator);
        }

        [Test]
        public void PluralFormsOfTheDescriptionAreChosenByTheTarget()
        {
            Assert.AreEqual("Collect 1 coin", DescribeCollect(1));
            Assert.AreEqual("Collect 5 coins (0 so far)", DescribeCollect(5));
        }

        [Test]
        public void UsesTheDefaultTranslatorWhenNoneIsGiven()
        {
            var original = Translator.Default;
            try
            {
                Translator.Default = translator;

                Assert.AreEqual("Goblin hunter", Task("kill").GetTitle());
            }
            finally
            {
                Translator.Default = original;
            }
        }
    }
}
