using Project2048.Skills;
using UnityEngine;

namespace Project2048.Presentation
{
    public readonly struct SkillVfxContext
    {
        public readonly Transform playerAnchor;
        public readonly Transform enemyAnchor;
        public readonly SkillVfxTrigger trigger;

        public SkillVfxContext(Transform playerAnchor, Transform enemyAnchor, SkillVfxTrigger trigger)
        {
            this.playerAnchor = playerAnchor;
            this.enemyAnchor = enemyAnchor;
            this.trigger = trigger;
        }

        public Transform AnchorFor(SkillVfxTarget target) =>
            target == SkillVfxTarget.Enemy ? enemyAnchor : playerAnchor;

        public Transform OppositeAnchorFor(SkillVfxTarget target) =>
            target == SkillVfxTarget.Enemy ? playerAnchor : enemyAnchor;
    }

    public static class SkillVfxPlayer
    {
        public static Vector3 ResolvePlacementWorldPosition(SkillVfxPlacement placement, SkillVfxContext ctx)
        {
            var anchor = ctx.AnchorFor(placement.target);
            if (anchor == null)
            {
                return placement.localOffset;
            }

            var basePos = ResolveVerticalWorldPosition(anchor, placement.vertical);
            return basePos + anchor.TransformVector(placement.localOffset);
        }

        private static Vector3 ResolveVerticalWorldPosition(Transform anchor, SkillVfxVertical vertical)
        {
            var renderer = anchor.GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null)
            {
                return anchor.position;
            }

            var b = renderer.bounds;
            var y = vertical switch
            {
                SkillVfxVertical.Feet => b.min.y,
                SkillVfxVertical.Head => b.max.y,
                _ => b.center.y,
            };
            return new Vector3(b.center.x, y, b.center.z);
        }

        public static System.Collections.Generic.List<GameObject> Play(
            SkillVfxDefinition definition,
            SkillVfxContext ctx,
            Transform parent,
            bool isPlaying)
        {
            var spawned = new System.Collections.Generic.List<GameObject>();
            if (definition == null) return spawned;

            foreach (var cue in definition.CuesFor(ctx.trigger))
            {
                // Edit mode / tests: ignore delay, spawn synchronously. Play-mode delay is handled
                // by the caller (the view) via a coroutine.
                var go = SpawnCue(cue, ctx, parent, isPlaying);
                if (go != null) spawned.Add(go);
            }

            return spawned;
        }

        private static GameObject SpawnCue(SkillVfxCue cue, SkillVfxContext ctx, Transform parent, bool isPlaying)
        {
            if (cue == null || !cue.HasPrefab) return null;

            var pos = ResolvePlacementWorldPosition(cue.placement, ctx);
            var instance = Object.Instantiate(cue.prefab, pos, Quaternion.identity, parent);
            instance.name = cue.prefab.name;

            if (cue.scale > 0f && !Mathf.Approximately(cue.scale, 1f))
            {
                instance.transform.localScale *= cue.scale;
            }

            var projectile = instance.GetComponentInChildren<CombatProjectileEffect>(true);
            if (projectile != null)
            {
                var targetAnchor = ctx.OppositeAnchorFor(cue.placement.target);
                projectile.LaunchFromWorldPosition(pos, targetAnchor, Vector3.zero);
            }

            var lifetime = cue.lifetimeOverride > 0f ? cue.lifetimeOverride : 0f;
            if (lifetime > 0f && isPlaying)
            {
                Object.Destroy(instance, lifetime);
            }

            return instance;
        }
    }
}
