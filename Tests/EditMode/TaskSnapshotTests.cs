namespace TeaSpoons.GameTask.Tests
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using static TestTasks;

    public class TaskSnapshotTests
    {
        private ManualClock clock;

        [SetUp]
        public void SetUp()
        {
            clock = new ManualClock();
        }

        private TaskTracker Tracker(params TaskDefinition[] definitions) => new TaskTracker(definitions, clock);

        [Test]
        public void ProgressSurvivesSavingAndRestoring()
        {
            var original = Tracker(
                Task("a", "kill", 10),
                Task("b", "win", 1),
                Task("c", "other", prerequisites: new[] { "b" }));
            original.Report("kill", 4);
            original.Report("win");
            original.TryClaim("b");

            var json = original.Save().ToJson();

            var restored = Tracker(
                Task("a", "kill", 10),
                Task("b", "win", 1),
                Task("c", "other", prerequisites: new[] { "b" }));
            restored.Restore(TaskTrackerSnapshot.FromJson(json));

            Assert.AreEqual(TaskState.Active, restored.Get("a").State);
            Assert.AreEqual(4, restored.Get("a").Progress);
            Assert.AreEqual(TaskState.Claimed, restored.Get("b").State);
            Assert.AreEqual(TaskState.Active, restored.Get("c").State);
        }

        [Test]
        public void TheActivationTimeIsKeptSoExpiryContinues()
        {
            var original = Tracker(Task("a", durationSeconds: 100));
            clock.Advance(TimeSpan.FromSeconds(70));

            var restored = Tracker(Task("a", durationSeconds: 100));
            restored.Restore(original.Save());

            clock.Advance(TimeSpan.FromSeconds(31));
            restored.Tick();

            Assert.AreEqual(TaskState.Expired, restored.Get("a").State);
        }

        [Test]
        public void RestoringRaisesNoEvents()
        {
            var original = Tracker(Task("a", target: 2));
            original.Report("act", 2);

            var restored = Tracker(Task("a", target: 2));
            var raised = 0;
            restored.TaskCompleted += _ => raised++;
            restored.ProgressChanged += _ => raised++;
            restored.TaskActivated += _ => raised++;

            restored.Restore(original.Save());

            Assert.AreEqual(0, raised);
            Assert.AreEqual(TaskState.Completed, restored.Get("a").State);
        }

        [Test]
        public void TasksThatAreNewSinceTheSaveKeepTheirStateOrUnlockSilently()
        {
            var old = Tracker(Task("a"));
            old.Report("act");
            old.TryClaim("a");
            var saved = old.Save();

            var updated = Tracker(
                Task("a"),
                Task("b", "next", prerequisites: new[] { "a" }),
                Task("c", "fresh"));
            var activated = 0;
            updated.TaskActivated += _ => activated++;

            updated.Restore(saved);

            Assert.AreEqual(TaskState.Active, updated.Get("b").State, "Unlocked by the restored 'a'.");
            Assert.AreEqual(TaskState.Active, updated.Get("c").State);
            Assert.AreEqual(0, activated);
        }

        [Test]
        public void EntriesForTasksThatNoLongerExistAreIgnored()
        {
            var old = Tracker(Task("gone"), Task("a"));
            old.Report("act");

            var updated = Tracker(Task("a"));

            Assert.DoesNotThrow(() => updated.Restore(old.Save()));
            Assert.AreEqual(TaskState.Completed, updated.Get("a").State);
        }

        [Test]
        public void ProgressIsKeptWithinTheTarget()
        {
            var json = "{\"tasks\":[{\"id\":\"a\",\"state\":\"Active\",\"progress\":9999,\"activatedAtTicks\":0}," +
                       "{\"id\":\"b\",\"state\":\"Active\",\"progress\":-5,\"activatedAtTicks\":0}]}";
            var tracker = Tracker(Task("a", "x", 10), Task("b", "y", 10));

            tracker.Restore(TaskTrackerSnapshot.FromJson(json));

            Assert.AreEqual(10, tracker.Get("a").Progress);
            Assert.AreEqual(0, tracker.Get("b").Progress);
        }

        [Test]
        public void ACompletedTaskAlwaysHasFullProgress()
        {
            var json = "{\"tasks\":[{\"id\":\"a\",\"state\":\"Completed\",\"progress\":1,\"activatedAtTicks\":0}]}";
            var tracker = Tracker(Task("a", "x", 10));

            tracker.Restore(TaskTrackerSnapshot.FromJson(json));

            Assert.AreEqual(10, tracker.Get("a").Progress);
        }

        [Test]
        public void AStateThisVersionDoesNotKnowIsIgnored()
        {
            var json = "{\"tasks\":[{\"id\":\"a\",\"state\":\"Vanished\",\"progress\":3,\"activatedAtTicks\":0}," +
                       "{\"id\":\"b\",\"state\":\"42\",\"progress\":3,\"activatedAtTicks\":0}]}";
            var tracker = Tracker(Task("a", "x", 10), Task("b", "y", 10));

            tracker.Restore(TaskTrackerSnapshot.FromJson(json));

            Assert.AreEqual(TaskState.Active, tracker.Get("a").State);
            Assert.AreEqual(0, tracker.Get("a").Progress);
            Assert.AreEqual(TaskState.Active, tracker.Get("b").State);
        }

        [Test]
        public void TheJsonHasReadableStates()
        {
            var tracker = Tracker(Task("a", target: 2));
            tracker.Report("act");

            var json = tracker.Save().ToJson();

            StringAssert.Contains("\"id\":\"a\"", json);
            StringAssert.Contains("\"state\":\"Active\"", json);
            StringAssert.Contains("\"progress\":1", json);
        }

        [Test]
        public void JsonThatIsNotASnapshotIsRefused()
        {
            Assert.Throws<FormatException>(() => TaskTrackerSnapshot.FromJson("this is not json"));
            Assert.Throws<ArgumentNullException>(() => Tracker(Task("a")).Restore(null));
        }

        [Test]
        public void ATaskSetBuildsATrackerFromAsset()
        {
            var set = ScriptableObject.CreateInstance<TaskSet>();
            try
            {
                set.SetTasks(new[] { Task("a", "kill", 3), Task("b", "win") });

                var tracker = set.CreateTracker(clock);
                tracker.Report("kill", 3);

                Assert.AreEqual(2, set.Tasks.Count);
                Assert.AreEqual(TaskState.Completed, tracker.Get("a").State);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(set);
            }
        }

        [Test]
        public void ATaskSetStoresItsTasksInTheInspectorShape()
        {
            var set = ScriptableObject.CreateInstance<TaskSet>();
            try
            {
                set.SetTasks(new[] { Task("kill", "kill-enemy", 10, prerequisites: new[] { "intro" }, durationSeconds: 60, rewardKey: "big") });
                var serialized = new UnityEditor.SerializedObject(set);

                Assert.AreEqual("kill", serialized.FindProperty("tasks.Array.data[0].id").stringValue);
                Assert.AreEqual("kill-enemy", serialized.FindProperty("tasks.Array.data[0].actionKey").stringValue);
                Assert.AreEqual(10, serialized.FindProperty("tasks.Array.data[0].target").longValue);
                Assert.AreEqual("intro", serialized.FindProperty("tasks.Array.data[0].prerequisites.Array.data[0]").stringValue);
                Assert.AreEqual(60, serialized.FindProperty("tasks.Array.data[0].durationSeconds").longValue);
                Assert.AreEqual("big", serialized.FindProperty("tasks.Array.data[0].rewardKey").stringValue);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(set);
            }
        }
    }
}
