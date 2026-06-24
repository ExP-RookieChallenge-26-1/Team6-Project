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
- `CombatManager.BeginSkillPresentationLock(float durationSeconds)`
- `CombatManager.ClearSkillPresentationLock()`
- `CombatSnapshot.IsSkillPresentationLocked`
- `CombatUiView.RenderActionPanel()`
- `CombatWorldSpriteView.HandlePlayerSkillUsed(SkillSO skill, EnemyController target)`

#### 3. Contracts
- `CombatManager.RequestUseSkill(...)` and `RequestUseSkillById(...)` must
  return `false` while `IsSkillPresentationLocked` is true.
- The first accepted skill still spends cost and executes gameplay immediately.
- `CombatWorldSpriteView` may call `BeginSkillPresentationLock(...)` after it
  resolves the visual lifetime for the accepted skill.
- UI buttons must derive interactability from `CombatSnapshot`, not from direct
  presentation object references.

---

## Server State

<!-- How server data is cached and synchronized -->

### Validation & Error Matrix
- `durationSeconds <= 0` -> do not enter a lock.
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
- Good: `CombatWorldSpriteView` reports a resolved VFX duration, `CombatManager`
  locks for that duration, and `CombatUiView` disables skill buttons from the
  snapshot.
- Base: no authored VFX duration is available; fallback family duration is used.
- Bad: UI disables buttons locally but `CombatManager.RequestUseSkill` still
  accepts a second skill request.

### Tests Required
- Manager regression: while locked, skill request returns false and target HP is
  unchanged; after clear, the same request can succeed.
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
