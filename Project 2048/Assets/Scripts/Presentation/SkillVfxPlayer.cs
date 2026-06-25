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

        // 랜턴 머즐 기본 오프셋(앵커 비주얼 센터 기준, facing 반영).
        // CombatWorldSpriteView의 LanternMuzzleLocalX/Y와 동일값 — 프로바이더 castPoint 미지정 시 폴백.
        private const float DefaultCastPointLocalX = 0.34f;
        private const float DefaultCastPointLocalY = 0.36f;

        public static Vector3 ResolveEndpointWorldPosition(VfxEndpoint endpoint, SkillVfxContext ctx)
        {
            var anchor = ctx.ResolveActor(endpoint.actor);
            if (anchor == null)
            {
                return endpoint.localOffset;
            }

            // 발사 방향 기준이 되는 상대 액터(시전자의 머즐은 주대상 쪽을 향한다).
            var facingTarget = ctx.ResolveActor(OppositeActor(endpoint.actor));
            var basePos = ResolveSocketWorldPosition(anchor, endpoint.socket, facingTarget);
            return basePos + anchor.TransformVector(endpoint.localOffset);
        }

        private static Vector3 ResolveSocketWorldPosition(Transform anchor, VfxSocket socket, Transform facingTarget)
        {
            // 1) 캐릭터에 명시 소켓(손/입/무기 머즐)이 있으면 최우선.
            var provider = anchor.GetComponentInParent<CombatVfxAnchorProvider>();
            if (provider != null && provider.TryGetSocket(socket, out var socketTransform))
            {
                return socketTransform.position;
            }

            var hasBounds = TryResolveRendererBounds(anchor, out var bounds);
            var center = hasBounds ? bounds.center : anchor.position;

            // 2) CastPoint = 랜턴 머즐: 비주얼 센터에서 타깃 쪽으로 facing 반영한 오프셋.
            if (socket == VfxSocket.CastPoint)
            {
                var facingSign = ResolveFacingSign(anchor, facingTarget);
                return center + new Vector3(DefaultCastPointLocalX * facingSign, DefaultCastPointLocalY, 0f);
            }

            // 3) Root는 앵커 원점, 그 외는 bounds 기반(Feet/Body/Head, HitPoint는 몸통 중심).
            if (socket == VfxSocket.Root || !hasBounds)
            {
                return anchor.position;
            }

            var y = socket switch
            {
                VfxSocket.Feet => bounds.min.y,
                VfxSocket.Head => bounds.max.y,
                _ => bounds.center.y, // Body / HitPoint
            };
            return new Vector3(bounds.center.x, y, bounds.center.z);
        }

        // 시전자가 타깃을 바라보는 방향(+1 오른쪽 / -1 왼쪽). CombatWorldSpriteView.ResolveAttackFacingSign과 동일 규칙.
        private static float ResolveFacingSign(Transform anchor, Transform facingTarget)
        {
            if (anchor == null || facingTarget == null)
            {
                return 1f;
            }

            return anchor.position.x <= facingTarget.position.x ? 1f : -1f;
        }

        // 슬래시/빔류: 출발·도착 두 점의 중점에 놓고 +X축을 출발→도착 방향으로 회전.
        // 레거시 PlaySlashArcAttackArt(center=Lerp(src,dst,0.5), rotation=FromToRotation(right,dir))와 동일.
        private static void PlaceAsBeam(Transform t, Vector3 spawnPos, Vector3 destinationPos)
        {
            var direction = destinationPos - spawnPos;
            direction.z = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.right;
            }

            t.position = Vector3.Lerp(spawnPos, destinationPos, 0.5f);
            t.rotation = Quaternion.FromToRotation(Vector3.right, direction.normalized);
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
            else if (cue.useDestination)
            {
                // 이동하지 않는 빔/스팬(슬래시류): 두 점의 중점에 놓고 출발→도착 방향으로 회전.
                var destinationPos = ResolveEndpointWorldPosition(cue.destination, ctx);
                PlaceAsBeam(instance.transform, pos, destinationPos);
                PlayVisuals(instance);
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
