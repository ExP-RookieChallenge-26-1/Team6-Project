# Skill VFX Architecture Overhaul Implementation Plan

## Step Order

1. [x] Add contract enums and fields with safe serialization defaults.
   - `VfxEndpoint.mirrorOffsetXWithCastDirection`
   - `VfxFlipMode`
   - `VfxAuthoredFacing`
   - `VfxAttachMode`
   - `SkillVfxCue.flipMode`
   - `SkillVfxCue.authoredFacing`
   - `SkillVfxCue.attachMode`
2. [x] Add `SkillVfxVisualRoot` component and visual transform lookup.
3. [x] Update endpoint resolution to mirror local X offset by cast direction only
   when the endpoint requests it.
4. [x] Add tests for same definition in both directions and mirrored front offsets.
5. [x] Add visual flip calculation and tests that root scale is not the intended
   authoring path when `SkillVfxVisualRoot` exists.
6. [x] Add attach-mode support for follow-spawn-actor cues.
7. [ ] Move only the necessary playback ownership from `SkillVfxPlayer` toward
   `SkillVfxRunner`; keep public compatibility methods stable.
8. [ ] Pilot migrated projectile behavior on one projectile skill definition.
9. [ ] Audit editor builders so normal validation cannot overwrite designer prefab
   appearance values.
10. [ ] Leave all-skill migration and legacy field removal for follow-up tasks.

## First Slice Verification

- `dotnet build "Project 2048.slnx" -nologo -v:minimal -p:UseSharedCompilation=false`
  passed with 0 warnings and 0 errors.
- Unity EditMode `Project2048.Tests.SkillVfxPlayerTests` passed 16/16.
- Unity EditMode
  `Project2048.Tests.CombatPresentationEffectTests.CombatWorldSpriteView_SlashArcSource_UsesAuthoredPlayerFrontOffset`
  passed 1/1.
- `git diff --check` passed for files touched by this slice.

## Validation

- `dotnet build "Project 2048.slnx" -nologo -v:minimal -p:UseSharedCompilation=false`
- Unity EditMode targeted tests:
  - `Project2048.Tests.SkillVfxPlayerTests`
  - relevant `Project2048.Tests.CombatPresentationEffectTests`
  - any new tests added for mirror/flip/attach behavior
- `git diff --check -- <touched files>`

## Risk Areas

- `Assets/Scripts/Skills/SkillVfxCue.cs`
- `Assets/Scripts/Skills/VfxEndpoint.cs`
- `Assets/Scripts/Presentation/SkillVfxPlayer.cs`
- `Assets/Scripts/Presentation/SkillVfxRunner.cs`
- `Assets/Scripts/Presentation/CombatProjectileEffect.cs`
- skill assets that already have authored `vfxDefinition` cues
- editor builder scripts that write VFX defaults into skill assets

## Stop Conditions

- Any asset migration starts touching unrelated skill balance fields.
- Legacy fields need deletion to make a first-slice test pass.
- A normal builder/validator path rewrites prefab particle values.
- A new runtime branch checks a specific skill ID or assumes enemy-owned casts.
