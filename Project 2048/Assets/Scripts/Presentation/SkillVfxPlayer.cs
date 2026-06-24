using Project2048.Skills;
using UnityEngine;
using UnityEngine.VFX;

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
        private static readonly string[] VisualEffectColorPropertyNames =
        {
            "Color",
            "Tint",
            "BaseColor",
            "Base Color",
            "MainColor",
            "ParticleColor",
        };

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
            if (!TryResolveRendererBounds(anchor, out var bounds))
            {
                return anchor.position;
            }

            var y = vertical switch
            {
                SkillVfxVertical.Feet => bounds.min.y,
                SkillVfxVertical.Head => bounds.max.y,
                _ => bounds.center.y,
            };
            return new Vector3(bounds.center.x, y, bounds.center.z);
        }

        private static bool TryResolveRendererBounds(Transform anchor, out Bounds bounds)
        {
            bounds = default;
            if (anchor == null)
            {
                return false;
            }

            var renderer = anchor.GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null)
            {
                bounds = renderer.bounds;
                return true;
            }

            var hasBounds = false;
            foreach (var childRenderer in anchor.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (childRenderer == null || childRenderer.sprite == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = childRenderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(childRenderer.bounds);
                }
            }

            return hasBounds;
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
                var go = PlayCue(cue, ctx, parent, isPlaying);
                if (go != null) spawned.Add(go);
            }

            return spawned;
        }

        public static GameObject PlayCue(SkillVfxCue cue, SkillVfxContext ctx, Transform parent, bool isPlaying)
        {
            if (cue == null || !cue.HasPrefab) return null;

            var pos = ResolvePlacementWorldPosition(cue.placement, ctx);
            var instance = Object.Instantiate(cue.prefab, pos, Quaternion.identity, parent);
            instance.name = cue.prefab.name;

            if (cue.scale > 0f && !Mathf.Approximately(cue.scale, 1f))
            {
                instance.transform.localScale *= cue.scale;
            }

            ApplyTint(instance, cue.tint);

            var projectile = instance.GetComponentInChildren<CombatProjectileEffect>(true);
            if (projectile != null)
            {
                // 프로젝타일은 스스로 이동/임팩트 파티클을 재생한다.
                var targetAnchor = ctx.OppositeAnchorFor(cue.placement.target);
                projectile.LaunchFromWorldPosition(pos, targetAnchor, Vector3.zero);
            }
            else
            {
                PlayVisuals(instance);
            }

            var lifetime = cue.lifetimeOverride > 0f ? cue.lifetimeOverride : 0f;
            if (lifetime > 0f && isPlaying)
            {
                Object.Destroy(instance, lifetime);
            }

            return instance;
        }

        public static void ApplyTint(GameObject instance, Color tint)
        {
            if (instance == null || tint.a <= 0f) return; // Color.clear = leave the prefab's authored colors

            foreach (var sr in instance.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.color = tint;
            }

            foreach (var ps in instance.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.startColor = tint;
            }

            var vectorColor = new Vector4(tint.r, tint.g, tint.b, tint.a);
            foreach (var vfx in instance.GetComponentsInChildren<VisualEffect>(true))
            {
                if (vfx == null)
                {
                    continue;
                }

                foreach (var propertyName in VisualEffectColorPropertyNames)
                {
                    if (vfx.HasVector4(propertyName))
                    {
                        vfx.SetVector4(propertyName, vectorColor);
                    }
                    else if (vfx.HasVector3(propertyName))
                    {
                        vfx.SetVector3(propertyName, new Vector3(tint.r, tint.g, tint.b));
                    }
                }
            }
        }

        private static void PlayVisuals(GameObject instance)
        {
            foreach (var ps in instance.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Play(true);
            }

            foreach (var vfx in instance.GetComponentsInChildren<VisualEffect>(true))
            {
                if (Application.isPlaying)
                {
                    vfx.Reinit();
                }

                vfx.Play();
            }
        }
    }
}
