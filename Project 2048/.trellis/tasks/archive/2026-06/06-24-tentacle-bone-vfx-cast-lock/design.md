# Design

## Boundaries

- Keep gameplay math inside `CombatManager` / `SkillExecutor`; this task should
  not move damage or cost timing into presentation code.
- Use `CombatWorldSpriteView` as the runtime/preview presentation owner.
- Keep `SkillSO.vfx` / prefab fields as the main skill edit surface.
- Avoid another ScriptableObject indirection layer.
- Prefer a data/prefab solution for tentacle art, with code only for placement,
  playback, and lock timing.

## Tentacle Asset Path

- Preferred source sprite:
  `Assets/Art/Source/ExP/Effects/Effect_Tentacle.png`.
- This sprite already has a seven-bone rig and weights in its importer metadata.
- The larger `Assets/Art/Source/ExP 1` copy is useful as source evidence, but it
  lacks bone metadata and should not be the primary runtime sprite unless the rig
  is copied or rebuilt.

## Tentacle Prefab Strategy

- Create or update an authored tentacle strike prefab under the existing
  SkillVFX prefab area.
- The prefab should use the ExP tentacle sprite and 2D Animation/SpriteSkin
  components where Unity supports the serialized setup.
- Use the prefab's serialized SpriteSkin bone chain plus a focused presentation
  component for the strike motion. This keeps the authored rig in the prefab
  while letting runtime placement bend toward the active enemy.
- Keep sizing, sorting, and placement consistent with the existing
  `TentacleStrikeDurationSeconds` timing unless visual testing shows a different
  duration is required.
- If direct text editing of SpriteSkin serialized data proves too brittle, use
  Unity editor APIs/MCP to construct the prefab and verify it in Unity before
  committing.

## Runtime Presentation Flow

- `TentacleStrike.asset` should point to the authored prefab path through the
  existing VFX fields or cue path.
- `CombatWorldSpriteView` should prefer the authored prefab for
  `SkillVfxFamily.TentacleWhip`.
- If no authored prefab is available, the current LineRenderer tentacle path can
  remain as fallback.
- Preview and actual combat playback should share the same tentacle prefab path
  so the showcase does not drift from runtime.

## Skill Presentation Lock

- Add a combat-level presentation lock, not only a UI button flag.
- `CombatManager.RequestUseSkill` and `RequestUseSkillById` should reject new
  skill requests while the lock is active.
- The first skill still executes immediately after being accepted.
- `CombatWorldSpriteView`, which already knows the resolved visual lifetime, can
  signal the lock duration around skill presentation.
- UI snapshots should expose enough state for `CombatUiView` to render skill
  buttons non-interactable during the lock.
- The lock should release by timer after the resolved presentation duration, with
  a small safety clamp for zero-duration effects.

## First-Turn Cost Floor

- The cost usability issue should be fixed at the combat action-cost boundary,
  not by editing every starting skill cost.
- Do not change `CostConverter` globally unless tests show the whole board-cost
  curve is wrong; that converter is shared by every turn and already has
  coverage for merge rewards.
- Add a first-action-turn floor in `CombatManager.ResolveBoardPhase()`:
  - only applies when resolving the first player turn's board phase;
  - only applies when the converted board cost is positive but below the
    cheapest currently equipped usable skill cost;
  - includes carried cost and existing next-cost modifiers after the floor logic
    in a predictable order;
  - leaves later turns on the normal board-to-cost curve.
- The floor should use actual equipped skill costs so it follows future balance
  changes instead of hardcoding `10`.
- If all equipped skills are unavailable or cost 0, the floor should not add
  unnecessary cost.

## Compatibility

- Existing tests that call `CombatManager.RequestUseSkill` in edit mode should
  still pass unless they intentionally attempt back-to-back casts during a lock.
- Existing final-stage and victory behavior should not depend on the lock ending
  before combat result calculation.
- Because gameplay execution remains immediate, save/combat result logic should
  be lower risk than delaying execution until after VFX.
- First-turn cost floor should not affect direct test setups that explicitly set
  board state and later turn counts.

## Rollback

- If the authored SpriteSkin prefab is unstable in serialized form, keep the
  ExP sprite imported with bones and land the cast-lock code separately only if
  it is independently verified.
- If the lock causes UI snapshot churn, fall back to CombatManager-only rejection
  first, then add UI rendering in a smaller follow-up.
- If first-turn cost floor changes too many existing expectations, limit it to
  the stage/runtime path instead of the core manager; the current recommended
  implementation is core-manager first because tests can cover it directly.
