# Implementation Plan

1. Activate the task after planning artifacts exist.
2. Load code guidelines with `trellis-before-dev`.
3. Update prefab playback:
   - ensure `CombatProjectileEffect` can play VisualEffect Graph components for projectile prefabs.
   - add or update EditMode tests for prefab/VFX playback.
4. Wire requested skill data:
   - Fireball/BurstFireball/BurnOut cue entries for foot fire and impact explosion.
   - LightRecover/FocusBreath cue entries for healing prefab.
   - chain projectile speed by prefab/data timing.
   - tentacle prefab hook remains authored-prefab based.
5. Wire ending story:
   - add background sprite fields to story data/view in a backwards-compatible way.
   - create ending story data asset.
   - reference ending story on StoryScene.
6. Validate:
   - targeted EditMode tests for VFX player/projectile/story/flow.
   - `dotnet build "Project 2048.slnx" -nologo -v:minimal -p:UseSharedCompilation=false`.
   - `git diff --check`.
7. Commit task changes separately from the WIP preservation commit.

## Risk / Rollback

- Serialized Unity YAML edits can break references if GUID/fileID pairs are wrong. Verify with grep and build/tests.
- VisualEffect playback in EditMode may not behave like PlayMode; tests should assert component support and spawned object wiring, not full GPU rendering.
- Scene YAML edits should be minimal: only add the endingStory reference and any necessary serialized fields.
