# Game Task

Tasks, quests, missions and achievements as data. You tell the tracker what the player does ("killed 3 enemies"), and it
counts that for the tasks that ask for it: progress, prerequisites, time limits, claiming, and save and restore.

## What's in it

- **`TaskDefinition`** describes a task: an id, the **action key** whose reports count, a target, optional prerequisites,
  required arguments, a duration and keys for its texts. Author them in code or as a **`TaskSet`** asset
  (**Assets > Create > TeaSpoons > Game Task > Task Set**).
- **`TaskTracker`** keeps the state of a set of tasks. `Report(action, amount, arguments)` is the only thing your game
  has to call.
- **States**: `Locked` (waiting for prerequisites), `Active`, `Completed` (target reached), `Claimed`, `Expired`.
- **Events**: `TaskActivated`, `ProgressChanged`, `TaskCompleted`, `TaskClaimed`, `TaskExpired`.
- **Save and restore**: `tracker.Save().ToJson()` and `tracker.Restore(TaskTrackerSnapshot.FromJson(json))`.

## Example

```csharp
using System.Collections.Generic;
using TeaSpoons.GameTask;

var tracker = new TaskTracker(new[]
{
    new TaskDefinition("kill-goblins", "kill", target: 10,
        requiredArguments: new[] { new KeyValuePair<string, string>("enemy", "goblin") }),

    new TaskDefinition("reach-level", "level", target: 5, progressMode: ProgressMode.Highest),

    // Unlocks once "kill-goblins" is claimed, and has to be finished within an hour of unlocking.
    new TaskDefinition("kill-the-chief", "kill", target: 1,
        prerequisites: new[] { "kill-goblins" }, durationSeconds: 3600),
});

tracker.TaskCompleted += task => ShowToast(task.Id);

// Anywhere in your game:
tracker.Report("kill", 1, new Dictionary<string, string> { ["enemy"] = "goblin" });
tracker.Report("level", player.Level);           // "Highest" tasks keep the highest value reported

// When the player taps the reward button:
if (tracker.TryClaim("kill-goblins")) { /* give the reward */ }

// Now and then, if tasks have durations:
tracker.Tick();
```

### How reports are counted

- A report counts for every **active** task with the same action key whose required arguments it carries.
- `Accumulate` tasks add the amounts (progress stops at the target); `Highest` tasks keep the highest amount.
- Locked, completed, claimed and expired tasks ignore reports.
- A task that a completion unlocks during a report does not count that same report.

### Prerequisites and time

- Tasks without prerequisites start active. A task with prerequisites unlocks when all of them are **claimed**
  (the default) or, with `UnlockCondition.PrerequisiteCompleted`, completed. Prerequisites that do not exist, or that
  lead back to themselves, are refused when the tracker is created.
- A task with a `durationSeconds` runs from the moment it becomes active. Expiry is checked by `Tick`, `Report`
  and `TryClaim`. Completed tasks never expire, so a reward can wait to be claimed. Pass your own `IClock` to use server
  time or to test.

### Saving

```csharp
string json = tracker.Save().ToJson();
// ... later, with the same definitions ...
tracker.Restore(TaskTrackerSnapshot.FromJson(json));
```

Restoring raises no events. Tasks that were added since the save keep their starting state (and unlock if the restored tasks
allow it); saved tasks that no longer exist are ignored.

## Requirements

- Unity 6000.3 (developed and tested there; older versions were not tested)
- No dependencies.

## Installation

In Unity: **Window > Package Manager > + > Add package from git URL**, then enter:

```
https://github.com/tea-spoons/game-task.git
```

Pin a release by appending a tag, for example `#v0.1.0`.

## Works with

These packages are optional. When your project has them, they get extra features (Unity detects them automatically):

| Package | Adds |
|---|---|
| [Game Reward](https://github.com/tea-spoons/game-reward) (0.1.0+) | `TaskRewards`: a task names its reward with `rewardKey`, and `TryClaim(taskId, out result)` grants that reward through your `RewardService` and claims the task. If the reward cannot be granted the task stays completed, so the player can claim again. |
| [Localizer](https://github.com/tea-spoons/localizer) (0.1.0+) | `GetTitle` and `GetDescription`: texts under `task.{id}.title` and `task.{id}.description`, with `{progress}`, `{target}`, `{remaining}` and the task's required arguments filled in. Plural forms (`...description.one`, `...description.other`) are chosen by the target. |

```csharp
var taskRewards = new TaskRewards(tracker, rewardService, TaskRewards.LookupByName(rewardAssets));
if (taskRewards.TryClaim("kill-goblins", out var result)) { /* claimed and rewarded */ }

titleLabel.text = task.Definition.GetTitle();
descriptionLabel.text = task.GetDescription();
```

## Change plan

See [CHANGE-PLAN.md](CHANGE-PLAN.md) for what is planned next.

## License

[MIT](LICENSE.md), © Muhammad Tarek Abdou.
