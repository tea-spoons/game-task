# Changelog

## 0.2.0

- `TaskSet` inherits from `StaticDataObject` when [Static Data](https://github.com/tea-spoons/static-data)
  (0.1.0+) is installed, otherwise from `ScriptableObject` as before (Unity detects this automatically). No
  API change either way; a `TaskSet` gains an `Id` and can be indexed by a `StaticDataLibrary` when the
  integration is active.

## 0.1.1

- No code changes. Independent development continues from this release, outside of Bigpoint; 0.1.0 was the last version developed there.

## 0.1.0

- Initial version: task definitions and tracker with progress by action key, prerequisites, expiry, claiming and snapshots, optional Game Reward and Localizer integrations.
