# Polish fire shield slash skill VFX

## Goal

Raise fire projectile origins, align ExP fire/explosion coloring, simplify shield hit VFX, and route slash skills through ExP beam attacks with heavy-strike hit impact.

## Requirements

- Fire skill projectiles spawn from the player's upper body/hand area, not the feet.
- Fire skill visuals use the fire asset from the ExP effect folder, and impact explosions are tinted to match the fire projectile.
- Shield bash/burst visuals are simplified: keep the shield projectile readable, then show the Easy Explosion VFX at impact.
- Heavy/strong strike skills keep their current presentation.
- Slash skills use the ExP attack beam from the player toward the enemy and show the heavy-strike hit effect on impact.
- Runtime skill VFX should continue to complete before the next skill can be cast.

## Acceptance Criteria

- [ ] Fireball-family skills launch higher from the player in runtime and preview paths.
- [ ] Fire explosion color is close to the fire projectile color.
- [ ] Shield bash/burst no longer spawn the noisy shield burst composition; they show a thrown shield plus Easy Explosion impact.
- [ ] Slash-family skills emit an ExP attack beam from player to enemy and preserve heavy-strike impact readability.
- [ ] Existing heavy strike behavior remains unchanged.
- [ ] Targeted presentation tests and a solution build pass.

## Notes

- User reference image: `C:\Users\hana\Downloads\IMG_1633.png`.
- Prefer existing `SkillSO.vfx` / `CombatWorldSpriteView` seams over new package ScriptableObjects.
