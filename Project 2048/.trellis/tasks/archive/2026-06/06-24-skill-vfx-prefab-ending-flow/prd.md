# Skill VFX prefab playback and ending story flow

## Goal

Make the next VFX pass work from authored prefab GameObjects instead of ParticleSystem-only references, then wire the final stage completion path to show the ending story with the ending background.

## Requirements

- Existing local VFX restructure work is preserved before new edits.
- Skill VFX cues spawn prefab GameObjects, and those prefabs may contain ParticleSystem components, VisualEffect Graph components, regular child GameObjects, or projectile controllers.
- Projectile-prefab playback must not silently skip VisualEffect Graph content.
- Fire-family skills use the VFX Test assets:
  - impact uses `Assets/VFX Test/Prefab/vfx_EasyExplosion.prefab`.
  - foot fire uses `Assets/VFX Test/Prefab/vfx_Fire.prefab`.
- Heal-family skills use `Assets/VFX Test/Prefab/vfx_Healing.prefab`.
- Tentacle skill presentation should be set up as an authored prefab animation path rather than another ParticleSystem-only slot. A full hand-authored bone animation pass is allowed to remain prefab/asset focused if Unity editor tooling limits direct rig authoring in this session.
- Chain projectile speed should be slower than the current authored chain projectile.
- Clearing the final configured stage, including stage 20, loads `StoryScene` in ending mode and displays ending dialogue over the ending background.

## Acceptance Criteria

- [ ] The current branch is consistently named `Feature/vfx-restructure-phase1`.
- [ ] Existing WIP is committed separately from the new task changes.
- [ ] `SkillVfxPlayer`/projectile playback supports prefab GameObjects with VisualEffect Graph content as well as ParticleSystem content.
- [ ] Fireball/BurstFireball/BurnOut VFX cues include the requested foot fire and hit explosion where appropriate.
- [ ] LightRecover/FocusBreath use the VFX Test healing prefab through prefab-based cues.
- [ ] Chain projectile travel is visibly slower by data or prefab configuration.
- [ ] StoryScene has an ending story data reference and can apply the ending background from the imported ending asset.
- [ ] Existing final-stage flow tests still prove run completion requests the ending scene; new or updated tests cover ending story/background data behavior where practical.
- [ ] `dotnet build "Project 2048.slnx" -nologo -v:minimal -p:UseSharedCompilation=false` passes, or any failure is reported with the blocking reason.

## Notes

- User explicitly prefers fewer VFX indirection layers and direct skill/prefab edit points.
