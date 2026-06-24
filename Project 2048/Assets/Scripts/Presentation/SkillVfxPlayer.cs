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
    }
}
