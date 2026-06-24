# Implementation Plan

1. [x] Activate the task after user review of this plan.
2. [x] Load pre-development guidelines with `trellis-before-dev`.
3. [x] Verify Unity editor state and current console before asset edits.
4. [x] Tentacle asset/prefab work:
   - inspect the ExP tentacle sprite importer in Unity;
   - create/update an authored tentacle strike prefab using the ExP sprite;
   - add SpriteSkin/bone chain usage where Unity serialization is stable;
   - use `TentacleBoneStrikeEffect` for directed emerge/strike/retract motion;
   - connect the prefab through the current skill VFX prefab path.
5. [x] Runtime tentacle playback:
   - update `CombatWorldSpriteView` so `TentacleWhip` prefers the authored
     prefab path;
   - keep the procedural LineRenderer tentacle as fallback;
   - preserve preview/runtime parity.
6. [x] Presentation lock:
   - add a lock state to `CombatManager` and expose it via snapshot;
   - reject new skill requests while locked;
   - have `CombatWorldSpriteView` lock for the resolved presentation duration;
   - have `CombatUiView.RenderActionPanel` disable skill buttons during lock.
7. [x] First-turn cost floor:
   - add first-player-action floor in `CombatManager.ResolveBoardPhase`;
   - derive the floor from the cheapest equipped nonzero skill cost;
   - add regression coverage for first-turn castability.
8. [x] Tests:
   - update tentacle preview tests to assert authored prefab/SpriteSkin path when
     available;
   - add manager tests proving a second skill cast is rejected during the
     presentation lock and accepted after release;
   - add cost tests proving low first-turn board output can still cast the
     cheapest equipped skill;
   - keep existing cost/skill behavior tests green.
9. [x] Validation:
   - [x] Unity console errors: zero;
   - [x] targeted EditMode tests for tentacle presentation and cast lock;
   - [x] `dotnet build "Project 2048.slnx" -nologo -v:minimal -p:UseSharedCompilation=false`;
   - [x] `git diff --check`.
10. Commit feature changes separately from Trellis archive/journal cleanup.

## Risky Files

- `Assets/Scripts/Prototype/CombatWorldSpriteView.cs`
- `Assets/Scripts/Combat/CombatManager.cs`
- `Assets/Scripts/Combat/CombatSnapshot.cs`
- `Assets/Scripts/Prototype/CombatUiView.cs`
- `Assets/Data/Skills/TentacleStrike.asset`
- authored tentacle prefab / animation assets under `Assets/Art/Effects/SkillVFX`
- `Assets/Tests/EditMode/CombatManagerTests.cs`
- `Assets/Tests/EditMode/CombatUiViewTests.cs`
- `Assets/Tests/EditMode/CombatPresentationEffectTests.cs`

## Review Gate

User approved the recommended presentation-lock product decision:

- accept and execute the first skill immediately;
- block subsequent skill requests until presentation VFX finishes;
- do not delay damage/effects until after VFX.
