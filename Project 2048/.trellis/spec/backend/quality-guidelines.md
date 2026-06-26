# Quality Guidelines

> Code quality standards for backend development.

---

## Overview

<!--
Document your project's quality standards here.

Questions to answer:
- What patterns are forbidden?
- What linting rules do you enforce?
- What are your testing requirements?
- What code review standards apply?
-->

(To be filled by the team)

---

## Forbidden Patterns

<!-- Patterns that should never be used and why -->

(To be filled by the team)

---

## Required Patterns

<!-- Patterns that must always be used -->

(To be filled by the team)

---

## Testing Requirements

<!-- What level of testing is expected -->

### Combat Outcome Regression Tests

When a player-initiated action can damage the player during its own resolution
(for example enemy thorn retaliation during `CombatManager.RequestUseSkill` or a
pending charged attack in `ResolvePendingChargedAttack`), resolve defeat before
victory.

Good case:
- `RequestUseSkill_PlayerKilledByEnemyThornGuard_RaisesDefeat` asserts
  `OnCombatDefeat` fires, `OnCombatVictory` does not fire, and the phase becomes
  `CombatPhase.Defeat`.
- `ChargeAttack_PlayerKilledByEnemyThornGuard_RaisesDefeat` covers the same
  contract for automatic next-turn charged attacks.

Bad case:
- Checking only `CheckVictory()` after player skill execution leaves the combat
  in action flow even when `PlayerCombatController.IsDead` is already true, or
  can turn a simultaneous death into victory.

### Coroutine Timing Regression Tests

When a behavior contract depends on a coroutine delay, tests should exercise the
public event/lock path and wait through the runtime timer. Do not call private
completion methods through reflection just to make the test synchronous; that
couples the test to implementation details and can miss broken timer wiring.

Good case:
- `PlayerSkill_PendingPresentationCompletesAfterFixedDelayBeforeUnlock` accepts
  a skill through `RequestUseSkillById`, lets `OnPlayerSkillUsed` call
  `BeginSkillPresentationLock(...)`, waits past the configured damage delay,
  then asserts damage/popup completion while the presentation lock remains.

Bad case:
- Invoking `CompletePendingSkillPresentation()` with `BindingFlags.NonPublic`
  proves only the private helper body, not that `StartCoroutine(...)`, sequence
  guards, or lock release timing are wired correctly.

---

## Code Review Checklist

<!-- What reviewers should check -->

(To be filled by the team)
