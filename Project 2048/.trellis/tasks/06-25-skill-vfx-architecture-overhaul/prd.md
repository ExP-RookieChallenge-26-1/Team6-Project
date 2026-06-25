# Skill VFX architecture overhaul

## Goal

Make skill VFX data actor-agnostic and reusable for both player and enemy casts.
Runtime code should resolve only `caster` and `primaryTarget`, then use each
skill's authored VFX definition to determine spawn position, optional
destination, direction, visual flip, attachment, impact timing, and lifetime.

The first implementation must improve the current `SkillVfxDefinition` path
without destabilizing the active PR. Full legacy removal and all-skill migration
are separate follow-up work after the core contract is proven.

## Current Facts

- The project already has `VfxActorRef.Caster` and `VfxActorRef.PrimaryTarget`.
- The project already has `VfxEndpoint` and `SkillVfxCue.spawnAt` /
  `destination` data.
- `SkillVfxRunner` already selects cue triggers, handles delays, and can invoke
  impact callbacks for projectile effects.
- `SkillVfxPlayer` still owns most endpoint resolving and playback behavior.
- The current data contract does not explicitly support cast-direction X offset
  mirroring, visual-only flip modes, authored facing, or attach modes.
- Legacy VFX fields and editor/builder paths still exist and must remain
  compatible during the first pass.

## Requirements

- VFX data must not encode player/enemy ownership. Use caster and primary
  target semantics only.
- VFX placement must be expressed as spawn endpoint plus optional destination
  endpoint. Target-only effects use only spawn.
- Endpoint local offsets must optionally mirror X by cast direction.
- Visual flipping must affect a visual child/root only, not the prefab root.
- Runtime direction logic must not special-case "enemy means flip".
- Projectile impact callbacks should be driven by the actual projectile impact
  event where that behavior is available.
- Designer-authored prefab values must not be overwritten by normal runtime or
  editor validation paths.
- Existing skill assets must keep working during migration.

## Acceptance Criteria

- [ ] The same skill VFX definition works when cast left-to-right and
      right-to-left.
- [ ] A mirrored X offset spawns in front of the caster in both directions.
- [ ] Self-buff and target-debuff VFX can be expressed without destination
      inference.
- [ ] Projectile impact callbacks fire from projectile arrival, not a fixed
      delay, for migrated projectile cues.
- [ ] Missing anchors fall back deterministically without throwing.
- [ ] No new VFX path adds player/enemy branches, per-skill ID branches, or
      additional `SkillVfxFamily` switches.
- [ ] EditMode tests cover mirror offsets, visual flip direction, projectile
      impact timing, and missing-anchor fallback.
- [ ] `dotnet build "Project 2048.slnx" -nologo -v:minimal
      -p:UseSharedCompilation=false` passes.

## Out Of Scope For First PR Slice

- Deleting all legacy `activationEffect`, `vfxPackage`, or `SkillVfxFamily`
  compatibility paths.
- Migrating every skill asset in one pass.
- Rebuilding or regenerating designer-authored VFX prefabs.
- Renaming every serialized field if doing so would create broad asset churn.

## Open Question

- Should the first implementation slice stay limited to the contract/runtime
  core plus one projectile pilot skill, or should it attempt the full sequential
  skill migration in this PR?

## Notes

- Keep `prd.md` focused on requirements, constraints, and acceptance criteria.
- Lightweight tasks can remain PRD-only.
- For complex tasks, add `design.md` for technical design and `implement.md` for execution planning before `task.py start`.
