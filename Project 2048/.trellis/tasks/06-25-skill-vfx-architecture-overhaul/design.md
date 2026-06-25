# Skill VFX Architecture Overhaul Design

## Direction

Use the existing `SkillVfxDefinition` pipeline as the migration surface. The
project already has most of the vocabulary from the proposal: definitions, cues,
spawn endpoints, optional destinations, caster/primary-target actor references,
anchor providers, and a runner. The first slice should complete that contract
instead of introducing another parallel VFX layer.

## Data Contract

Extend the current serialized types with conservative defaults:

- `VfxEndpoint.mirrorOffsetXWithCastDirection`
  - Default `false`.
  - When enabled, local X offset is multiplied by the cast direction sign before
    it is transformed by the resolved anchor.
- `VfxFlipMode`
  - `None`
  - `CasterToTarget`
  - `SpawnToDestination`
- `VfxAuthoredFacing`
  - `Right`
  - `Left`
- `VfxAttachMode`
  - `World`
  - `FollowSpawnActor`
- `SkillVfxCue.flipMode`, `authoredFacing`, and `attachMode`
  - Defaults must preserve existing visuals.
- `SkillVfxVisualRoot`
  - Optional component on VFX prefabs.
  - If present, scale/rotation flips should apply to the visual transform it
    exposes.
  - If absent, runtime may fall back to the spawned root only for compatibility,
    but new prefabs should use the visual-root component.

Avoid serialized field renames in the first slice unless Unity migration is
explicitly handled. `spawnAt` and `destination` can stay as code names while the
design language uses spawn/destination.

## Runtime Ownership

Move ownership toward `SkillVfxRunner`:

- Select cues for the requested trigger.
- Apply cue delay.
- Resolve spawn and destination endpoints.
- Compute cast direction and optional offset mirroring.
- Spawn prefab and apply scale/tint.
- Apply visual-only flip.
- Apply attach mode.
- Start behavior playback.
- Subscribe to impact events and invoke impact callbacks.
- Handle lifetime and destroy behavior.

`SkillVfxPlayer` can remain during the first slice as a helper or compatibility
facade, but new behavior should trend toward runner-owned execution.

## Endpoint Resolving

Endpoint resolution should use `CombatVfxAnchorProvider` when available:

1. Resolve actor from `caster` or `primaryTarget`.
2. Resolve requested socket from the provider.
3. Fall back to actor transform if the provider or socket is missing.
4. Apply mirrored local X offset when requested.
5. Transform the offset through the resolved anchor.

Direction sign is derived from world positions, not actor identity:

- Prefer caster to primary-target direction for caster-relative VFX.
- Prefer spawn to destination direction when `SpawnToDestination` is requested
  and a destination exists.
- Fall back to current transform facing or positive X if positions overlap.

## Behavior

Keep behavior migration incremental:

- Static particle cues remain immediate spawn/play behavior.
- Projectile cues use `CombatProjectileEffect.Impacted` for impact callbacks.
- Beam/midpoint behavior stays compatible until runner-owned behavior tests are
  in place.
- A later slice can extract `SkillVfxBehaviour` and `SkillVfxPlayContext` after
  the current player/runner boundary has tests around parity.

## Migration Plan

Start with a narrow pilot:

1. Complete data contract fields.
2. Add mirror-offset and visual-flip tests.
3. Move the minimal runner/player logic needed for the new fields.
4. Prove a projectile skill works in both directions.
5. Only then migrate more skills.

Do not remove legacy data until migrated skills and editor validation cover the
old and new asset paths.

