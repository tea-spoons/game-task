# Change plan

> Draft. This file tracks what I plan to change next. Edit freely.

## Origin

Written from scratch by me (Muhammad Tarek Abdou) in 2026 and released under the MIT license. It is not derived from
any employer's or other project's code.

## Planned changes

- [x] Tag and publish `v0.1.0` with the Release workflow.
- [ ] Repeating tasks (daily and weekly resets) with a reset time in the definition.
- [ ] Tasks with several objectives that all have to be met.
- [ ] A drawer or editor window to author `TaskSet` assets more comfortably, and to check prerequisites.
- [ ] Try older Unity versions than 6000.3 (only 6000.3 is tested).
<!-- review-items:start -->
- [ ] **P1** Add a `version` to `TaskTrackerSnapshot` and a migration hook, before the first users depend on the format.
- [ ] **P1** Add a `Failed` state with an event (for example a task that fails when a deadline passes with a condition, or on request).
- [ ] **P1** Run the tests in CI. The kit's `run-tests` needs a Unity project, so this waits for package-mode support in `unity-ci-kit` (planned there; GameCI's test runner has a `packageMode` for the same reason).
- [ ] **P2** Index tasks by action key so `Report` does not visit every task.
- [ ] **P2** Allow adding and removing task definitions at runtime (daily and generated tasks).
- [ ] **P2** Optional AND/OR composition of argument conditions (the multi-objective item above covers the rest).
<!-- review-items:end -->

<!-- review:start -->
## Review (September 2026)

Reviewed as a senior Unity engineer would: I read the code and compared the package with similar open-source projects (September 2026). Those projects are listed for ideas only. Nothing was copied from them, and their licenses are noted in case code is ever reused. Priorities: **P0** correctness bug or broken metadata, **P1** should be done soon, **P2** nice to have.

### Compared with

| Project | License | Worth noting |
|---|---|---|
| [mechaniqe/unity-quest-core](https://github.com/mechaniqe/unity-quest-core) | MIT | Quests and objectives as ScriptableObjects, conditions composed with AND/OR, prerequisites, optional objectives, a graph editor, snapshots for save/load, and events including `OnQuestFailed`. Unity 2021.3 LTS and later. |
| [lluispalerm/QuestSystem](https://github.com/lluispalerm/QuestSystem) | not checked | Quest system with saving and a custom graph editor. |

### Findings from reading the code

- **[Gap]** A task has one objective (`Target`) and one action key. Quest Core has multiple objectives, optional ones, and AND/OR conditions.
- **[States]** There is `Expired` but no `Failed` state, and no event for a task that cannot be completed any more.
- **[Save format]** `TaskTrackerSnapshot` has no version number, so a later change to the format cannot be migrated safely.
- **[Perf]** `Report` loops over every task on each call. With hundreds of tasks and frequent reports an index from action key to tasks would make it O(matching tasks).
- **[Limits]** The set of tasks is fixed when the tracker is created, so daily or generated tasks cannot be added while running.
<!-- review:end -->

## Notes and ideas

_Add your own here._
