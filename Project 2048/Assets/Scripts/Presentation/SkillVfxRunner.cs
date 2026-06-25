using System.Collections;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Presentation
{
    // 큐 순회·지연·스폰·수명 관리를 한 곳에 모은다.
    // 뷰(CombatWorldSpriteView)는 Play() 한 번만 호출하고 빠진다.
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
                    StartCoroutine(PlayAfterDelay(cue, ctx, spawnParent, cue.delaySeconds));
                }
                else
                {
                    SkillVfxPlayer.PlayCue(cue, ctx, spawnParent, Application.isPlaying);
                }
            }

            return playedAny;
        }

        private IEnumerator PlayAfterDelay(SkillVfxCue cue, SkillVfxContext ctx, Transform spawnParent, float delaySeconds)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, delaySeconds));
            SkillVfxPlayer.PlayCue(cue, ctx, spawnParent, Application.isPlaying);
        }
    }
}
