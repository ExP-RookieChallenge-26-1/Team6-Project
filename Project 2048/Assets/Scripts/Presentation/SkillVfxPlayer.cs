using Project2048.Skills;
using UnityEngine;
using UnityEngine.VFX;

namespace Project2048.Presentation
{
    // 같은 SkillSO를 누가 쓰든 컨텍스트만 뒤집힌다.
    // 플레이어 시전: caster=플레이어, primaryTarget=적 / 적 시전: 그 역. 데이터(SkillSO)는 복제하지 않는다.
    public readonly struct SkillVfxContext
    {
        public readonly Transform caster;
        public readonly Transform primaryTarget;
        public readonly SkillVfxTrigger trigger;

        public SkillVfxContext(Transform caster, Transform primaryTarget, SkillVfxTrigger trigger)
        {
            this.caster = caster;
            this.primaryTarget = primaryTarget;
            this.trigger = trigger;
        }

        public Transform ResolveActor(VfxActorRef actor) =>
            actor == VfxActorRef.PrimaryTarget ? primaryTarget : caster;
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

        public static VfxActorRef OppositeActor(VfxActorRef actor) =>
            actor == VfxActorRef.Caster ? VfxActorRef.PrimaryTarget : VfxActorRef.Caster;

        public static Vector3 ResolveEndpointWorldPosition(VfxEndpoint endpoint, SkillVfxContext ctx)
        {
            var anchor = ctx.ResolveActor(endpoint.actor);
            if (anchor == null)
            {
                return endpoint.localOffset;
            }

            var basePos = ResolveSocketWorldPosition(anchor, endpoint.socket);
            return basePos + anchor.TransformVector(endpoint.localOffset);
        }

        private static Vector3 ResolveSocketWorldPosition(Transform anchor, VfxSocket socket)
        {
            // Step 5에서 CombatVfxAnchorProvider가 명시 소켓을 우선 제공한다. 여기서는 스프라이트 bounds 폴백.
            if (socket == VfxSocket.Root || !TryResolveRendererBounds(anchor, out var bounds))
            {
                return anchor.position;
            }

            var y = socket switch
            {
                VfxSocket.Feet => bounds.min.y,
                VfxSocket.Head => bounds.max.y,
                // Body / CastPoint / HitPoint 는 소켓 미설정 시 몸통 중심으로 폴백.
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
                // by the caller (the view / runner) via a coroutine.
                var go = PlayCue(cue, ctx, parent, isPlaying);
                if (go != null) spawned.Add(go);
            }

            return spawned;
        }

        public static GameObject PlayCue(SkillVfxCue cue, SkillVfxContext ctx, Transform parent, bool isPlaying)
        {
            if (cue == null || !cue.HasPrefab) return null;

            var pos = ResolveEndpointWorldPosition(cue.spawnAt, ctx);
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
                if (cue.useDestination)
                {
                    // 도착점이 명시됨 → 그 월드 좌표로 직행.
                    var destinationPos = ResolveEndpointWorldPosition(cue.destination, ctx);
                    projectile.LaunchBetweenWorldPositions(pos, destinationPos);
                }
                else
                {
                    // 미지정 → 반대 액터로 폴백(프리팹 자체 targetLocalOffset 유지).
                    var targetAnchor = ctx.ResolveActor(OppositeActor(cue.spawnAt.actor));
                    projectile.LaunchFromWorldPosition(pos, targetAnchor, Vector3.zero);
                }
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
