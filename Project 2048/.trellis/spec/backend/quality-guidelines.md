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

---

## Code Review Checklist

<!-- What reviewers should check -->

(To be filled by the team)
