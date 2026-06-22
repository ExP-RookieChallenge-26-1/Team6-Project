# Team6-Project Agent Guide

## Role

You are a careful coding agent working in this repository.
Make small, correct, reviewable changes.
Do not perform large rewrites unless the task explicitly asks for them.

## Core Rules

- Read relevant files before editing.
- Make the smallest change that solves the task.
- Do not rewrite unrelated systems.
- Do not rename public APIs, folders, files, or configuration unless required.
- Do not add dependencies without explaining why.
- Do not delete existing behavior silently.
- Add or update tests when behavior changes.
- Run relevant verification before finishing.
- If verification cannot be run, explain why.
- Do not revert or overwrite existing user work in the working tree.

Final responses must include:

- what changed
- what was verified
- files changed
- remaining risks

## Repository Rules

- The real Git root is this directory, not the nested `Project 2048/` folder.
- The Unity project lives under `Project 2048/`.
- Prefer existing project structure over inventing a new one.
- Follow the style already present in the repository.
- Keep code, configuration, documentation, and tests clearly separated when practical.
- Avoid speculative abstractions.
- Do not create large new systems for small tasks.
- Preserve backward compatibility unless the task explicitly allows breaking changes.
- Do not edit Unity-generated folders such as `Library/`, `Logs/`, `Temp/`, or `UserSettings/` unless the task explicitly requires it.

## Unity UI Rules

- All UI must be authored at design time in Unity scenes or prefabs.
- Do not create, position, resize, recolor, restyle, or add UI layout/effect components from gameplay/runtime scripts.
- Runtime UI scripts may bind serialized references, subscribe to events, toggle active state, update interactable state, and change displayed data.
- If a UI layout or style changes, update the scene/prefab asset and add or update EditMode tests that read the authored asset.
- `Project 2048/Assets/Scenes/MainMenu.unity` is the design-time source of truth for the main menu layout.

## Task Discipline

Before making non-trivial changes:

1. Identify the goal.
2. Identify relevant files.
3. State non-goals.
4. Choose the smallest safe plan.
5. Decide how the result will be verified.

For larger tasks, create or update a task brief in `tasks/`.

## Commands

Prefer these commands when available:

- `scripts/doctor`
- `scripts/bootstrap`
- `scripts/check`
- `scripts/test`
- `scripts/eval`

On Windows:

- `./scripts/doctor.ps1`
- `./scripts/bootstrap.ps1`
- `./scripts/check.ps1`
- `./scripts/test.ps1`
- `./scripts/eval.ps1`

If these scripts are missing or not applicable, inspect the repository and use the closest existing commands.

## Done Definition

A task is done only when:

- The requested change is implemented.
- The change is focused and reviewable.
- Relevant checks or tests were run.
- No unrelated files were changed.
- Any important durable decision was recorded.
- Remaining risks are clearly reported.

## Failure Handling

If a command fails:

1. Read the error.
2. Identify the likely cause.
3. Fix it if it is in scope.
4. Re-run the command.
5. If it still fails, report the blocker clearly.

Never claim success without verification.
