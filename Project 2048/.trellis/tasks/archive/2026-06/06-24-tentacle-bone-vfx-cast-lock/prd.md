# Tentacle bone animation and VFX cast lock

## Goal

Replace the current procedural tentacle-strike presentation with an authored
bone/sprite animation based on the ExP tentacle source image, and prevent the
player from casting another skill in the battle scene until the current skill's
presentation VFX has finished.

The user-facing result should match the supplied reference direction: a
tentacle emerges from the player's side / ground area, bends toward the enemy,
strikes, then retracts or fades cleanly before another skill can be cast.

## Confirmed Facts

- The active checkout is clean on `Feature/vfx-restructure-phase1`.
- The requested source art exists at
  `Assets/Art/Source/ExP/Effects/Effect_Tentacle.png`.
- A visually identical larger source copy exists under `Assets/Art/Source/ExP 1`,
  but that copy does not carry Unity bone metadata.
- `Assets/Art/Source/ExP/Effects/Effect_Tentacle.png.meta` already contains a
  seven-bone sprite rig and weights.
- The project already has `com.unity.2d.animation` installed.
- The current `TentacleStrike` skill uses `SkillVfxFamily.TentacleWhip`.
- Runtime and preview currently render tentacle strike with procedural
  LineRenderer geometry in `CombatWorldSpriteView`.
- Skill use is currently accepted immediately by `CombatManager.RequestUseSkill`
  while the combat phase is `ActionPhase`; the action skill UI only disables
  buttons by skill availability and cost.
- First-turn skill usability is controlled by `CombatManager.ResolveBoardPhase()`
  after `CostConverter.ConvertBoardToCost(...)` converts the 2048 board into
  action cost.
- `PrototypePlayer.asset` has `initialBoardMoveCount: 12`, and
  `BattleScene.unity` currently serializes `StageFlowController.boardMoveCount`
  as 12, but scattered low tiles can still produce less than the 10-cost floor
  used by the current skill balance pool.
- Prototype skill costs are authored in 10/20/30/40 tiers; the cheapest current
  skills cost 10.

## Requirements

- Use the ExP tentacle source image, not the generic `Trail2D` tentacle texture.
- Preserve the current skill data surface: `TentacleStrike.asset` remains the
  gameplay skill, and VFX wiring stays on the existing per-skill/prefab path.
- Build an authored tentacle prefab path that can be animated with Unity's 2D
  bone/sprite animation tooling.
- The tentacle animation should visually strike toward the active enemy, using
  placement and timing that matches battle and preview scenes.
- Keep the existing procedural tentacle fallback only if needed for tests or
  compatibility; the authored prefab should be the preferred path.
- While a skill presentation is active, prevent the battle scene from accepting
  another skill cast.
- The lock must release when the skill's presentation duration completes, even
  if the VFX is prefab-based rather than ParticleSystem-only.
- Avoid delaying core gameplay execution unless explicitly needed; the first
  skill's cost, damage, and status effects should keep the existing flow.
- Preserve combat result correctness: victory/defeat and enemy death fade should
  still work after a locked skill presentation.
- Fix first-turn action pacing so the first action phase can always afford at
  least the cheapest currently equipped usable skill when the board produced any
  cost.
- Avoid globally inflating every board result if a narrower first-turn floor
  solves the usability problem.

## Acceptance Criteria

- [x] `TentacleStrike` uses an authored prefab based on
      `Assets/Art/Source/ExP/Effects/Effect_Tentacle.png`.
- [x] The authored tentacle prefab contains or uses the ExP sprite rig/bone
      setup rather than a ParticleSystem-only or LineRenderer-only effect.
- [x] Runtime and preview paths show the authored tentacle strike instead of the
      old procedural whip when the prefab is available.
- [x] The old procedural tentacle rendering does not break existing preview
      coverage if used as fallback.
- [x] The battle scene rejects/ignores additional skill input while a skill
      presentation lock is active.
- [x] Skill buttons visually become unavailable or non-interactable while the
      presentation lock is active.
- [x] The presentation lock releases after the VFX duration so the next skill can
      be cast.
- [x] First action phase cost is floored high enough to cast the cheapest
      equipped skill when that cost would otherwise be below the skill-cost
      floor.
- [x] Later turns continue to use normal board-to-cost conversion unless their
      cost is modified by existing carried-cost or skill effects.
- [x] Targeted EditMode tests cover authored tentacle asset wiring and skill
      presentation locking.
- [x] Targeted EditMode tests cover first-turn minimum action cost.
- [x] `dotnet build "Project 2048.slnx" -nologo -v:minimal -p:UseSharedCompilation=false`
      passes, or any blocking failure is reported.

## Notes

- User approved implementation after reviewing the recommended presentation-lock
  approach: accept and execute the first skill immediately, then block only
  subsequent skill requests until presentation VFX finishes.
