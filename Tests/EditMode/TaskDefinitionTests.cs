namespace TeaSpoons.GameTask.Tests
{
    using System;
    using NUnit.Framework;
    using static TestTasks;

    public class TaskDefinitionTests
    {
        [Test]
        public void AValidDefinitionHasNoProblem()
        {
            Assert.IsNull(Task("kill", "kill-enemy", 10).Validate());
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void ItNeedsAnId(string id)
        {
            StringAssert.Contains("id", new TaskDefinition(id, "act").Validate());
        }

        [Test]
        public void ItNeedsAnActionKey()
        {
            StringAssert.Contains("action", new TaskDefinition("a", "").Validate());
        }

        [Test]
        public void TheTargetMustBeAtLeastOne()
        {
            StringAssert.Contains("target", new TaskDefinition("a", "act", 0).Validate());
            StringAssert.Contains("target", new TaskDefinition("a", "act", -3).Validate());
        }

        [Test]
        public void TheDurationCannotBeNegative()
        {
            StringAssert.Contains("duration", new TaskDefinition("a", "act", durationSeconds: -1).Validate());
        }

        [Test]
        public void PrerequisitesAreChecked()
        {
            StringAssert.Contains("itself", new TaskDefinition("a", "act", prerequisites: new[] { "a" }).Validate());
            StringAssert.Contains("twice", new TaskDefinition("a", "act", prerequisites: new[] { "b", "b" }).Validate());
            StringAssert.Contains("empty", new TaskDefinition("a", "act", prerequisites: new[] { "" }).Validate());
        }

        [Test]
        public void ARequiredArgumentNeedsAName()
        {
            StringAssert.Contains("argument", Task("a", requiredArguments: new[] { ("", "x") }).Validate());
        }

        [Test]
        public void KeysDefaultToTheId()
        {
            var definition = new TaskDefinition("kill", "act");

            Assert.AreEqual("task.kill.title", definition.TitleKey);
            Assert.AreEqual("task.kill.description", definition.DescriptionKey);
        }

        [Test]
        public void KeysCanBeChosen()
        {
            var definition = new TaskDefinition("kill", "act", titleKey: "my.title", descriptionKey: "my.description", rewardKey: "big-reward");

            Assert.AreEqual("my.title", definition.TitleKey);
            Assert.AreEqual("my.description", definition.DescriptionKey);
            Assert.AreEqual("big-reward", definition.RewardKey);
            Assert.IsTrue(definition.HasReward);
            Assert.IsFalse(new TaskDefinition("a", "act").HasReward);
        }

        [Test]
        public void TheDurationIsATimeSpanOrNothing()
        {
            Assert.AreEqual(TimeSpan.FromMinutes(2), new TaskDefinition("a", "act", durationSeconds: 120).Duration);
            Assert.IsNull(new TaskDefinition("a", "act").Duration);
        }

        [Test]
        public void MatchesNeedsEveryRequiredArgument()
        {
            var definition = Task("a", requiredArguments: new[] { ("enemy", "goblin"), ("weapon", "bow") });

            Assert.IsTrue(definition.Matches(Args(("enemy", "goblin"), ("weapon", "bow"), ("extra", "ignored"))));
            Assert.IsFalse(definition.Matches(Args(("enemy", "goblin"))));
            Assert.IsFalse(definition.Matches(Args(("enemy", "orc"), ("weapon", "bow"))));
            Assert.IsFalse(definition.Matches(null));
        }

        [Test]
        public void ADefinitionWithoutRequiredArgumentsMatchesEverything()
        {
            var definition = Task("a");

            Assert.IsTrue(definition.Matches(null));
            Assert.IsTrue(definition.Matches(Args(("anything", "goes"))));
        }

        [Test]
        public void ArgumentValuesAreCaseSensitive()
        {
            Assert.IsFalse(Task("a", requiredArguments: new[] { ("enemy", "goblin") }).Matches(Args(("enemy", "Goblin"))));
        }
    }
}
