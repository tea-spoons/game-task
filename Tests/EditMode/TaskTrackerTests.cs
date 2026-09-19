namespace TeaSpoons.GameTask.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using static TestTasks;

    public class TaskTrackerTests
    {
        private ManualClock clock;
        private List<string> events;

        [SetUp]
        public void SetUp()
        {
            clock = new ManualClock();
            events = new List<string>();
        }

        private TaskTracker Tracker(params TaskDefinition[] definitions)
        {
            var tracker = new TaskTracker(definitions, clock);
            tracker.TaskActivated += task => events.Add("activated " + task.Id);
            tracker.ProgressChanged += task => events.Add("progress " + task.Id + " " + task.Progress);
            tracker.TaskCompleted += task => events.Add("completed " + task.Id);
            tracker.TaskClaimed += task => events.Add("claimed " + task.Id);
            tracker.TaskExpired += task => events.Add("expired " + task.Id);
            return tracker;
        }

        // ---- progress

        [Test]
        public void TasksWithoutPrerequisitesStartActiveAndTheOthersLocked()
        {
            var tracker = Tracker(Task("a"), Task("b", prerequisites: new[] { "a" }));

            Assert.AreEqual(TaskState.Active, tracker.Get("a").State);
            Assert.AreEqual(TaskState.Locked, tracker.Get("b").State);
            Assert.AreEqual(clock.UtcNow, tracker.Get("a").ActivatedAtUtc);
            Assert.IsNull(tracker.Get("b").ActivatedAtUtc);
        }

        [Test]
        public void ReportsAddUpToTheTarget()
        {
            var tracker = Tracker(Task("kill", "kill-enemy", 10));

            tracker.Report("kill-enemy", 3);
            tracker.Report("kill-enemy");
            tracker.Report("kill-enemy", 2);

            var task = tracker.Get("kill");
            Assert.AreEqual(6, task.Progress);
            Assert.AreEqual(0.6, task.Fraction, 1e-9);
            Assert.AreEqual(TaskState.Active, task.State);
        }

        [Test]
        public void ReachingTheTargetCompletesTheTask()
        {
            var tracker = Tracker(Task("kill", "kill-enemy", 3));

            tracker.Report("kill-enemy", 3);

            Assert.AreEqual(TaskState.Completed, tracker.Get("kill").State);
            CollectionAssert.AreEqual(new[] { "progress kill 3", "completed kill" }, events);
        }

        [Test]
        public void ProgressStopsAtTheTargetAndACompletedTaskIgnoresReports()
        {
            var tracker = Tracker(Task("kill", "kill-enemy", 3));

            tracker.Report("kill-enemy", 100);
            tracker.Report("kill-enemy", 100);

            Assert.AreEqual(3, tracker.Get("kill").Progress);
            Assert.AreEqual(2, events.Count, "Only the first report changed anything.");
        }

        [Test]
        public void AHugeAmountDoesNotOverflow()
        {
            var tracker = Tracker(Task("a", target: long.MaxValue - 1));

            tracker.Report("act", long.MaxValue);
            tracker.Report("act", long.MaxValue);

            Assert.AreEqual(TaskState.Completed, tracker.Get("a").State);
            Assert.AreEqual(long.MaxValue - 1, tracker.Get("a").Progress);
        }

        [Test]
        public void OtherActionsAreIgnored()
        {
            var tracker = Tracker(Task("kill", "kill-enemy", 3));

            tracker.Report("win-match", 5);

            Assert.AreEqual(0, tracker.Get("kill").Progress);
            Assert.IsEmpty(events);
        }

        [Test]
        public void OneReportCountsForEveryMatchingTask()
        {
            var tracker = Tracker(Task("a", "kill", 5), Task("b", "kill", 2));

            tracker.Report("kill", 2);

            Assert.AreEqual(2, tracker.Get("a").Progress);
            Assert.AreEqual(TaskState.Completed, tracker.Get("b").State);
        }

        [Test]
        public void HighestModeKeepsTheHighestValue()
        {
            var tracker = Tracker(Task("level", "reach-level", 10, ProgressMode.Highest));

            tracker.Report("reach-level", 4);
            tracker.Report("reach-level", 2);
            Assert.AreEqual(4, tracker.Get("level").Progress);

            tracker.Report("reach-level", 12);
            Assert.AreEqual(TaskState.Completed, tracker.Get("level").State);
            Assert.AreEqual(10, tracker.Get("level").Progress);
        }

        [Test]
        public void ARequiredArgumentMustBeCarriedByTheReport()
        {
            var tracker = Tracker(Task("goblins", "kill", 2, requiredArguments: new[] { ("enemy", "goblin") }));

            tracker.Report("kill", 1);
            tracker.Report("kill", 1, Args(("enemy", "orc")));
            Assert.AreEqual(0, tracker.Get("goblins").Progress);

            tracker.Report("kill", 1, Args(("enemy", "goblin"), ("weapon", "bow")));
            Assert.AreEqual(1, tracker.Get("goblins").Progress);
        }

        [Test]
        public void ALockedTaskDoesNotCount()
        {
            var tracker = Tracker(Task("a", "x", 1), Task("b", "kill", 5, prerequisites: new[] { "a" }));

            tracker.Report("kill", 3);

            Assert.AreEqual(0, tracker.Get("b").Progress);
        }

        [Test]
        public void ZeroCountsForNothingAndNegativeAmountsAreRefused()
        {
            var tracker = Tracker(Task("a"));

            tracker.Report("act", 0);
            Assert.AreEqual(0, tracker.Get("a").Progress);

            Assert.Throws<ArgumentOutOfRangeException>(() => tracker.Report("act", -1));
            Assert.Throws<ArgumentException>(() => tracker.Report("", 1));
            Assert.Throws<ArgumentException>(() => tracker.Report(null, 1));
        }

        // ---- prerequisites

        [Test]
        public void ByDefaultAPrerequisiteHasToBeClaimed()
        {
            var tracker = Tracker(Task("a"), Task("b", prerequisites: new[] { "a" }));

            tracker.Report("act");
            Assert.AreEqual(TaskState.Locked, tracker.Get("b").State, "Completed is not enough by default.");

            Assert.IsTrue(tracker.TryClaim("a"));
            Assert.AreEqual(TaskState.Active, tracker.Get("b").State);
            CollectionAssert.AreEqual(new[] { "progress a 1", "completed a", "claimed a", "activated b" }, events);
        }

        [Test]
        public void ACompletedPrerequisiteCanBeEnough()
        {
            var tracker = Tracker(Task("a"), Task("b", "other", prerequisites: new[] { "a" }, unlockOn: UnlockCondition.PrerequisiteCompleted));

            tracker.Report("act");

            Assert.AreEqual(TaskState.Active, tracker.Get("b").State);
            Assert.AreEqual(clock.UtcNow, tracker.Get("b").ActivatedAtUtc);
        }

        [Test]
        public void EveryPrerequisiteHasToBeDone()
        {
            var tracker = Tracker(
                Task("a", "one"),
                Task("b", "two"),
                Task("c", "three", prerequisites: new[] { "a", "b" }, unlockOn: UnlockCondition.PrerequisiteCompleted));

            tracker.Report("one");
            Assert.AreEqual(TaskState.Locked, tracker.Get("c").State);

            tracker.Report("two");
            Assert.AreEqual(TaskState.Active, tracker.Get("c").State);
        }

        [Test]
        public void ATaskUnlockedDuringAReportDoesNotCountThatReport()
        {
            var tracker = Tracker(
                Task("first", "kill"),
                Task("second", "kill", 1, prerequisites: new[] { "first" }, unlockOn: UnlockCondition.PrerequisiteCompleted));

            tracker.Report("kill");

            Assert.AreEqual(TaskState.Completed, tracker.Get("first").State);
            Assert.AreEqual(TaskState.Active, tracker.Get("second").State);
            Assert.AreEqual(0, tracker.Get("second").Progress);
        }

        [Test]
        public void AChainOfTasksCanBeWorkedThrough()
        {
            var tracker = Tracker(
                Task("one", "act"),
                Task("two", "act", prerequisites: new[] { "one" }),
                Task("three", "act", prerequisites: new[] { "two" }));

            foreach (var id in new[] { "one", "two", "three" })
            {
                Assert.AreEqual(TaskState.Active, tracker.Get(id).State);
                tracker.Report("act");
                Assert.IsTrue(tracker.TryClaim(id));
            }

            Assert.IsTrue(tracker.Tasks.All(task => task.State == TaskState.Claimed));
        }

        // ---- claiming

        [Test]
        public void OnlyACompletedTaskCanBeClaimedAndOnlyOnce()
        {
            var tracker = Tracker(Task("a", target: 2));

            Assert.IsFalse(tracker.TryClaim("a"), "Active");
            tracker.Report("act", 2);
            Assert.IsTrue(tracker.TryClaim("a"));
            Assert.IsFalse(tracker.TryClaim("a"), "Already claimed");
            Assert.IsFalse(tracker.TryClaim("missing"));
            Assert.IsFalse(tracker.TryClaim(null));
            Assert.AreEqual(TaskState.Claimed, tracker.Get("a").State);
        }

        // ---- expiry

        [Test]
        public void AnActiveTaskExpiresWhenItsTimeIsUp()
        {
            var tracker = Tracker(Task("a", target: 5, durationSeconds: 60));

            clock.Advance(TimeSpan.FromSeconds(59));
            tracker.Tick();
            Assert.AreEqual(TaskState.Active, tracker.Get("a").State);
            Assert.AreEqual(clock.UtcNow.AddSeconds(1), tracker.Get("a").ExpiresAtUtc);

            clock.Advance(TimeSpan.FromSeconds(1));
            tracker.Tick();

            Assert.AreEqual(TaskState.Expired, tracker.Get("a").State);
            Assert.IsNull(tracker.Get("a").ExpiresAtUtc);
            CollectionAssert.AreEqual(new[] { "expired a" }, events);
        }

        [Test]
        public void ReportingChecksExpiryToo()
        {
            var tracker = Tracker(Task("a", target: 5, durationSeconds: 60));
            tracker.Report("act", 2);

            clock.Advance(TimeSpan.FromMinutes(5));
            tracker.Report("act", 2);

            Assert.AreEqual(TaskState.Expired, tracker.Get("a").State);
            Assert.AreEqual(2, tracker.Get("a").Progress, "Progress after the deadline does not count.");
        }

        [Test]
        public void ACompletedTaskDoesNotExpireAndCanStillBeClaimed()
        {
            var tracker = Tracker(Task("a", durationSeconds: 60));
            tracker.Report("act");

            clock.Advance(TimeSpan.FromHours(1));

            Assert.IsTrue(tracker.TryClaim("a"));
        }

        [Test]
        public void ALockedTaskDoesNotExpireAndItsTimeStartsWhenItUnlocks()
        {
            var tracker = Tracker(Task("a"), Task("b", "other", prerequisites: new[] { "a" }, durationSeconds: 60));

            clock.Advance(TimeSpan.FromHours(1));
            tracker.Tick();
            Assert.AreEqual(TaskState.Locked, tracker.Get("b").State);

            tracker.Report("act");
            tracker.TryClaim("a");
            Assert.AreEqual(TaskState.Active, tracker.Get("b").State);

            clock.Advance(TimeSpan.FromSeconds(30));
            tracker.Tick();
            Assert.AreEqual(TaskState.Active, tracker.Get("b").State);
        }

        [Test]
        public void TasksWithoutADurationNeverExpire()
        {
            var tracker = Tracker(Task("a"));

            clock.Advance(TimeSpan.FromDays(3650));
            tracker.Tick();

            Assert.AreEqual(TaskState.Active, tracker.Get("a").State);
            Assert.IsNull(tracker.Get("a").ExpiresAtUtc);
        }

        // ---- setup errors

        [Test]
        public void DefinitionsAreChecked()
        {
            Assert.Throws<ArgumentException>(() => Tracker(Task("a"), Task("a")), "duplicate id");
            Assert.Throws<ArgumentException>(() => Tracker(Task("a", prerequisites: new[] { "missing" })), "unknown prerequisite");
            Assert.Throws<ArgumentException>(() => Tracker(Task("a", target: 0)), "invalid definition");
            Assert.Throws<ArgumentException>(() => new TaskTracker(new TaskDefinition[] { null }), "null definition");
            Assert.Throws<ArgumentNullException>(() => new TaskTracker(null));
        }

        [Test]
        public void PrerequisiteCyclesAreRefused()
        {
            var ex = Assert.Throws<ArgumentException>(() => Tracker(
                Task("a", prerequisites: new[] { "c" }),
                Task("b", prerequisites: new[] { "a" }),
                Task("c", prerequisites: new[] { "b" })));

            StringAssert.Contains("lead back to itself", ex.Message);
        }

        [Test]
        public void ADiamondOfPrerequisitesIsNotACycle()
        {
            Assert.DoesNotThrow(() => Tracker(
                Task("root"),
                Task("left", prerequisites: new[] { "root" }),
                Task("right", prerequisites: new[] { "root" }),
                Task("end", prerequisites: new[] { "left", "right" })));
        }

        [Test]
        public void GetThrowsForAnUnknownTask()
        {
            var tracker = Tracker(Task("a"));

            Assert.Throws<KeyNotFoundException>(() => tracker.Get("missing"));
            Assert.IsFalse(tracker.TryGet("missing", out _));
            Assert.IsTrue(tracker.TryGet("a", out var task));
            Assert.AreEqual("a", task.Id);
        }

        [Test]
        public void TasksKeepTheOrderOfTheDefinitions()
        {
            var tracker = Tracker(Task("z"), Task("a"), Task("m"));

            CollectionAssert.AreEqual(new[] { "z", "a", "m" }, tracker.Tasks.Select(task => task.Id));
        }

        [Test]
        public void AHandlerThatReportsAgainDoesNotBreakTheTracker()
        {
            var tracker = Tracker(Task("a", "one"), Task("b", "two"));
            tracker.TaskCompleted += task =>
            {
                if (task.Id == "a")
                {
                    tracker.Report("two");
                }
            };

            tracker.Report("one");

            Assert.AreEqual(TaskState.Completed, tracker.Get("a").State);
            Assert.AreEqual(TaskState.Completed, tracker.Get("b").State);
        }
    }
}
