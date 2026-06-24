# Design

## Boundaries

- Keep the existing `SkillSO.vfxDefinition` cue model as the main edit surface for new authored effects.
- Do not add another ScriptableObject package layer for this pass.
- Keep legacy `SkillVfxTuning` fallback behavior intact for skills that have not yet migrated.

## VFX Playback

- `SkillVfxCue.prefab` remains a `GameObject` reference.
- `SkillVfxPlayer.Play` instantiates cue prefabs at resolved player/enemy Feet/Body/Head placements.
- Non-projectile cue instances call a shared visual replay path that plays child `ParticleSystem` and child `VisualEffect` components.
- Projectile cue instances still use `CombatProjectileEffect`, but the projectile component must also support VisualEffect content during travel/impact instead of only ParticleSystem arrays.
- Data migration for requested skills should prefer cue entries over legacy particle-only fields.

## Requested Skill Mapping

- Fireball-family:
  - activation/projectile remains authored as the current fireball projectile when present.
  - enemy feet cue uses VFX Test `vfx_Fire.prefab`.
  - impact cue uses VFX Test `vfx_EasyExplosion.prefab`.
- Heal-family:
  - player body/feet cue uses VFX Test `vfx_Healing.prefab`.
- Tentacle:
  - keep using an authored prefab path and avoid expanding the procedural particle path.
  - if bone rig authoring is too Unity-editor-specific for text editing, leave the prefab hook/data ready and document the remaining editor rigging step.
- Chain:
  - change projectile timing on the authored chain projectile prefab or the skill cue so travel is slower without hardcoding a chain-only branch.

## Ending Flow

- Existing flow already routes final `StageResult.RunCompleted` to `GameContext.GameState.Ending` and `OnEndingSceneLoadRequested`.
- `SceneFlowManager.LoadEnding()` already loads `StoryScene`.
- `StoryController` already selects `endingStory` when the flow state is Ending.
- Missing integration is data/scene setup: create an ending story asset, add background sprite support to story data/view, and reference it from StoryScene.

## Compatibility

- Empty `vfxDefinition` must continue falling back to the existing procedural presentation path.
- Opening story behavior must remain unchanged when story data has no explicit background overrides.
- Final-stage flow must still save/close the run before requesting the ending scene.
