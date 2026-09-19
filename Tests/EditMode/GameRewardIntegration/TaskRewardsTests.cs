namespace TeaSpoons.GameTask.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using TeaSpoons.GameReward;
    using UnityEngine;
    using static TestTasks;

    public class TaskRewardsTests
    {
        private TaskTracker tracker;
        private RewardService service;
        private TaskRewards taskRewards;
        private long gold;

        [SetUp]
        public void SetUp()
        {
            tracker = new TaskTracker(new[]
            {
                Task("boss", "kill-boss", 1, rewardKey: "boss-loot"),
                Task("intro", "start"),
                Task("broken", "x", 1, rewardKey: "does-not-exist"),
            }, new ManualClock());

            gold = 0;
            service = new RewardService();
            service.Register("gold", item => gold += item.Amount);

            var rewards = new Dictionary<string, Reward>
            {
                ["boss-loot"] = new Reward(new RewardItem("gold", 500)),
            };
            taskRewards = new TaskRewards(tracker, service, key => rewards.TryGetValue(key, out var reward) ? reward : null);
        }

        [Test]
        public void ClaimingGrantsTheRewardAndClaimsTheTask()
        {
            tracker.Report("kill-boss");

            Assert.IsTrue(taskRewards.TryClaim("boss", out var result));

            Assert.AreEqual(500, gold);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(TaskState.Claimed, tracker.Get("boss").State);
        }

        [Test]
        public void ATaskWithoutARewardIsJustClaimed()
        {
            tracker.Report("start");

            Assert.IsTrue(taskRewards.TryClaim("intro", out var result));

            Assert.IsNull(result);
            Assert.AreEqual(TaskState.Claimed, tracker.Get("intro").State);
            Assert.AreEqual(0, gold);
        }

        [Test]
        public void ATaskThatIsNotCompletedCannotBeClaimed()
        {
            Assert.IsFalse(taskRewards.TryClaim("boss", out var result));

            Assert.IsNull(result);
            Assert.AreEqual(0, gold);
            Assert.AreEqual(TaskState.Active, tracker.Get("boss").State);
            Assert.IsFalse(taskRewards.TryClaim("missing", out _));
        }

        [Test]
        public void TheRewardIsGrantedOnlyOnce()
        {
            tracker.Report("kill-boss");

            Assert.IsTrue(taskRewards.TryClaim("boss", out _));
            Assert.IsFalse(taskRewards.TryClaim("boss", out _));

            Assert.AreEqual(500, gold);
        }

        [Test]
        public void ARewardThatCannotBeGrantedLeavesTheTaskToClaimLater()
        {
            var full = true;
            service.Unregister("gold");
            service.Register("gold", item => gold += item.Amount, canGrant: _ => !full);
            tracker.Report("kill-boss");

            Assert.IsFalse(taskRewards.TryClaim("boss", out var result));

            Assert.AreEqual(RewardFailureReason.Rejected, result.Failures.Single().Reason);
            Assert.AreEqual(TaskState.Completed, tracker.Get("boss").State);
            Assert.AreEqual(0, gold);

            full = false;
            Assert.IsTrue(taskRewards.TryClaim("boss", out _));
            Assert.AreEqual(500, gold);
        }

        [Test]
        public void ARewardKeyThatTheLookupDoesNotKnowIsAnError()
        {
            tracker.Report("x");

            Assert.Throws<InvalidOperationException>(() => taskRewards.TryClaim("broken", out _));

            Assert.AreEqual(TaskState.Completed, tracker.Get("broken").State);
        }

        [Test]
        public void RewardAssetsCanBeLookedUpByName()
        {
            var asset = ScriptableObject.CreateInstance<RewardDefinition>();
            try
            {
                asset.name = "boss-loot";
                asset.SetItems(new[] { new RewardItem("gold", 250) });
                var byName = new TaskRewards(tracker, service, TaskRewards.LookupByName(new[] { asset }));
                tracker.Report("kill-boss");

                Assert.IsTrue(byName.TryClaim("boss", out _));

                Assert.AreEqual(250, gold);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void TheConstructorNeedsEverything()
        {
            Assert.Throws<ArgumentNullException>(() => new TaskRewards(null, service, _ => null));
            Assert.Throws<ArgumentNullException>(() => new TaskRewards(tracker, null, _ => null));
            Assert.Throws<ArgumentNullException>(() => new TaskRewards(tracker, service, null));
        }
    }
}
