# Design

## Runtime Seams

- Keep VFX routing in `CombatWorldSpriteView`, because preview and combat presentation already share this path.
- Use existing `SkillSO.vfx` fields and current prefab resolution before adding new serialized data.
- Keep heavy strike handlers unchanged; only slash-family handlers should borrow the heavy-strike hit effect.

## Asset Direction

- Resolve ExP source art from `Assets/Art/Source/ExP/Effects`.
- Resolve Easy Explosion from the existing VFX Test prefab path already used by fire skills.
- Tint explosion prefabs/components at spawn time when possible so the underlying prefab can remain reusable.

## Validation

- Add or update focused EditMode tests around affected presentation branches.
- Run `dotnet build "Project 2048.slnx" -nologo -v:minimal -p:UseSharedCompilation=false`.
