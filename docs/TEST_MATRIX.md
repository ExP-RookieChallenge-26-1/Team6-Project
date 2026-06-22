# Test Matrix

Every completed task needs proof.

Possible proof:

- automated test
- compile check
- lint or type check
- build command
- manual validation checklist

| Work type | Expected proof |
| --- | --- |
| Pure refactor | Existing checks pass. Behavior unchanged. |
| Bug fix | Regression test if practical. |
| New feature | Test or manual validation. |
| UI-only change | Screenshot or manual validation if automation is unavailable. |
| Config change | Explain affected behavior and run relevant checks. |
| Dependency change | Explain why it is needed and verify install or build. |
| Architecture change | Update `docs/decisions.md` and run broad checks. |

## Manual Validation

Use this when automated tests are unavailable:

- The project opens or starts successfully.
- The changed feature can be exercised.
- Existing core behavior still works.
- No new obvious errors appear.
- The validation steps are written in the final report.
