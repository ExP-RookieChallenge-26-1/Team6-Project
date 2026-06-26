# State Management

> How state is managed in this project.

---

## Overview

<!--
Document your project's state management conventions here.

Questions to answer:
- What state management solution do you use?
- How is local vs global state decided?
- How do you handle server state?
- What are the patterns for derived state?
-->

(To be filled by the team)

---

## State Categories

<!-- Local state, global state, server state, URL state -->

### Combat presentation snapshot state

#### 1. Scope / Trigger
- Trigger: combat presentation state that affects both runtime command
  acceptance and UI interactability.
- Owner: `CombatManager` owns combat truth; presentation/UI layers only consume
  snapshots or signal resolved visual duration.

---

## When to Use Global State

<!-- Criteria for promoting state to global -->

### Contract: Skill presentation lock

#### 2. Signatures
- `CombatManager.BeginSkillPresentationLock(float durationSeconds, float pendingCompletionDelaySeconds = 0f)`
- `CombatManager.ClearSkillPresentationLock()`
- `CombatSnapshot.IsSkillPresentationLocked`
- `CombatUiView.RenderActionPanel()`
- `CombatWorldSpriteView.HandlePlayerSkillUsed(SkillSO skill, EnemyController target)`

#### 3. Contracts
- `CombatManager.RequestUseSkill(...)` and `RequestUseSkillById(...)` must
  return `false` while `IsSkillPresentationLocked` is true.
- The first accepted skill still spends cost immediately.
- Normal skill gameplay/damage may complete before the lock releases when
  `pendingCompletionDelaySeconds > 0`; this is used so damage numbers can
  appear a fixed time after skill cast while skill buttons stay locked until
  the presentation lock ends.
- `CombatWorldSpriteView` may call `BeginSkillPresentationLock(...)` after it
  resolves the visual lifetime for the accepted skill.
- `CombatWorldSpriteView.HandlePlayerSkillUsed(...)` passes
  `SkillDamageDelaySeconds` (`0.1f`) for normal player skills.
- UI buttons must derive interactability from `CombatSnapshot`, not from direct
  presentation object references.

---

## Server State

<!-- How server data is cached and synchronized -->

### Validation & Error Matrix
- `durationSeconds <= 0 && pendingCompletionDelaySeconds <= 0` -> do not enter
  a lock; complete any pending presentation immediately.
- `pendingCompletionDelaySeconds <= 0` -> complete pending gameplay only when the
  lock releases.
- `pendingCompletionDelaySeconds > 0` -> complete pending gameplay after
  `pendingCompletionDelaySeconds`, but keep `IsSkillPresentationLocked` true
  until at least `durationSeconds`.
- New skill request while locked -> return `false`; do not spend cost; do not
  damage targets.
- Lock timer completes -> clear `IsSkillPresentationLocked` and emit a fresh
  combat snapshot.
- New combat starts -> clear any previous presentation lock before the first
  snapshot.

---

## Common Mistakes

<!-- State management mistakes your team has made -->

### Good / Base / Bad Cases
- Good: a normal attack reports a resolved VFX duration, damage/popup completes
  `0.1s` after skill cast, and skill buttons stay disabled until the
  presentation lock ends.
- Base: no authored VFX duration is available; fallback family duration is used.
- Bad: UI disables buttons locally but `CombatManager.RequestUseSkill` still
  accepts a second skill request.
- Bad: damage/popup for normal skills waits until the VFX is fully gone, making
  the hit feel late.
- Bad: normal skill damage timing is derived from VFX duration instead of the
  fixed `0.1s` cast delay.

### Tests Required
- Manager regression: while locked, skill request returns false and target HP is
  unchanged; after clear, the same request can succeed.
- Manager regression: a normal pending skill can complete damage/popup while
  `IsSkillPresentationLocked` remains true.
- UI/snapshot regression: `CombatSnapshot.IsSkillPresentationLocked` makes skill
  slots non-interactable.
- Presentation regression: prefab-backed and fallback VFX paths both return a
  finite visual duration.

### Wrong vs Correct

#### Wrong
```csharp
button.interactable = canAfford;
```

#### Correct
```csharp
button.interactable = canAfford && snapshot?.IsSkillPresentationLocked != true;
```

### Contract: Charged Attack Release Timing

#### 1. Scope / Trigger
- Trigger: a queued `SkillEffectKind.ChargeAttack` such as `gather-light` crosses from one player turn into the next.
- Owner: `CombatManager` owns when the queued gameplay resolves; `CombatWorldSpriteView` owns visual duration reporting for the release effect.

#### 2. Signatures
- `CombatManager.ResolveBoardPhase()`
- `PlayerCombatController.TryConsumePendingChargedAttack(...)`
- `CombatManager.OnPlayerChargedAttackReleased(string skillName, int chargedPower, int attackCount, EnemyController target)`
- `CombatWorldSpriteView.HandlePlayerChargedAttackReleased(...)`

#### 3. Contracts
- A queued charged attack must not release during `CombatPhase.PlayerTurnStart`.
- The next player turn must enter `CombatPhase.BoardPhase` with the charged attack still pending.
- The release starts only after the board is resolved and `ResolveBoardPhase()` transitions into `CombatPhase.ActionPhase`.
- While release VFX is active, `CombatSnapshot.IsSkillPresentationLocked` must be true and skill buttons must remain non-interactable.
- For `gather-light`, the release lock must cover projectile travel plus the vertical beam lifetime, not only the beam spawn moment.
- Unlike normal skills, `gather-light` charged release does not use the `0.1s`
  damage delay; release damage/popup completes when the release presentation
  lock ends.

#### 4. Validation & Error Matrix
- No pending charged attack -> `ResolveBoardPhase()` stays a normal board-to-action transition.
- Pending charged attack with presentation listener -> emit `OnPlayerChargedAttackReleased`, lock presentation, delay damage until `ClearSkillPresentationLock()`.
- Pending charged attack without presentation listener -> resolve damage immediately and remain in `ActionPhase` unless victory/defeat occurs.
- Release kills player through retaliation -> enter `Defeat` before any further action availability.
- Release kills final enemy -> enter `Victory`.

#### 5. Good / Base / Bad Cases
- Good: player ends turn after queuing `gather-light`, plays the next board, enters skill selection, release VFX locks buttons, then damage resolves and skills become available.
- Base: no presentation view is subscribed; charged damage resolves immediately when skill selection starts.
- Bad: release fires at `PlayerTurnStart`, leaving the previous turn's board visible during the release.
- Bad: buttons unlock after projectile impact while the delayed vertical beam is still playing.

#### 6. Tests Required
- `CombatManagerTests.ChargeAttack_FiresWhenNextActionPhaseStarts`
- `CombatManagerTests.ChargeAttack_StacksQueuedUsesAndReleasesCountAtNextActionPhase`
- `CombatManagerTests.ChargeAttack_WithPresentationListener_DelaysReleaseDamageAndAggregatesPopup`
- `CombatPresentationEffectTests.CombatWorldSpriteView_ChargedAttackRelease_OnlyKeepsVerticalBeamWithoutChargedBeam`

#### 7. Wrong vs Correct

#### Wrong
```csharp
ChangePhase(CombatPhase.PlayerTurnStart);
if (ResolvePendingChargedAttack())
{
    return;
}
StartBoardPhaseForCurrentPlayerTurn();
```

#### Correct
```csharp
StartBoardPhaseForCurrentPlayerTurn();

// Later, after the board resolves:
ChangePhase(CombatPhase.ActionPhase);
ResolvePendingChargedAttack();
```
