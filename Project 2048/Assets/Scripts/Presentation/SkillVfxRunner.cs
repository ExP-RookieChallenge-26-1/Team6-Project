using System.Collections;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Presentation
{
    // 큐 순회·지연·스폰·수명 관리를 한 곳에 모은다.
    // 뷰(CombatWorldSpriteView)는 Play() 한 번만 호출하고 빠진다.
    //
    // 투사체 큐를 스폰하면, 정의에 Impact 트리거 큐가 있을 때 투사체가 "실제로 도착"하는 순간
    // 그 큐들을 재생한다. 고정 delaySeconds로 충돌을 맞추던 방식을 대체한다(속도/거리 바뀌어도 안 어긋남).
    public sealed class SkillVfxRunner : MonoBehaviour
    {
        // 해당 트리거의 큐를 재생한다. 하나라도 재생(또는 지연 예약)됐으면 true.
        public bool Play(SkillVfxDefinition definition, SkillVfxContext ctx, Transform spawnParent)
        {
            if (definition == null || !definition.HasAnyCue)
            {
                return false;
            }

            var playedAny = false;
            foreach (var cue in definition.CuesFor(ctx.trigger))
            {
                if (cue == null || !cue.HasPrefab)
                {
                    continue;
                }

                playedAny = true;

                // 플레이 모드에서만 지연을 코루틴으로 처리. 에디트 모드/테스트는 즉시 스폰.
                if (Application.isPlaying && cue.delaySeconds > 0f)
                {
                    StartCoroutine(SpawnCueAfterDelay(cue, definition, ctx, spawnParent, cue.delaySeconds));
                }
                else
                {
                    SpawnCue(cue, definition, ctx, spawnParent);
                }
            }

            return playedAny;
        }

        private IEnumerator SpawnCueAfterDelay(
            SkillVfxCue cue, SkillVfxDefinition definition, SkillVfxContext ctx, Transform spawnParent, float delaySeconds)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, delaySeconds));
            SpawnCue(cue, definition, ctx, spawnParent);
        }

        private void SpawnCue(SkillVfxCue cue, SkillVfxDefinition definition, SkillVfxContext ctx, Transform spawnParent)
        {
            var instance = SkillVfxPlayer.PlayCue(cue, ctx, spawnParent, Application.isPlaying);
            if (instance == null)
            {
                return;
            }

            // 투사체 + Impact 큐가 있으면, 도착 시점에 Impact 트리거 큐를 재생한다.
            if (!definition.HasCuesFor(SkillVfxTrigger.Impact))
            {
                return;
            }

            var projectile = instance.GetComponentInChildren<CombatProjectileEffect>(true);
            if (projectile == null)
            {
                return;
            }

            var impactCtx = new SkillVfxContext(ctx.caster, ctx.primaryTarget, SkillVfxTrigger.Impact);
            if (Application.isPlaying)
            {
                projectile.Impacted += () => PlayImpactCues(definition, impactCtx, spawnParent);
            }
            else
            {
                // 에디트 모드/프리뷰: 투사체가 Update로 도착하지 않으므로 즉시 재생(기존 동작 유지).
                PlayImpactCues(definition, impactCtx, spawnParent);
            }
        }

        private void PlayImpactCues(SkillVfxDefinition definition, SkillVfxContext impactCtx, Transform spawnParent)
        {
            foreach (var cue in definition.CuesFor(SkillVfxTrigger.Impact))
            {
                if (cue == null || !cue.HasPrefab)
                {
                    continue;
                }

                // Impact 큐의 delaySeconds는 도착 후 미세 연출 지연용으로만 쓴다.
                if (Application.isPlaying && cue.delaySeconds > 0f)
                {
                    StartCoroutine(PlayCueAfterDelay(cue, impactCtx, spawnParent, cue.delaySeconds));
                }
                else
                {
                    SkillVfxPlayer.PlayCue(cue, impactCtx, spawnParent, Application.isPlaying);
                }
            }
        }

        private IEnumerator PlayCueAfterDelay(SkillVfxCue cue, SkillVfxContext ctx, Transform spawnParent, float delaySeconds)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, delaySeconds));
            SkillVfxPlayer.PlayCue(cue, ctx, spawnParent, Application.isPlaying);
        }
    }
}
